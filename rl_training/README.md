# SkyForge RL Training

## Requirements

```bash
pip install stable-baselines3 gymnasium numpy torch tensorboard
pip install mlagents-envs  # optional, for Unity ML-Agents connection
```

## Quick Start

### Train (with mock physics)
```bash
cd /Users/rudi/Projects/SkyForge/rl_training
python train_hover.py train --timesteps 500000 --n-envs 4
```

### Evaluate
```bash
python train_hover.py eval --model ./models/hover/best/best_model.zip
```

### TensorBoard
```bash
tensorboard --logdir ./models/hover/logs
```

## Architecture

- `train_hover.py` — Full PPO training pipeline with mock physics
- Unity ML-Agents connection — TODO (see `_unity_reset()` / `_unity_step()`)

## Mock vs Unity

The mock physics in `SkyForgeHoverEnv` provides a simplified quadcopter model
for testing the training pipeline without Unity running. Once the Unity-side
`RLEnvironment.cs` is implemented, the environment will connect via ML-Agents.

## Observation Space (18-dim)
| Index | Value | Range |
|-------|-------|-------|
| 0-2 | Position rel. to target | [-50, 50] m |
| 3-5 | Velocity | [-20, 20] m/s |
| 6-8 | Rotation (RPY) | [-π, π] rad |
| 9-11 | Angular velocity | [-10, 10] rad/s |
| 12-15 | Motor PWM (4x) | [0, 1] |
| 16 | Height AGL | [0, 100] m |
| 17 | Distance to target | [0, 50] m |

## Action Space (4-dim continuous)
| Index | Value | Range |
|-------|-------|-------|
| 0-3 | Motor PWM deltas | [-1, 1] |
