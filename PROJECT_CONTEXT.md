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

## Quality Gates
- **Build Gate:** Fehlerfreier Compile im Unity Editor
- **Unit Test Gate:** Alle NUnit Tests grün (noch nicht vollständig)
- **Integration Gate:** UDP-Bridging mit Betaflight SITL (`tools/start_sitl.sh`)
- **Render Gate:** GS-Scene >30 FPS bei 4K (manuell)
- **Fly Gate:** 30s Hover Test (Unity + SITL, Telemetrie-Validierung)

## Bekannte Probleme (Stand 2026-04-02)
- **UI Toolkit:** PanelSettings Asset fehlt bei manchen UIDocuments → StartScreen funktioniert, ControllerSetup + HUD teilweise ausgegraut. UIPanelSettingsUtility als Workaround vorhanden.
- **Input:** "Throttle" Input Axis wirft Exception in ControllerSetupController.cs — Legacy Input Manager Mapping fehlt.

## Offene Punkte
- GSScene Performance-Gate automatisieren
- Unit Tests für Kernmodule aufbauen
- `src/SkyForge/` archivieren oder entfernen (alte Kopie)

## Hinweis für Agenten
- Agenten greifen über `assets/Modules/<Modul>/CONTEXT.md` auf Modul-Kontext zu
- Neue Module: immer CONTEXT.md anlegen
- Pfade beziehen sich auf Root (`assets/`), NICHT auf `src/SkyForge/Assets/`

---
*Letzte Aktualisierung: 02.04.2026*
