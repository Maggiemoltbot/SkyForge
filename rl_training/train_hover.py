#!/usr/bin/env python3
"""
SkyForge RL Training — Hover Task (PPO via Stable-Baselines3)

Trains a drone agent to achieve stable hover using PPO.
Communicates with the SkyForge Unity simulation via Unity ML-Agents
or a custom UDP bridge.

Usage:
    python train_hover.py --timesteps 500000 --save-dir ./models
    python train_hover.py --eval --model ./models/hover_best.zip

Requirements:
    pip install stable-baselines3 gymnasium numpy torch tensorboard
    pip install mlagents-envs  # for Unity ML-Agents connection

Author: Maggie (CAIO, XFLIGHT GmbH)
Date:   2026-04-11
"""

import argparse
import os
import sys
import time
from pathlib import Path
from typing import Optional

import numpy as np

try:
    import gymnasium as gym
    from gymnasium import spaces
except ImportError:
    print("ERROR: gymnasium not installed. Run: pip install gymnasium")
    sys.exit(1)

try:
    from stable_baselines3 import PPO
    from stable_baselines3.common.callbacks import (
        CheckpointCallback,
        EvalCallback,
        CallbackList,
    )
    from stable_baselines3.common.monitor import Monitor
    from stable_baselines3.common.vec_env import DummyVecEnv, SubprocVecEnv
except ImportError:
    print("ERROR: stable-baselines3 not installed. Run: pip install stable-baselines3")
    sys.exit(1)


# ============================================================
# Reward Functions (modular)
# ============================================================

def hover_reward(obs: np.ndarray, action: np.ndarray, done: bool, info: dict) -> float:
    """
    Reward function for stable hover.

    Observation layout (18-dim):
        0-2:  position relative to target (x, y, z) in meters
        3-5:  velocity (vx, vy, vz) in m/s
        6-8:  rotation (roll, pitch, yaw) in radians
        9-11: angular velocity (wx, wy, wz) in rad/s
        12-15: motor PWM (4x) normalized [0, 1]
        16:   height above ground (m)
        17:   distance to target (m)

    Args:
        obs:    Current observation (18-dim float array)
        action: Applied action (4-dim float array)
        done:   Whether the episode ended
        info:   Additional info dict

    Returns:
        Scalar reward value
    """
    # Weights
    ALPHA = 1.0    # position error penalty
    BETA = 0.5     # orientation error penalty
    GAMMA = 0.1    # velocity penalty
    DELTA = 0.01   # energy penalty
    EPSILON = 0.1  # alive bonus

    # Extract components
    pos_error = obs[17]  # distance to target
    vel_magnitude = np.linalg.norm(obs[3:6])
    orientation_error = np.sqrt(obs[6]**2 + obs[7]**2)  # roll + pitch deviation
    motor_effort = np.sum(np.abs(action))
    height = obs[16]

    # Compute reward
    reward = 0.0
    reward -= ALPHA * pos_error
    reward -= BETA * orientation_error
    reward -= GAMMA * vel_magnitude
    reward -= DELTA * motor_effort
    reward += EPSILON  # alive bonus

    # Crash penalty
    if done and info.get("crash", False):
        reward -= 100.0

    # Out-of-bounds penalty
    if done and info.get("out_of_bounds", False):
        reward -= 50.0

    # Bonus for being very close to target
    if pos_error < 0.5:
        reward += 1.0
    if pos_error < 0.1:
        reward += 5.0

    return reward


# ============================================================
# Gym Environment Wrapper
# ============================================================

class SkyForgeHoverEnv(gym.Env):
    """
    Gymnasium wrapper for the SkyForge drone hover task.

    This is a skeleton that connects to Unity via:
    - Unity ML-Agents (preferred, via mlagents_envs)
    - Custom UDP bridge (fallback)

    For standalone testing, a simple physics mock is included.
    """

    metadata = {"render_modes": ["human"]}

    # Observation: 18-dim float
    OBS_DIM = 18
    # Action: 4 motor PWM deltas
    ACT_DIM = 4

    # Episode limits
    MAX_STEPS = 2000       # ~20 seconds at 100Hz
    MAX_HEIGHT = 100.0     # meters
    MAX_DISTANCE = 50.0    # meters from target
    CRASH_HEIGHT = 0.05    # below this = crash

    def __init__(
        self,
        unity_env=None,
        render_mode: Optional[str] = None,
        target_pos: Optional[np.ndarray] = None,
        dt: float = 0.01,
    ):
        super().__init__()
        self.render_mode = render_mode
        self.unity_env = unity_env
        self.dt = dt
        self.target_pos = target_pos if target_pos is not None else np.array([0.0, 2.0, 0.0])

        # Spaces
        obs_low = np.full(self.OBS_DIM, -100.0, dtype=np.float32)
        obs_high = np.full(self.OBS_DIM, 100.0, dtype=np.float32)
        self.observation_space = spaces.Box(obs_low, obs_high, dtype=np.float32)

        self.action_space = spaces.Box(
            low=-1.0, high=1.0, shape=(self.ACT_DIM,), dtype=np.float32
        )

        # State
        self._step_count = 0
        self._pos = np.zeros(3)
        self._vel = np.zeros(3)
        self._rot = np.zeros(3)
        self._ang_vel = np.zeros(3)
        self._motors = np.zeros(4)

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        if self.unity_env is not None:
            # Reset Unity environment
            obs = self._unity_reset()
            return obs, {}

        # Mock reset: drone at ground level with small random perturbation
        self._step_count = 0
        self._pos = np.array([0.0, 0.1, 0.0]) + self.np_random.uniform(-0.05, 0.05, 3)
        self._pos[1] = max(0.1, self._pos[1])
        self._vel = np.zeros(3)
        self._rot = self.np_random.uniform(-0.05, 0.05, 3)
        self._ang_vel = np.zeros(3)
        self._motors = np.full(4, 0.5)

        return self._get_obs(), {}

    def step(self, action: np.ndarray):
        self._step_count += 1
        action = np.clip(action, -1.0, 1.0)

        if self.unity_env is not None:
            obs, reward, done, info = self._unity_step(action)
            truncated = self._step_count >= self.MAX_STEPS
            return obs, reward, done, truncated, info

        # ---- Mock physics (simplified quadcopter) ----
        # Convert actions to motor PWM [0, 1]
        self._motors = np.clip(self._motors + action * 0.1, 0.0, 1.0)

        # Simplified thrust model
        GRAVITY = 9.81
        MASS = 0.5  # kg
        MAX_THRUST_PER_MOTOR = 3.0  # N (total 12N > mg=4.9N)

        total_thrust = np.sum(self._motors) * MAX_THRUST_PER_MOTOR
        thrust_accel = total_thrust / MASS

        # Acceleration (simplified: thrust acts along body Y-axis)
        cos_roll = np.cos(self._rot[0])
        cos_pitch = np.cos(self._rot[1])
        accel = np.array([
            -thrust_accel * np.sin(self._rot[1]),
            thrust_accel * cos_roll * cos_pitch - GRAVITY,
            thrust_accel * np.sin(self._rot[0]) * cos_pitch,
        ])

        # Torque from differential thrust (simplified)
        ARM_LENGTH = 0.1  # meters
        torque_roll = (self._motors[1] + self._motors[3] - self._motors[0] - self._motors[2]) * ARM_LENGTH
        torque_pitch = (self._motors[0] + self._motors[1] - self._motors[2] - self._motors[3]) * ARM_LENGTH
        torque_yaw = (self._motors[0] + self._motors[3] - self._motors[1] - self._motors[2]) * 0.02

        # Update angular velocity and rotation
        INERTIA = 0.01
        ang_accel = np.array([torque_roll, torque_pitch, torque_yaw]) / INERTIA
        self._ang_vel += ang_accel * self.dt
        self._ang_vel *= 0.98  # damping
        self._rot += self._ang_vel * self.dt

        # Update velocity and position
        self._vel += accel * self.dt
        self._vel *= 0.995  # air drag
        self._pos += self._vel * self.dt

        # Ground collision
        if self._pos[1] < 0.0:
            self._pos[1] = 0.0
            self._vel[1] = max(0.0, self._vel[1])

        # Check done conditions
        info = {"crash": False, "out_of_bounds": False, "timeout": False}
        done = False

        if self._pos[1] < self.CRASH_HEIGHT and self._vel[1] < -1.0:
            done = True
            info["crash"] = True

        dist = np.linalg.norm(self._pos - self.target_pos)
        if dist > self.MAX_DISTANCE:
            done = True
            info["out_of_bounds"] = True

        if abs(self._rot[0]) > np.pi / 2 or abs(self._rot[1]) > np.pi / 2:
            done = True
            info["crash"] = True

        truncated = self._step_count >= self.MAX_STEPS
        if truncated:
            info["timeout"] = True

        obs = self._get_obs()
        reward = hover_reward(obs, action, done, info)

        return obs, reward, done, truncated, info

    def _get_obs(self) -> np.ndarray:
        """Build 18-dim observation vector."""
        rel_pos = self._pos - self.target_pos
        distance = np.linalg.norm(rel_pos)
        obs = np.concatenate([
            rel_pos,             # 0-2: position relative to target
            self._vel,           # 3-5: velocity
            self._rot,           # 6-8: rotation
            self._ang_vel,       # 9-11: angular velocity
            self._motors,        # 12-15: motor PWM
            [self._pos[1]],      # 16: height AGL
            [distance],          # 17: distance to target
        ]).astype(np.float32)
        return obs

    def _unity_reset(self) -> np.ndarray:
        """Reset Unity environment via ML-Agents or UDP bridge."""
        # TODO: Implement Unity ML-Agents connection
        raise NotImplementedError("Unity connection not yet implemented. Use mock physics for now.")

    def _unity_step(self, action):
        """Step Unity environment."""
        # TODO: Implement Unity ML-Agents step
        raise NotImplementedError("Unity connection not yet implemented. Use mock physics for now.")

    def render(self):
        if self.render_mode == "human":
            print(
                f"Step {self._step_count:4d} | "
                f"Pos: ({self._pos[0]:+.2f}, {self._pos[1]:+.2f}, {self._pos[2]:+.2f}) | "
                f"Dist: {np.linalg.norm(self._pos - self.target_pos):.2f}m | "
                f"Motors: [{', '.join(f'{m:.2f}' for m in self._motors)}]"
            )


# ============================================================
# Training
# ============================================================

def make_env(rank: int = 0, seed: int = 0):
    """Create a monitored SkyForge environment."""
    def _init():
        env = SkyForgeHoverEnv(target_pos=np.array([0.0, 2.0, 0.0]))
        env.reset(seed=seed + rank)
        return Monitor(env)
    return _init


def train(args):
    """Train PPO agent for hover task."""
    print("=" * 60)
    print("SkyForge RL Training — Hover Task (PPO)")
    print("=" * 60)

    save_dir = Path(args.save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)
    log_dir = save_dir / "logs"
    log_dir.mkdir(parents=True, exist_ok=True)

    # Create vectorized environment
    n_envs = args.n_envs
    if n_envs > 1:
        env = SubprocVecEnv([make_env(i, args.seed) for i in range(n_envs)])
    else:
        env = DummyVecEnv([make_env(0, args.seed)])

    # Eval environment
    eval_env = DummyVecEnv([make_env(99, args.seed + 99)])

    # PPO hyperparameters (tuned for drone hover)
    model = PPO(
        "MlpPolicy",
        env,
        learning_rate=3e-4,
        n_steps=2048,
        batch_size=64,
        n_epochs=10,
        gamma=0.99,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=0.01,
        vf_coef=0.5,
        max_grad_norm=0.5,
        verbose=1,
        tensorboard_log=str(log_dir),
        seed=args.seed,
        device="auto",
        policy_kwargs=dict(
            net_arch=dict(pi=[256, 256], vf=[256, 256]),
        ),
    )

    # Callbacks
    checkpoint_cb = CheckpointCallback(
        save_freq=max(10000 // n_envs, 1),
        save_path=str(save_dir / "checkpoints"),
        name_prefix="hover",
    )

    eval_cb = EvalCallback(
        eval_env,
        best_model_save_path=str(save_dir / "best"),
        log_path=str(log_dir),
        eval_freq=max(5000 // n_envs, 1),
        n_eval_episodes=10,
        deterministic=True,
    )

    callbacks = CallbackList([checkpoint_cb, eval_cb])

    print(f"\nTraining for {args.timesteps:,} timesteps...")
    print(f"  Environments: {n_envs}")
    print(f"  Save directory: {save_dir}")
    print(f"  TensorBoard: tensorboard --logdir {log_dir}")
    print()

    t0 = time.time()
    model.learn(
        total_timesteps=args.timesteps,
        callback=callbacks,
        progress_bar=True,
    )
    elapsed = time.time() - t0

    # Save final model
    final_path = save_dir / "hover_final"
    model.save(str(final_path))

    print(f"\n{'=' * 60}")
    print(f"Training complete in {elapsed:.1f}s ({elapsed/60:.1f}min)")
    print(f"Final model saved: {final_path}.zip")
    print(f"Best model: {save_dir / 'best' / 'best_model.zip'}")
    print(f"TensorBoard logs: {log_dir}")
    print(f"{'=' * 60}")

    env.close()
    eval_env.close()


def evaluate(args):
    """Evaluate a trained model."""
    print(f"Loading model: {args.model}")
    model = PPO.load(args.model)

    env = SkyForgeHoverEnv(render_mode="human", target_pos=np.array([0.0, 2.0, 0.0]))
    obs, _ = env.reset()

    total_reward = 0.0
    n_episodes = args.eval_episodes
    episode_rewards = []

    for ep in range(n_episodes):
        obs, _ = env.reset()
        ep_reward = 0.0
        done = False
        truncated = False

        while not done and not truncated:
            action, _ = model.predict(obs, deterministic=True)
            obs, reward, done, truncated, info = env.step(action)
            ep_reward += reward
            env.render()

        episode_rewards.append(ep_reward)
        print(f"\nEpisode {ep + 1}/{n_episodes}: Reward = {ep_reward:.2f}")

    mean_reward = np.mean(episode_rewards)
    std_reward = np.std(episode_rewards)
    print(f"\n{'=' * 40}")
    print(f"Evaluation: {mean_reward:.2f} ± {std_reward:.2f} over {n_episodes} episodes")
    print(f"{'=' * 40}")


# ============================================================
# CLI
# ============================================================

def main():
    parser = argparse.ArgumentParser(description="SkyForge RL Training — Hover Task")
    subparsers = parser.add_subparsers(dest="command", help="Command")

    # Train
    train_parser = subparsers.add_parser("train", help="Train a PPO hover agent")
    train_parser.add_argument("--timesteps", type=int, default=500_000, help="Total training timesteps")
    train_parser.add_argument("--n-envs", type=int, default=4, help="Number of parallel environments")
    train_parser.add_argument("--save-dir", type=str, default="./models/hover", help="Model save directory")
    train_parser.add_argument("--seed", type=int, default=42, help="Random seed")

    # Eval
    eval_parser = subparsers.add_parser("eval", help="Evaluate a trained model")
    eval_parser.add_argument("--model", type=str, required=True, help="Path to trained model .zip")
    eval_parser.add_argument("--eval-episodes", type=int, default=10, help="Number of eval episodes")

    args = parser.parse_args()

    if args.command == "train":
        train(args)
    elif args.command == "eval":
        evaluate(args)
    else:
        # Default: run training
        args.command = "train"
        args.timesteps = 500_000
        args.n_envs = 4
        args.save_dir = "./models/hover"
        args.seed = 42
        train(args)


if __name__ == "__main__":
    main()
