"""
SkyForge Drone RL Training — train_hover.py
============================================
Gymnasium-kompatibles Environment + PPO Training-Loop mit Stable-Baselines3.

Kommuniziert mit Unity DroneRLEnvironment via TCP-Socket (Port 9020).
Unity muss vor dem Training gestartet sein und Play-Mode aktiv sein.

Verwendung:
    python train_hover.py                    # Training mit Standard-Config
    python train_hover.py --config my.yaml   # Training mit eigener Config
    python train_hover.py --eval             # Nur Evaluation (kein Training)
    python train_hover.py --resume           # Training fortsetzen

Abhängigkeiten:
    pip install stable-baselines3 gymnasium torch tensorboard pyyaml
"""

import argparse
import json
import os
import socket
import time
from pathlib import Path
from typing import Any, Optional, Tuple

import gymnasium as gym
import numpy as np
import yaml
from gymnasium import spaces
from stable_baselines3 import PPO
from stable_baselines3.common.callbacks import (
    BaseCallback,
    CheckpointCallback,
    EvalCallback,
)
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv

# ─────────────────────────────────────────────────────────────────────────────
# Default Hyperparameter Config
# ─────────────────────────────────────────────────────────────────────────────

DEFAULT_CONFIG = {
    "env": {
        "host": "127.0.0.1",
        "port": 9020,
        "connect_timeout": 30.0,      # Sekunden auf Unity-Verbindung warten
        "step_timeout": 5.0,          # Sekunden auf Step-Antwort warten
        "obs_dim": 18,
        "action_dim": 4,
        "action_low": -1.0,
        "action_high": 1.0,
    },
    "training": {
        "total_timesteps": 1_000_000,
        "n_steps": 2048,
        "batch_size": 64,
        "n_epochs": 10,
        "learning_rate": 3e-4,
        "gamma": 0.99,
        "gae_lambda": 0.95,
        "clip_range": 0.2,
        "ent_coef": 0.0,
        "vf_coef": 0.5,
        "max_grad_norm": 0.5,
        "target_kl": None,
    },
    "checkpoints": {
        "save_freq": 100,             # Checkpoint alle 100 Episoden (ca.)
        "save_path": "checkpoints/",
        "name_prefix": "drone_hover",
        "keep_last_n": 5,
    },
    "logging": {
        "tensorboard_log": "runs/",
        "verbose": 1,
    },
    "eval": {
        "eval_freq": 10_000,
        "n_eval_episodes": 5,
        "deterministic": True,
    },
}


# ─────────────────────────────────────────────────────────────────────────────
# Unity Socket Client
# ─────────────────────────────────────────────────────────────────────────────

class UnitySocketClient:
    """
    TCP-Client für die Kommunikation mit DroneRLEnvironment.cs in Unity.
    
    Protokoll (JSON, newline-terminiert):
    
    Senden:
        {"cmd": "reset"}
        {"cmd": "step", "action": [a0, a1, a2, a3]}
        {"cmd": "ping"}
    
    Empfangen:
        {"type": "reset", "obs": [...18 floats...]}
        {"type": "step", "obs": [...], "reward": float, "done": bool}
        {"type": "pong"}
    """

    def __init__(self, host: str = "127.0.0.1", port: int = 9020,
                 connect_timeout: float = 30.0, step_timeout: float = 5.0):
        self.host = host
        self.port = port
        self.connect_timeout = connect_timeout
        self.step_timeout = step_timeout
        self.sock: Optional[socket.socket] = None
        self._buffer = ""

    def connect(self) -> None:
        """Verbindet mit dem Unity TCP-Server. Wartet bis zu connect_timeout Sekunden."""
        deadline = time.time() + self.connect_timeout
        last_error = None

        print(f"[SocketClient] Verbinde mit Unity auf {self.host}:{self.port} ...")
        while time.time() < deadline:
            try:
                self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                self.sock.settimeout(2.0)
                self.sock.connect((self.host, self.port))
                self.sock.settimeout(self.step_timeout)
                print(f"[SocketClient] Verbunden!")
                return
            except (ConnectionRefusedError, OSError) as e:
                last_error = e
                time.sleep(1.0)

        raise TimeoutError(
            f"Konnte nicht mit Unity verbinden ({self.host}:{self.port}) "
            f"nach {self.connect_timeout}s. Letzter Fehler: {last_error}\n"
            "→ Stelle sicher, dass Unity im Play-Mode ist und "
            "DroneRLEnvironment.cs aktiv ist."
        )

    def disconnect(self) -> None:
        if self.sock:
            try:
                self.sock.close()
            except OSError:
                pass
            self.sock = None

    def send(self, data: dict) -> None:
        msg = json.dumps(data) + "\n"
        self.sock.sendall(msg.encode("utf-8"))

    def recv(self) -> dict:
        """Empfängt eine JSON-Nachricht (newline-terminiert)."""
        while "\n" not in self._buffer:
            chunk = self.sock.recv(4096).decode("utf-8")
            if not chunk:
                raise ConnectionError("Unity hat die Verbindung getrennt.")
            self._buffer += chunk

        line, self._buffer = self._buffer.split("\n", 1)
        return json.loads(line.strip())

    def ping(self) -> bool:
        """Testet die Verbindung."""
        try:
            self.send({"cmd": "ping"})
            resp = self.recv()
            return resp.get("type") == "pong"
        except Exception:
            return False


# ─────────────────────────────────────────────────────────────────────────────
# Gymnasium Environment
# ─────────────────────────────────────────────────────────────────────────────

class DroneHoverEnv(gym.Env):
    """
    Gymnasium-kompatibles Environment für den SkyForge Drone Hover Task.
    
    Wraps den Unity-Simulator via TCP-Socket.
    
    Observation Space: Box(18,) — float32
    Action Space:      Box(4,) in [-1, 1] — float32
    """

    metadata = {"render_modes": ["human"]}

    def __init__(self, config: dict):
        super().__init__()

        env_cfg = config["env"]

        # Observation & Action Spaces
        obs_dim = env_cfg["obs_dim"]
        self.observation_space = spaces.Box(
            low=-np.inf,
            high=np.inf,
            shape=(obs_dim,),
            dtype=np.float32,
        )

        action_dim = env_cfg["action_dim"]
        self.action_space = spaces.Box(
            low=env_cfg["action_low"],
            high=env_cfg["action_high"],
            shape=(action_dim,),
            dtype=np.float32,
        )

        # Socket Client
        self.client = UnitySocketClient(
            host=env_cfg["host"],
            port=env_cfg["port"],
            connect_timeout=env_cfg["connect_timeout"],
            step_timeout=env_cfg["step_timeout"],
        )
        self.client.connect()

        self._episode_reward = 0.0
        self._episode_steps = 0
        self._episode_count = 0

    def reset(
        self,
        *,
        seed: Optional[int] = None,
        options: Optional[dict] = None,
    ) -> Tuple[np.ndarray, dict]:
        super().reset(seed=seed)

        self.client.send({"cmd": "reset"})
        resp = self.client.recv()

        assert resp["type"] == "reset", f"Unerwartete Antwort: {resp}"
        obs = np.array(resp["obs"], dtype=np.float32)

        self._episode_reward = 0.0
        self._episode_steps = 0
        self._episode_count += 1

        return obs, {}

    def step(self, action: np.ndarray) -> Tuple[np.ndarray, float, bool, bool, dict]:
        action_list = action.tolist()
        self.client.send({"cmd": "step", "action": action_list})

        resp = self.client.recv()
        assert resp["type"] == "step", f"Unerwartete Antwort: {resp}"

        obs = np.array(resp["obs"], dtype=np.float32)
        reward = float(resp["reward"])
        terminated = bool(resp["done"])
        truncated = False

        self._episode_reward += reward
        self._episode_steps += 1

        info = {
            "episode_reward": self._episode_reward,
            "episode_steps": self._episode_steps,
        }

        return obs, reward, terminated, truncated, info

    def close(self) -> None:
        self.client.disconnect()

    def render(self) -> None:
        # Rendering passiert in Unity
        pass


# ─────────────────────────────────────────────────────────────────────────────
# Callbacks
# ─────────────────────────────────────────────────────────────────────────────

class EpisodeLogCallback(BaseCallback):
    """Loggt Episode-Rewards nach jeder abgeschlossenen Episode."""

    def __init__(self, verbose: int = 0):
        super().__init__(verbose)
        self._episode_rewards = []
        self._episode_count = 0

    def _on_step(self) -> bool:
        infos = self.locals.get("infos", [])
        dones = self.locals.get("dones", [])

        for i, done in enumerate(dones):
            if done and i < len(infos):
                ep_reward = infos[i].get("episode_reward", 0.0)
                self._episode_rewards.append(ep_reward)
                self._episode_count += 1

                if self.verbose >= 1 and self._episode_count % 10 == 0:
                    mean_reward = np.mean(self._episode_rewards[-100:])
                    print(
                        f"[Episode {self._episode_count:5d}] "
                        f"Reward: {ep_reward:8.2f} | "
                        f"Mean(100): {mean_reward:8.2f}"
                    )

                self.logger.record("episode/reward", ep_reward)
                self.logger.record("episode/count", self._episode_count)

        return True


class CheckpointByEpisodeCallback(BaseCallback):
    """Speichert Checkpoints alle N Episoden."""

    def __init__(self, save_freq_episodes: int, save_path: str,
                 name_prefix: str = "model", verbose: int = 0):
        super().__init__(verbose)
        self.save_freq_episodes = save_freq_episodes
        self.save_path = Path(save_path)
        self.name_prefix = name_prefix
        self._episode_count = 0
        self.save_path.mkdir(parents=True, exist_ok=True)

    def _on_step(self) -> bool:
        dones = self.locals.get("dones", [])
        for done in dones:
            if done:
                self._episode_count += 1
                if self._episode_count % self.save_freq_episodes == 0:
                    path = self.save_path / f"{self.name_prefix}_ep{self._episode_count}"
                    self.model.save(str(path))
                    if self.verbose >= 1:
                        print(f"[Checkpoint] Gespeichert: {path}")
        return True


# ─────────────────────────────────────────────────────────────────────────────
# Config-Handling
# ─────────────────────────────────────────────────────────────────────────────

def load_config(config_path: Optional[str] = None) -> dict:
    """Lädt Config aus YAML-Datei oder gibt Default zurück."""
    config = DEFAULT_CONFIG.copy()

    if config_path and Path(config_path).exists():
        with open(config_path, "r") as f:
            user_config = yaml.safe_load(f)
        # Deep merge
        for section, values in user_config.items():
            if section in config and isinstance(config[section], dict):
                config[section].update(values)
            else:
                config[section] = values
        print(f"[Config] Geladen aus: {config_path}")
    else:
        print("[Config] Verwende Default-Konfiguration.")

    return config


def save_config(config: dict, path: str) -> None:
    """Speichert aktuelle Config als YAML."""
    with open(path, "w") as f:
        yaml.dump(config, f, default_flow_style=False)
    print(f"[Config] Gespeichert: {path}")


# ─────────────────────────────────────────────────────────────────────────────
# Training
# ─────────────────────────────────────────────────────────────────────────────

def make_env(config: dict):
    """Factory-Funktion für das Environment (für VecEnv-Kompatibilität)."""
    def _init():
        env = DroneHoverEnv(config)
        env = Monitor(env)
        return env
    return _init


def train(config: dict, resume_path: Optional[str] = None) -> PPO:
    """Startet oder setzt das PPO-Training fort."""

    train_cfg = config["training"]
    log_cfg = config["logging"]
    ckpt_cfg = config["checkpoints"]
    eval_cfg = config["eval"]

    # Environment erstellen
    print("[Training] Erstelle Environment...")
    vec_env = DummyVecEnv([make_env(config)])

    # Modell erstellen oder laden
    if resume_path and Path(resume_path).exists():
        print(f"[Training] Lade Modell aus: {resume_path}")
        model = PPO.load(
            resume_path,
            env=vec_env,
            tensorboard_log=log_cfg["tensorboard_log"],
        )
    else:
        print("[Training] Erstelle neues PPO-Modell...")
        model = PPO(
            policy="MlpPolicy",
            env=vec_env,
            learning_rate=train_cfg["learning_rate"],
            n_steps=train_cfg["n_steps"],
            batch_size=train_cfg["batch_size"],
            n_epochs=train_cfg["n_epochs"],
            gamma=train_cfg["gamma"],
            gae_lambda=train_cfg["gae_lambda"],
            clip_range=train_cfg["clip_range"],
            ent_coef=train_cfg["ent_coef"],
            vf_coef=train_cfg["vf_coef"],
            max_grad_norm=train_cfg["max_grad_norm"],
            target_kl=train_cfg.get("target_kl"),
            verbose=log_cfg["verbose"],
            tensorboard_log=log_cfg["tensorboard_log"],
        )

    # Callbacks
    callbacks = [
        EpisodeLogCallback(verbose=log_cfg["verbose"]),
        CheckpointByEpisodeCallback(
            save_freq_episodes=ckpt_cfg["save_freq"],
            save_path=ckpt_cfg["save_path"],
            name_prefix=ckpt_cfg["name_prefix"],
            verbose=log_cfg["verbose"],
        ),
    ]

    # Eval Environment (optional, gleiche Verbindung — nur für Metriken)
    # Hinweis: Bei Single-Instance-Unity nicht parallel möglich.
    # eval_env = DummyVecEnv([make_env(config)])
    # callbacks.append(EvalCallback(eval_env, ...))

    # Config speichern
    save_config(config, "training_config.yaml")

    print(f"[Training] Starte Training für {train_cfg['total_timesteps']:,} Timesteps...")
    print(f"[Training] TensorBoard: tensorboard --logdir {log_cfg['tensorboard_log']}")

    model.learn(
        total_timesteps=train_cfg["total_timesteps"],
        callback=callbacks,
        reset_num_timesteps=resume_path is None,
        progress_bar=True,
    )

    # Finales Modell speichern
    final_path = Path(ckpt_cfg["save_path"]) / f"{ckpt_cfg['name_prefix']}_final"
    model.save(str(final_path))
    print(f"[Training] Fertig! Finales Modell gespeichert: {final_path}")

    vec_env.close()
    return model


def evaluate(config: dict, model_path: str, n_episodes: int = 10) -> None:
    """Evaluiert ein trainiertes Modell."""
    print(f"[Eval] Lade Modell: {model_path}")

    env = DroneHoverEnv(config)
    model = PPO.load(model_path)

    episode_rewards = []
    episode_lengths = []

    for ep in range(n_episodes):
        obs, _ = env.reset()
        done = False
        ep_reward = 0.0
        ep_steps = 0

        while not done:
            action, _ = model.predict(obs, deterministic=True)
            obs, reward, terminated, truncated, _ = env.step(action)
            done = terminated or truncated
            ep_reward += reward
            ep_steps += 1

        episode_rewards.append(ep_reward)
        episode_lengths.append(ep_steps)
        print(f"  Episode {ep+1:3d}: Reward={ep_reward:8.2f}, Steps={ep_steps:5d}")

    print(f"\n[Eval] Zusammenfassung über {n_episodes} Episoden:")
    print(f"  Mean Reward: {np.mean(episode_rewards):.2f} ± {np.std(episode_rewards):.2f}")
    print(f"  Mean Length: {np.mean(episode_lengths):.1f} Steps")
    print(f"  Best Episode: {max(episode_rewards):.2f}")

    env.close()


# ─────────────────────────────────────────────────────────────────────────────
# CLI Entry Point
# ─────────────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description="SkyForge Drone RL Training — Hover Task"
    )
    parser.add_argument(
        "--config", type=str, default=None,
        help="Pfad zur YAML-Config-Datei"
    )
    parser.add_argument(
        "--eval", action="store_true",
        help="Nur Evaluation (kein Training)"
    )
    parser.add_argument(
        "--model", type=str, default=None,
        help="Pfad zum Modell für Evaluation oder Resume"
    )
    parser.add_argument(
        "--resume", action="store_true",
        help="Training fortsetzen (--model Pfad angeben)"
    )
    parser.add_argument(
        "--episodes", type=int, default=10,
        help="Anzahl Eval-Episoden (nur mit --eval)"
    )
    parser.add_argument(
        "--host", type=str, default=None,
        help="Unity TCP-Host (überschreibt Config)"
    )
    parser.add_argument(
        "--port", type=int, default=None,
        help="Unity TCP-Port (überschreibt Config)"
    )
    args = parser.parse_args()

    # Config laden
    config = load_config(args.config)

    # CLI-Overrides
    if args.host:
        config["env"]["host"] = args.host
    if args.port:
        config["env"]["port"] = args.port

    if args.eval:
        if not args.model:
            parser.error("--eval benötigt --model <pfad>")
        evaluate(config, args.model, n_episodes=args.episodes)
    elif args.resume:
        if not args.model:
            parser.error("--resume benötigt --model <pfad>")
        train(config, resume_path=args.model)
    else:
        train(config, resume_path=None)


if __name__ == "__main__":
    main()
