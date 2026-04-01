# PROJECT_CONTEXT.md – SkyForge (aktualisiert 2026-04-02)

## Projektstruktur
- Root: `/Users/rudi/Projects/SkyForge` — kanonisches Unity-Projekt (Assets-Ordner = `assets/`)
- `assets/Modules/`         — Hauptmodule der Simulator-Architektur (je Modul ein CONTEXT.md)
- `assets/Scripts/`         — Controller, Manager, MCP-Client
- `assets/Scripts/MCP/`     — MCP JSON-RPC Client (MCPClient.cs, MiniJson.cs)
- `assets/Scripts/UI/`      — UI Controller (StartScreen, HUD, ControllerSetup, UIManager)
- `assets/UI/`              — UI-Komponenten (Toolkit UXML/USS, Scenes, Shared Theme)
- `assets/Editor/`          — Editor-Tools (SkyForgeIntegrator, SkyForgeCommandHandler, ControllerDebugWindow, SuppressInputWarning)
- `assets/Prefabs/`         — Drone/Stage Prefabs
- `assets/Scenes/`          — Haupt- und Test-Szenen (SkyForgeMain, MeineScene, TestNachBugs, u.a.)
- `assets/Configs/`         — ScriptableObject Configs (BridgeConfig, ControllerConfig, DroneConfig)
- `assets/Rendering/`       — Custom Render Features (DroneOverlayFeature)
- `assets/Resources/`       — Runtime-ladbare Assets (OSDData, UI Styles, PanelSettings)
- `tools/`                  — SITL/Build-Skripte (start_sitl.sh, stop_sitl.sh, test_integration.sh, unity_batch.sh)
- `ProjectSettings/`, `Packages/` — Unity Standard

### Hinweis zur Verzeichnisstruktur
`src/SkyForge/Assets/` ist der alte, git-getrackte Quellpfad. Seit Sprint 3 ist `assets/` (Root) das aktive Unity-Projekt.
`src/SkyForge/` enthält ältere Versionen und wird nicht mehr aktiv genutzt. Konsolidierung nach Root ist abgeschlossen.

## Architektur (Module)

Alle Module liegen unter `assets/Modules/`. Jedes Modul hat ein `CONTEXT.md` für Agenten-Zugriff.

| Modul | Beschreibung | CONTEXT.md | Quelldateien |
|-------|-------------|------------|-------------|
| **FlightBridge** | UDP-Bridge: Unity ↔ Betaflight SITL | Ja | BridgeConfig.cs, CoordinateConverter.cs, FDMPacket.cs, FlightDynamicsBridge.cs |
| **DroneModel** | Quadcopter-Physikmodell (Motoren, Propeller, FPV-Kamera) | Ja | DroneConfig.cs, DroneController.cs, DroneSetup.cs, FPVCamera.cs, LEDThrustIndicator.cs, MotorModel.cs, PlaceholderMesh.cs, PropellerRotation.cs |
| **ControllerInput** | Gamecontroller → Betaflight RC-Kanäle | Ja | ControllerConfig.cs, RCPacket.cs |
| **GSScene** | Gaussian Splatting Scene Loader/Manager | Ja | (nur Kontext-Doku, Code in Plugins) |
| **OSD** | On-Screen-Display: Artificial Horizon, Batterie, RSSI via MSP | Ja | MSPClient.cs, OSDController.cs, OSDData.cs, OSDOverlay.uss/.uxml |
| **Sensors** | IMU-Simulation (Gyro, Accelerometer, Barometer) | Ja | IMUSimulator.cs |
| **UIPanelSettingsUtility** | Fallback-Mechanismus für fehlende PanelSettings Assets | Ja | (Utility, kein eigenständiges Modul) |

## devForge Integration (v3.3)

SkyForge ist als devForge-Projekt registriert mit eigener SkyForge-optimierter Config.

- **Allgemeine Config:** `~/DevForge/config-v3.3.yaml` (Parallelisierungs-Policy, Reasoning Effort, Plan-Hygiene)
- **SkyForge-Config:** `~/DevForge/config-v3.3-skyforge.yaml` (Modell-Matrix, Quality Gates, Claude Code Integration)
- **Prompt-Template:** `~/DevForge/prompts/codex-impl-v3.3.md`
- **Compaction Engine:** `~/DevForge/src/orchestra-compaction.ts` (Prototyp)
- **Optimierungsdoku:** `~/.openclaw/workspace/docs/2026-04-01-opus-devforge-optimization.md`

### Dual-Modus Workflow
- **ACP (Claude Code):** Für system-nahe Mono-Tasks (FlightBridge, Betaflight SITL, Cross-Module Bugfixing)
- **Orchestra (Subagents):** Für parallele Tasks (Codex → C#/Python, Nemotron → Architektur, Qwen3 → QA)

### Modell-Matrix (8 Task-Typen)
| Task-Typ | Primär | Fallback |
|----------|--------|----------|
| Unity C# (komplex) | gpt-5-codex (Free) | Qwen3-Coder |
| Betaflight/Native | Claude Code | Nemotron |
| GS Rendering/Shader | gpt-5-codex | Qwen3-Coder |
| RL/ML Training | gpt-5-codex | Nemotron |
| Architektur | Nemotron | DeepSeek-R1 |
| Code Review | Qwen3-Coder | Claude Code |
| Bugfixing (Cross-Module) | Claude Code | gpt-5-codex |
| UI/UX | gpt-5-codex | Qwen3-30b |

### Unity-MCP Skill
OpenClaw-Skill für Unity-Steuerung: `~/.openclaw/workspace/skills/unity-mcp/`
- Shell-Scripts: `unity_status.sh`, `unity_command.sh`, `unity_scene.sh`, `unity_console.sh`
- Custom C# Tools: `SkyForgeTelemetryTool.cs`, `SITLStatusTool.cs`
- Geplant als automatische Test-Phase in devForge-Workflow (Szene laden → Play → Telemetrie → Validierung)

## Quality Gates
- **G1 Build:** Fehlerfreier Compile im Unity Editor
- **G2 Unit Test:** Alle NUnit Tests grün (noch nicht vollständig)
- **G3 Integration:** UDP-Bridging mit Betaflight SITL (`tools/start_sitl.sh`)
- **G4 Render:** GS-Scene >30 FPS bei 4K (manuell, Rudi bestätigt)
- **G5 Fly:** Quad schwebt stabil (Position Error < 0.5m über 10s)
- **G6 RL:** Training-Reward steigt monoton über 1000 Episodes (noch offen)

## Bekannte Probleme (Stand 2026-04-02)
- **UI Toolkit:** PanelSettings Asset fehlt bei manchen UIDocuments → StartScreen funktioniert, ControllerSetup + HUD teilweise ausgegraut. UIPanelSettingsUtility als Workaround vorhanden.
- **Input:** "Throttle" Input Axis wirft Exception in ControllerSetupController.cs — Legacy Input Manager Mapping fehlt.

## Offene Punkte
- GSScene Performance-Gate automatisieren (G4)
- Unit Tests für Kernmodule aufbauen (G2)
- Geplante Module anlegen: RLTraining, InputRouter, UIFPV (jeweils mit CONTEXT.md)
- `src/SkyForge/` archivieren oder entfernen (alte Kopie)
- Unity-MCP als devForge Test-Phase integrieren (Sprint 2)

## Hinweis für Agenten
- Agenten greifen über `assets/Modules/<Modul>/CONTEXT.md` auf Modul-Kontext zu
- Neue Module: immer CONTEXT.md anlegen
- Pfade beziehen sich auf Root (`assets/`), NICHT auf `src/SkyForge/Assets/`
- devForge-Configs liegen in `~/DevForge/`, NICHT im SkyForge-Repo

---
*Letzte Aktualisierung: 02.04.2026*
