# RLTraining Module — CONTEXT.md
**Modul:** RLTraining
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/RLTraining
**Beschreibung:** Reinforcement-Learning-Modul für autonomes Drohnenfliegen. Trainiert Agenten via PPO (Stable-Baselines3) in der SkyForge Unity-Simulation. Kommunikation über Unity ML-Agents oder eine eigene UDP/SharedMemory-Bridge.

## Architektur

### Unity-seitig (C#)
- **RLEnvironment.cs** (geplant)
  - MonoBehaviour als Wrapper um den Drohnen-Simulator
  - Implementiert `Reset()` → Drohne auf Startposition, Motoren idle
  - Implementiert `Step(action)` → Wendet Motorkommandos an, wartet FixedUpdate
  - Implementiert `GetObservation()` → Sammelt Sensorwerte als float-Array
  - Implementiert `GetReward()` → Berechnet Reward basierend auf Flugzustand
  - Erkennt `done`-Bedingungen (Crash, Out-of-Bounds, Timeout)

- **RLBridge.cs** (geplant)
  - Kommunikationsschicht zwischen Python-Training und Unity
  - Option A: Unity ML-Agents SDK (com.unity.ml-agents) — bevorzugt
  - Option B: Custom UDP-Bridge (Port 9010/9011)
  - Empfängt Actions, sendet Observations + Reward + Done

### Python-seitig
- **train_hover.py** — PPO-Training mit Stable-Baselines3
  - Nutzt `UnityEnvironment` (ML-Agents) oder Custom `gym.Env` Wrapper
  - Observation Space: Position, Rotation, Velocity, Angular Velocity, Motorstatus
  - Action Space: 4× Motor-PWM (Continuous, Box[-1,1])
  - Reward: Positions-Fehler + Orientierungs-Fehler + Energieverbrauch
  - Hyperparameter: lr=3e-4, n_steps=2048, batch_size=64, n_epochs=10

- **eval_hover.py** — Evaluierung eines trainierten Modells
- **reward_functions.py** — Modulare Reward-Definitionen

## Observation Space (18-dim float)
| Index | Wert | Bereich | Quelle |
|-------|------|---------|--------|
| 0-2 | Position (x,y,z) relativ zum Ziel | [-50, 50] m | DroneController |
| 3-5 | Velocity (vx,vy,vz) | [-20, 20] m/s | DroneController |
| 6-8 | Rotation (roll, pitch, yaw) | [-π, π] rad | DroneController |
| 9-11 | Angular Velocity (ωx, ωy, ωz) | [-10, 10] rad/s | IMUSimulator |
| 12-15 | Motor-PWM (4×) normalisiert | [0, 1] | MotorModel |
| 16 | Höhe über Grund | [0, 100] m | DroneController |
| 17 | Distanz zum Ziel | [0, 50] m | berechnet |

## Action Space (4-dim continuous)
| Index | Wert | Bereich | Beschreibung |
|-------|------|---------|-------------|
| 0 | Motor FL | [-1, 1] | Front-Left PWM delta |
| 1 | Motor FR | [-1, 1] | Front-Right PWM delta |
| 2 | Motor BL | [-1, 1] | Back-Left PWM delta |
| 3 | Motor BR | [-1, 1] | Back-Right PWM delta |

## Reward-Funktion (Hover-Task)
```
reward = -α * position_error      # Halte Position (α=1.0)
         -β * orientation_error    # Halte Level (β=0.5)
         -γ * velocity_penalty     # Minimiere Drift (γ=0.1)
         -δ * energy_penalty       # Minimiere Energieverbrauch (δ=0.01)
         + ε * alive_bonus         # Bonus fürs Überleben (ε=0.1)
         - crash_penalty           # -100 bei Crash
```

## Dependencies
- **Unity:** DroneController, IMUSimulator, MotorModel, FlightDynamicsBridge
- **Python:** stable-baselines3, gymnasium, mlagents (>=1.0), torch, numpy
- **Kommunikation:** Unity ML-Agents SDK oder Custom UDP Bridge

## Quality Gate
- **G6 (RL Gate):** Training-Reward steigt monoton über 1000 Episodes

## Status
- **Phase:** Implementiert (Sprint 2026-04-11)
- Observation/Action Space definiert und implementiert
- Reward-Funktion implementiert
- **DroneRLEnvironment.cs** — vollständig implementiert (TCP-Socket, JSON-Protokoll, 18-dim Obs)
- **train_hover.py** — vollständig (Gymnasium DroneHoverEnv, SB3 PPO, TensorBoard, Checkpoints)
- **requirements.txt** — Python-Abhängigkeiten dokumentiert
- Unit Tests in `assets/Tests/Editor/RLEnvironmentTests.cs` vorhanden

## Neue Dateien (2026-04-11)
| Datei | Beschreibung |
|-------|--------------|
| `Scripts/DroneRLEnvironment.cs` | Unity MonoBehaviour — TCP-Server, Obs/Action/Reward/Done |
| `rl_training/train_hover.py` | Python PPO-Training mit Gymnasium + SB3 |
| `rl_training/requirements.txt` | Python-Abhängigkeiten |

## TCP-Protokoll (Port 9020)
```
Python → Unity: {"cmd": "reset"}
Python → Unity: {"cmd": "step", "action": [a0,a1,a2,a3]}
Python → Unity: {"cmd": "ping"}

Unity → Python: {"type": "reset", "obs": [...18 floats...]}
Unity → Python: {"type": "step", "obs": [...], "reward": float, "done": bool}
Unity → Python: {"type": "pong"}
```

## Nächste Schritte
- [ ] Unity in Play-Mode starten, dann `python train_hover.py` ausführen
- [ ] Hyperparameter-Tuning für stabilen Hover
- [ ] RLBridge.cs als InputRouter-Quelle (RLAgentInputSource)
- [ ] Curriculum Learning: Hover → Waypoint → Acro
- [ ] G6 Gate: Reward steigt monoton über 1000 Episodes
