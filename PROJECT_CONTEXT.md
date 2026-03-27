# SkyForge — PROJECT_CONTEXT.md
**Aktualisiert:** 27. März 2026 | **DevForge:** v3.3

## Projekt-Übersicht
SkyForge ist der XFLIGHT-eigene Drohnensimulator (macOS M4, Unity 6 LTS). Er kombiniert Betaflight SITL (echte Firmware), Gaussian Splatting (fotorealistische 3D-Welten) und Reinforcement Learning (Maggie lernt fliegen). Zwei Modi: Rudi fliegt manuell (FPV-Training), Maggie fliegt autonom (KI-gesteuert).

## Tech Stack

| Komponente | Technologie | Lizenz |
|---|---|---|
| Game Engine | Unity 6 LTS (Metal, URP) | Free <$200K |
| Flight Controller | Betaflight SITL → SimITL (.dylib) | GPL |
| Betaflight Configurator | `/Applications/Betaflight - Configuration and management application.app` | — |
| GS Renderer | UnityGaussianSplatting (Aras-P) | MIT |
| RL Framework | Stable-Baselines3 (PPO) + ML-Agents | MIT/Apache |
| Architektur | Modularer Monolith (Unity Host) | — |
| Hardware | Mac Mini M4 Pro (24GB) | — |

## Aktuelle Phase
**Phase 1A: GS-Datenquellen evaluieren** — Abgeschlossen.
- Erfolgreiche Quelle: mkkellogg GitHub (direkter .ksplat Download)
- Verfügbare Szenen: bonsai, garden, stump, truck (inkl. _high Varianten)
- Format: .ksplat (Unity-kompatibel)
- Pfade: /Users/rudi/Projects/SkyForge/assets/mkkellogg_gs/[scene]/[file].ksplat

**Nächster Schritt: Phase 1B — RunPod-Setup für Performance-Benchmarks**

## Module

| Modul | Pfad | Sprache | DevForge Modell |
|---|---|---|---|
| GS Scene Manager | Assets/Modules/GSScene | C# + Metal | Codex (Free) |
| Flight Dynamics Bridge | Assets/Modules/FlightBridge | C# + C | Claude Code |
| RL Training Engine | Assets/Modules/RLTraining | Python + C# | Codex (Free) |
| Betaflight SITL | betaflight/ | C | Claude Code |
| Input Router | Assets/Modules/InputRouter | C# | Codex (Free) |
| UI / FPV Camera | Assets/Modules/UIFPV | C# + UXML | Codex (Free) |

## DevForge v3.3 Modell-Mapping

| Task-Typ | Primär | Fallback |
|---|---|---|
| Unity C# | Codex (Free) | Qwen3-Coder-480B |
| BF Integration | Claude Code | Nemotron 120B |
| GS Shader | Codex (Free) | Qwen3-Coder-480B |
| RL/ML Code | Codex (Free) | Nemotron 120B |
| Architektur | Nemotron 120B | DeepSeek-R1 |
| Code Review | Qwen3-Coder-480B | Claude Code |
| Bugfixing | Claude Code | Codex + Nemotron |
| UI/UX | Codex (Free) | Qwen3-30B |

## Quality Gates

1. **G1 Build:** Unity kompiliert fehlerfrei (Auto)
2. **G2 Test:** NUnit Tests grün (Auto)
3. **G3 Integration:** BF SITL antwortet auf FDM-Packets (Semi-Auto)
4. **G4 Render:** GS rendert >30 FPS ohne Artefakte (Rudi visuell)
5. **G5 Fly:** Quad schwebt stabil <0.5m Error (Auto)
6. **G6 RL:** Reward steigt monoton über 1000 Episodes (Auto)

## Wichtige Links

- [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) — GS Plugin
- [Betaflight SITL](https://www.betaflight.com/docs/development/autopilot/SITL_Autopilot_Testing_Gazebo) — BF Simulation Docs
- [ML-Agents](https://github.com/Unity-Technologies/ml-agents) — Unity RL Framework
- [Stable-Baselines3](https://stable-baselines3.readthedocs.io/) — PPO/SAC Implementierungen
- [Rubble 4K Dataset](https://huggingface.co/datasets/HexuZhao/mega_nerf_rubble_colmap) — GS Testszene
- [SimITL/pr0p](https://github.com/pr0p) — BF als Shared Library Referenz
- [Swift RL Drone (UZH)](https://rpg.ifi.uzh.ch/AgileAutonomy.html) — RL Drohnen-Referenz
- [GauU-Scene Dataset](https://saliteta.github.io/CUHKSZ_SMBU) — Große urbane GS-Szenen

## Nächste Schritte

1. [ ] Unity 6 LTS installieren + Projekt anlegen
2. [ ] UnityGaussianSplatting klonen + Rubble 4K laden
3. [ ] GS-Szene auf M4 rendern, Performance messen
4. [ ] Betaflight SITL auf macOS kompilieren (PR #14284)
5. [ ] UDP-Bridge Prototyp (Unity ↔ BF SITL)

## Pfade

- **Projekt:** `/Users/rudi/Projects/SkyForge`
- **Unity Projekt:** `/Users/rudi/Projects/SkyForge/src/SkyForge/`
- **DevForge Config:** `/Users/rudi/DevForge/config-v3.3-skyforge.yaml`
- **Architektur-Plan:** `/Users/rudi/.openclaw/workspace/docs/2025-03-27-drone-sim-architektur-plan.md`
- **BF Research:** `/Users/rudi/.openclaw/workspace/docs/2025-03-27-drone-sim-betaflight-ardupilot-research.md`
- **GS Research:** `/Users/rudi/.openclaw/workspace/docs/2025-03-27-drone-sim-gaussian-splat-research.md`
- **RL Research:** `/Users/rudi/.openclaw/workspace/docs/2025-03-27-drone-sim-rl-training-research.md`
