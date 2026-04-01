# PROJECT_CONTEXT.md – SkyForge (aktualisiert 2026-04-01)

## Projektstruktur
- Root: `/Users/rudi/Projects/SkyForge`
    - `Assets/`                 — Unity Assets (Module, Scripts, UI, Prefabs, Scenes)
    - `Assets/Modules/`         — Hauptmodule der Simulator-Architektur
    - `Assets/Scripts/`         — Legacy und allgemeine Controller-/Manager-Implementierungen
    - `Assets/UI/`              — UI-Komponenten inkl. OSD/Hud
    - `Assets/Editor/`          — Editor-Tools (Commandhandler, Integrator)
    - `Assets/Prefabs/`         — Drone/Stage Prefabs
    - `Assets/Scenes/`          — Haupt- und Test-Szenen
    - `ProjectSettings/`, `Packages/`, `Tools/` wie Unity Standard

## Architektur (Module, Stand 2026-04-01)

### Module mit Kontext-Dokumentation:
- **FlightBridge**   — UDP-Bridge: Unity ↔ Betaflight. Kontext: `FlightBridge/CONTEXT.md`
- **DroneModel**     — Umsetzung eines Quadcopter-Physikmodells. Kontext: `DroneModel/CONTEXT.md`
- **GSScene**        — Gaussian Splatting Scene Loader/Manager. Kontext: `GSScene/CONTEXT.md`
- **ControllerInput**— Liest Gamecontroller, mappt auf BF RCInput. Kontext: `ControllerInput/CONTEXT.md`

### Module ohne Dokumentation (Stand: Sprint 3)
- OSD                — Artificial Horizon, Battery, RSSI (nur Code, kein CONTEXT.md)
- Sensors            — IMU-Simulation (Basis-Code, kein CONTEXT.md)
- (Editor-/Utility: SkyForgeIntegrator, SkyForgeCommandHandler)
- **Fehlend/Platzhalter nur in Config:**
    - InputRouter
    - UIFPV
    - RLTraining

## devForge Integration (v3.3)
- devForge v3.3 (Sprint 3) voll an SkyForge angebunden
- Orchestra-Compaction-Engine & Multi-Layer-Kontext: siehe `src/orchestra-compaction.ts`
- Unity-MCP als Phase: TestArena/SkyForgeMain verwendbar, Telemetrie Validation vordefiniert
- Siehe Configs: `config-v3.3.yaml`, `config-v3.3-skyforge.yaml`

## Quality Gates
- **Build Gate:** Fehlerfreier Compile im Unity Editor
- **Unit Test Gate:** Alle NUnit Tests grün (noch keine vollständigen Tests im Repo)
- **Integration Gate:** UDP-Bridging mit Betaflight SITL (`tools/start_sitl.sh`)
- **Render Gate:** GS-Scene >30 FPS bei 4K (noch manuell zu prüfen)
- **Fly Gate:** 30s Hover Test (Unity+SITL/Telemetry, automatische Validierung per Phase)
- **RL Gate:** (trained >1000 episodes, monotones Reward, noch offen)

## UI-/Toolchain-Probleme (Stand 2026-04-01)
- **UI Toolkit:** Kein PanelSettings Asset zugewiesen → StartScreen funktioniert, ControllerSetup + HUD ausgegraut
- **Input:** "Throttle" Input Axis fehlt, wirft Exception (`ControllerSetupController.cs` Line 217), kein automatischer UI-Fallback/Fehlertest
- **MCP/Toolchain:** unity_scene.sh kennt Schalter --list nicht; unity_console.sh Log-Endpoint umgezogen (404); JSON-RPC/REST API Migration inkonsistent (Legacy/Modern Tools parallel)
- **DevForge v3.3 Status:** Multi-Tool Parallelisierung/Planhygiene + Reasoning Effort/Compaction Engine technisch aktiv, produktiver Einsatz limitiert (vorerst Test-Phasen)

## Offene Punkte
- CONTEXT.md für OSD und Sensors anlegen (Dokumentation fehlt)
- InputRouter/UIFPV/RLTraining Module initialisieren, Dummy-Kontext+Basisarchitektur einfügen
- GSScene Performance-Gate (automatisch) als Sprint 4 Ziel
- UnitTests für alle Kernmodule Sprint 4
- MCP/Toolchain-Skripte aktualisieren (siehe oben)

## Hinweis zu devForge/Codex
- Codex-Agenten greifen ausschließlich über dokumentierte Module/CONTEXT.md auf Spezialfunktionen zu
- Alle hier fehlenden Module/Dokumentationen sind für Subagenten NICHT zugreifbar
- Architektur sicherstellen: Neue Module immer CONTEXT.md anlegen

---
*Letzte Aktualisierung: Maggie, Sprint 3, 01.04.2026*
