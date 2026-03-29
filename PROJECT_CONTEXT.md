# SkyForge — PROJECT_CONTEXT.md
**Aktualisiert:** 29. März 2026, 21:15 | **DevForge:** v3.3

## Projekt-Übersicht
SkyForge ist der XFLIGHT-eigene Drohnensimulator (macOS M4, Unity 6 LTS). Er kombiniert Betaflight SITL (echte Firmware), Gaussian Splatting (fotorealistische 3D-Welten) und Reinforcement Learning (Maggie lernt fliegen). Zwei Modi: Rudi fliegt manuell (FPV-Training), Maggie fliegt autonom (KI-gesteuert).

## Tech Stack

| Komponente | Technologie | Lizenz |
|---|---|---|
| Game Engine | Unity 6 LTS (Metal, URP) | Free <$200K |
| Flight Controller | Betaflight SITL 2026.6.0-alpha | GPL |
| Betaflight Configurator | `/Applications/Betaflight - Configuration and management application.app` | — |
| GS Renderer | UnityGaussianSplatting (Aras-P) | MIT |
| RL Framework | Stable-Baselines3 (PPO) + ML-Agents | MIT/Apache |
| Architektur | Modularer Monolith (Unity Host) | — |
| Hardware | Mac Mini M4 Pro (24GB) | — |
| Backup-Hardware | M1 Max + 32 GB RAM | — |
| Controller | RadioMaster TX16S (EdgeTX, USB Joystick) | — |

## Session-Log 28. März 2026

### Welle 1 — GS Rendering + Performance ✅
- GaussianSplatURPFeature fehlte im PC_Renderer → manuell hinzugefügt
- Input System auf "Both" umgestellt (Unity 6 Default = nur New Input System)
- FlyCamera.cs erstellt für Game-Modus Navigation (WASD + Maus)
- **Performance train_30k (~1M Splats): 57-62 FPS** ✅
- **Performance garden_30k (~5-6M Splats): 22-30+ FPS** ✅
- 13 offizielle 3DGS Maps importiert (16 GB total)

### Welle 2 — BF SITL + Bridge + Drone + Controller ✅
- **Paket A:** Betaflight SITL 2026.6.0-alpha nativ kompiliert (macOS ARM64) ✅
  - Binary: betaflight/obj/main/betaflight_SITL.elf (333 KB)
  - Ports: UDP 9001-9004 + TCP 5761 (MSP)
  - Start-Script: tools/start_sitl.sh
- **Paket B:** FlightDynamicsBridge erstellt (UDP Client/Server, NED↔Unity Transform) ✅
  - Pfad: Assets/Modules/FlightBridge/
  - Dateien: FlightDynamicsBridge.cs, FDMPacket.cs, CoordinateConverter.cs, BridgeConfig.cs
- **Paket C:** DroneModel erstellt (Rigidbody, 4 Motoren, FPV Camera) ✅
  - Pfad: Assets/Modules/DroneModel/
  - Dateien: DroneController.cs, MotorModel.cs, DroneConfig.cs, DroneSetup.cs, FPVCamera.cs, PlaceholderMesh.cs
- **Controller:** RadioMaster TX16S erkannt als USB Joystick ✅
  - Pfad: Assets/Modules/ControllerInput/
  - Dateien: RCInputBridge.cs, ControllerConfig.cs, RCPacket.cs

## Session-Log 29. März 2026

### Welle 2 Abschluss — DevForge Loop ✅
Kompletter DevForge-Loop: Architekt (qwen3 235B, 30s) → Implementierer A+B parallel (qwen3-coder, ~1m30s) → QA (qwen3-small, 1m40s)

- **SkyForgeIntegrator.cs** — Ein-Klick Scene Setup: SkyForge → Setup Drone in Scene ✅
  - Erstellt automatisch: Drone, FlightDynamicsBridge, RCInputBridge, CameraManager, GroundPlane, HUD, ControllerConfigPanel
  - Konfiguriert alle Referenzen + ScriptableObject Assets
  - Auto-Save der Scene nach Setup
  - Commit: `5c7bd3d`

- **SITL Tools** — Shell Scripts für Betaflight SITL ✅
  - `tools/start_sitl.sh` — Startet SITL, wartet auf Ports, zeigt Verbindungsinfo
  - `tools/stop_sitl.sh` — Stoppt SITL sauber
  - `tools/test_integration.sh` — Testet UDP-Verbindung
  - `tools/arm_sitl.sh` — Armed SITL via MSP

- **CameraManager.cs** — Single-Camera Ansatz (Main Camera für alle Modi) ✅
  - F1 = FreeCam (WASD + Maus via FlyCamera)
  - F2 = FPV (auf der Drohne, 120° FOV)
  - F3 = Third Person (Chase Cam, Offset 0/2/-5)
  - Tab = Cycle Cameras
  - Commit: `c4fd920`

- **Port-Fix** — UDP Ports waren vertauscht ✅
  - 9002 = Unity empfängt PWM von SITL
  - 9003 = Unity sendet FDM an SITL

- **Ground Plane** — Unsichtbarer Collider auf Y=0 (1000m x 1000m) ✅
  - Drohne fällt nicht mehr ins Unendliche
  - Commit: `457ae03`

### Welle 3 — UI & Usability ✅ (Code fertig, Testing läuft)

- **Controller-Konfigurator (F4)** — Runtime OnGUI Panel ✅
  - Pfad: `Assets/UI/ControllerConfigPanel.cs`
  - Kanal-Zuordnung (RC-Channel 0-15 pro Achse)
  - Invert-Toggle pro Achse
  - Deadzone-Slider (0-0.5)
  - Expo-Slider (0-1)
  - Live PWM-Werte von RCInputBridge
  - JSON Save/Load (`Application.persistentDataPath/ControllerMapping.json`)
  - Verschiebbares GUILayout.Window

- **HUD Overlay** — Flugdaten rechts unten ✅
  - Pfad: `Assets/UI/HudOverlay.cs`
  - ALT (Höhe), SPD (Geschwindigkeit), MODE (Kameramodus)
  - ARM (Armed/Disarmed basierend auf motorPWM)
  - BAT (Batterie-Simulation, sinkt wenn armed)
  - SITL Connection Status
  - Grüne Pilot-HUD Optik
  - Debug-Label "HUD ACTIVE" oben links (Magenta)

- **LED Thrust Indicator** — Motor-Farben nach Thrust ✅
  - Pfad: `Assets/Modules/DroneModel/LEDThrustIndicator.cs`
  - Grün (idle) → Gelb (mittel) → Rot (Vollgas)
  - MaterialPropertyBlock für Performance (_BaseColor)

- **Propeller Rotation** — Drehen proportional zum PWM ✅
  - Pfad: `Assets/Modules/DroneModel/PropellerRotation.cs`
  - CW/CCW je nach Motor-Position
  - Flache Cylinder als Propeller-Disc

- **DroneOverlay Shader:** `ZTest Always` + Overlay Queue (4000) ⚠️ Testing
  - DroneOverlayFeature.cs: RasterRenderPass + DrawRendererList implementiert ✅
  - renderPassEvent: AfterRenderingPostProcessing
  - Visueller Test noch ausstehend

### Welle 3 Commits
| Commit | Beschreibung |
|--------|-------------|
| `5c7bd3d` | Welle 2 Integration: SkyForgeIntegrator + SITL Tools |
| `c4fd920` | Fix: SkyForgeIntegrator fpvCamera Referenz entfernt |
| `457ae03` | Feature: Unsichtbarer Ground Plane Collider |
| `73e4563` | Welle 3: Controller-Konfigurator + HUD + LED/Propeller + Opaque Fix |
| `5da3db3` | Fix: Drohne renderQueue 3100 (Versuch 2) |
| `677f9d6` | Fix: DroneOverlay shader (ZTest Always, Queue Overlay) + HUD debug |

### Offene Probleme
- **Drohne unsichtbar in Third Person View** — DroneOverlay Shader noch nicht bestätigt funktionierend
  - Root Cause: GaussianComposite.shader Fullscreen-Blit überschreibt normales Rendering
  - Nächster Versuch: Custom ScriptableRendererFeature die Drohne AfterRenderingTransparents zeichnet
- **Controller-Setup UI** — funktional aber nicht intuitiv genug
  - Recherche läuft: Wie machen Liftoff, VelociDrone, DRL das Controller-Setup?
  - Geplant: Auto-Detect (Stick bewegen → Achse erkannt), Wizard-basiertes Setup
- **TX16S Batterie leer** — Controller-Test morgen

### Betaflight Konfigurator Verbindung
- SITL starten: `tools/start_sitl.sh`
- BF Configurator: **Manual Connection → tcp://127.0.0.1:5761**
- Receiver Tab: Kanal-Zuordnung live verifizieren
- Modes Tab: Arm-Switch, Flugmodi einstellen

## Fixes & Learnings (Welle 1+2)
- MCP: .mcp.json Config muss `uvx --from mcpforunityserver` nutzen, nicht `node`
- .gitignore: `/assets/` statt `assets/` — sonst werden Unity Assets/ Ordner ausgeschlossen
- DroneController Properties: `new` keyword reicht nicht — Properties umbenannt zu `Current*` Prefix
- Input System: "Both" Mode bleibt aktiv (Old für FlyCamera, New für RCInputBridge)
- DroneController.cs: `rb.linearAcceleration` existiert nicht → manuell berechnet
- RCInputBridge: `Gamepad.current` findet TX16S nicht → Fallback auf `Joystick.current`
- RCInputBridge: `enabled = false` in OnEnable verursacht Config-Verlust → Lazy Init
- RCInputBridge: UDP Send Error rate-limited auf alle 5s
- Nemotron (Nebius): reasoning=true verursacht leeren content → reasoning=false
- Unity 6: `FindObjectOfType<T>()` deprecated → `FindFirstObjectByType<T>()`
- Unity 6: `rb.velocity` deprecated → `rb.linearVelocity`
- GaussianComposite Shader: `ZTest Always` + `Blend SrcAlpha OneMinusSrcAlpha` = überschreibt alles
- renderQueue allein reicht NICHT gegen GS Composite (RenderGraph Unsafe Pass)

## Session-Log 30. März 2026 (Night Build)

### Welle 4 — UI Toolkit UXML/USS ✅
- **Start-Screen** (UXML/USS) — Karten-Grid mit 13 Maps, Fly/Setup/Settings/Quit Buttons
- **Controller-Setup Wizard** (UXML/USS) — 4-Schritt Auto-Detect (Throttle→Yaw→Pitch→Roll)
- **HUD** (UXML/USS) — ALT/SPD/MODE/ARMED/BAT im Piloten-HUD Stil
- **Controller-Scripts:** StartScreenController, ControllerSetupController, HUDController
- XFLIGHT Corporate Design: Dunkel (#1a1a2e), Blau-Akzent (#0066ff)
- Compile-Fix: VisualAsset→VisualTreeAsset, Doppelter UIManager entfernt
- Commit: `d523e54`

### DroneOverlayFeature Fix ✅
- DroneOverlayFeature.cs komplett neu implementiert mit Unity 6 RenderGraph API
- RasterRenderPass statt UnsafePass, DrawRendererList statt auskommentiertem TODO
- renderPassEvent: AfterRenderingPostProcessing (nach GS Composite)
- SortingSettings API-Fix für Unity 6
- Commit: `abbfa23`

### Betaflight OSD Overlay ✅
- MSPClient.cs: TCP-Verbindung zu SITL (Port 5761), MSP v1 Protokoll, Auto-Reconnect
- OSDData.cs: Parser für Batterie, Höhe, Flugmodus, Armed-Status, RSSI, Timer
- OSDOverlay (UXML/USS): Betaflight-Stil Overlay, Monospace, semi-transparent
- Toggle mit F5, nur im FPV-Modus (F2) sichtbar
- Commit: `f3b374d`

### FPV-Simulator UI-Recherche ✅
- Analyse von Liftoff, VelociDrone, DRL Simulator, Uncrashed
- Best Practices: Controller-Setup, Hauptmenü, Map-Auswahl, HUD
- Konkrete Empfehlungen für SkyForge UI
- Report: `docs/2026-03-30-fpv-sim-ui-recherche.md`

## Nächste Schritte (30. März 2026)

### Phase B — Erster Flug
1. [ ] TX16S aufladen und anschließen
2. [ ] SITL starten → BF Configurator verbinden (TCP 5761)
3. [ ] Receiver Tab: Kanal-Zuordnung verifizieren
4. [ ] F4 in Unity: Kanäle ggf. umordnen
5. [ ] Drohne armen + erster Flug testen

### Drohne sichtbar machen (Prio 1)
6. [x] DroneOverlayFeature korrekt implementiert (RasterRenderPass + DrawRendererList)
7. [x] Kompiliert fehlerfrei — Visueller Test steht noch aus (F3 Third Person)
8. [ ] Falls nicht sichtbar: renderPassEvent anpassen oder GS Feature Injection Order prüfen

### Controller-UI Verbesserung
9. [x] FPV-Simulator UI Recherche auswerten (Liftoff, VelociDrone, DRL) ✅
10. [x] Auto-Detect Wizard: "Bewege Throttle-Stick" → Achse erkannt ✅ (Welle 4)
11. [x] Start-Menü mit Karten-Auswahl, Drohnen-Config, Controller-Setup ✅ (Welle 4)
12. [ ] Bessere Menü-Struktur (scrollbar, passt in Bildschirm) — Verbesserungen aus UI-Recherche anwenden

### Welle 4 — Advanced Features
- [x] Betaflight OSD Overlay (MSP-Daten) ✅
- [ ] Replay-System
- [ ] Kollisions-Proxy aus Splat-Positionen
- [ ] RL Training Grundlagen (ML-Agents Integration)

## Neue Commits (29.03. Abend + Nacht)
- f4aaa17: fix: compile errors (SerializedObject pattern, deprecated API, InputSystem buffer)
- eaf5901: feat: UI Toolkit menu system v1 (MainMenu, ControllerSetup, MapSelection, About)
- 81fd2f9: feat: DroneOverlayFeature v1
- d523e54: Welle 4 UI Toolkit (StartScreen, Controller-Wizard, HUD)
- abbfa23: Fix DroneOverlayFeature RenderGraph
- f3b374d: Betaflight OSD Overlay (MSPClient, OSDData, Renderer)
- fbb6518: UI Toolkit Refactoring (StartScreen, ControllerSetup, HUD, UIManager)
- 69279be: betaflight submodule als 'beta' eingebunden
- 68f22a1: Kernkomponenten reimplementiert (DroneController, FlightDynamicsBridge, RCInputBridge)

## SITL Status
- Binary jetzt unter beta/obj/main/betaflight_SITL.elf (nicht mehr betaflight/)
- BF Modes: ARM=AUX2 (rechter DJI Kippschalter), AirMode=AUX1-M, Angle=AUX1-N
- API Version 1.48, BF Configurator 10.10 verbindet sich

## UI-System
- 2 parallele Versionen erstellt: Assets/UI/Scenes/ und Assets/UI/Toolkit/
- Noch NICHT in aktive Szene eingebunden (UIDocument + UXML muss zugewiesen werden)
- Konsolidierung nötig

## DroneOverlayFeature
- Zum PC_Renderer.asset manuell hinzugefügt
- Noch nicht visuell verifiziert

## Offene Punkte für 30.03.
- UI in Szene einbinden
- Drohne sichtbar verifizieren
- Erster Flugtest
- Physik-Simulation: Hohe Update-Rate für Flight Controller (FixedUpdate entkoppelt von Framerate)
- Redundante UI-Dateien konsolidieren

## Module

| Modul | Pfad | Status |
|---|---|---|
| GS Renderer | Assets/ (Aras-P Plugin) | ✅ Funktioniert |
| Flight Dynamics Bridge | Assets/Modules/FlightBridge/ | ✅ Code fertig |
| Drone Model | Assets/Modules/DroneModel/ | ✅ Code fertig |
| Controller Input | Assets/Modules/ControllerInput/ | ✅ TX16S erkannt |
| Betaflight SITL | betaflight/ | ✅ Kompiliert + läuft |
| FlyCamera | Assets/Scripts/FlyCamera.cs | ✅ Funktioniert |
| CameraManager | Assets/Scripts/CameraManager.cs | ✅ F1/F2/F3/Tab |
| SkyForgeIntegrator | Assets/Editor/SkyForgeIntegrator.cs | ✅ Ein-Klick Setup |
| HUD Overlay | Assets/UI/HudOverlay.cs | ✅ Funktioniert |
| Controller Config | Assets/UI/ControllerConfigPanel.cs | ✅ F4 Panel |
| LED Indicator | Assets/Modules/DroneModel/LEDThrustIndicator.cs | ✅ Code fertig |
| Propeller Rotation | Assets/Modules/DroneModel/PropellerRotation.cs | ✅ Code fertig |
| OSD Module | Assets/Modules/OSD/ | ✅ Code fertig |
| DroneOverlay Shader | Assets/Shaders/DroneOverlay.shader | ✅ Implementiert |
| MCP Integration | .mcp.json + Packages/manifest.json | ✅ Plugin installiert |
| Editor Commands | Assets/Editor/SkyForgeCommandHandler.cs | ✅ 6 Befehle |
| Batch Runner | tools/unity_batch.sh | ✅ CLI-Steuerung |
| SITL Tools | tools/start_sitl.sh + stop/test/arm | ✅ Funktioniert |
| RL Training Engine | Assets/Modules/RLTraining/ | ⏳ Phase 2 |

## DevForge v3.3 Modell-Mapping

| Task-Typ | Primär | Fallback |
|---|---|---|
| Unity C# | Codex (Free) | Qwen3-Coder-480B |
| BF Integration | Claude Code | Nemotron 120B |
| GS Shader | Codex (Free) | Qwen3-Coder-480B |
| RL/ML Code | Codex (Free) | Nemotron 120B |
| Architektur | Qwen3 235B | DeepSeek V3.2 |
| Code Review | Qwen3-small 30B | Qwen3-Coder-480B |
| Implementierung | Qwen3-Coder-480B | Claude Code |
| Architektur Fallback | DeepSeek V3.2 (nebius) | Nemotron 120B |

### DevForge Loop Performance (29. März)
| Rolle | Modell | Durchschnittl. Zeit |
|-------|--------|---------------------|
| Architekt | qwen3 235B | ~30-45s |
| Implementierer | qwen3-coder 480B | ~45-65s |
| QA | qwen3-small 30B | ~90-100s |
| **Gesamt (3 Impl. parallel)** | — | **~3-4 Min** |

## Quality Gates

1. **G1 Build:** Unity kompiliert fehlerfrei ✅
2. **G2 Test:** NUnit Tests grün (Auto) — noch nicht eingerichtet
3. **G3 Integration:** BF SITL antwortet auf FDM-Packets ⏳ NÄCHSTER SCHRITT
4. **G4 Render:** GS rendert >30 FPS ohne Artefakte ✅ BESTANDEN
5. **G5 Fly:** Quad schwebt stabil <0.5m Error ⏳
6. **G6 RL:** Reward steigt monoton über 1000 Episodes ⏳

## Pfade

- **Projekt:** `/Users/rudi/Projects/SkyForge`
- **Unity Projekt:** `/Users/rudi/Projects/SkyForge/src/SkyForge/`
- **Betaflight:** `/Users/rudi/Projects/SkyForge/betaflight/`
- **SITL Binary:** `/Users/rudi/Projects/SkyForge/betaflight/obj/main/betaflight_SITL.elf`
- **SITL Workdir:** `/Users/rudi/Projects/SkyForge/tools/sitl_workdir/`
- **DevForge Config:** `/Users/rudi/DevForge/config-v3.3-skyforge.yaml`
- **Architektur-Plan Welle 2:** `/Users/rudi/Projects/SkyForge/docs/architekt-plan-erster-flug.md`
- **Architektur-Plan Welle 3:** `/Users/rudi/Projects/SkyForge/docs/architekt-plan-welle3.md`

## Wichtige Links

- [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) — GS Plugin
- [Betaflight](https://github.com/betaflight/betaflight) — Flight Controller
- [ML-Agents](https://github.com/Unity-Technologies/ml-agents) — Unity RL Framework
- [3DGS Official](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/) — GS Paper + Dataset

## Verfügbare GS-Maps

### 3DGS Official Dataset (PLY-Format, 30K Iterationen)
Pfad: `/Users/rudi/Projects/SkyForge/assets/3dgs-official/[scene]/point_cloud/iteration_30000/point_cloud.ply`

| Szene | Größe | Typ |
|---|---|---|
| bicycle | 1.4 GB | Outdoor |
| garden | 1.3 GB | Outdoor |
| stump | 1.1 GB | Outdoor |
| treehill | 895 MB | Outdoor |
| flowers | 860 MB | Outdoor |
| drjohnson | 805 MB | Indoor |
| truck | 601 MB | Outdoor |
| playroom | 602 MB | Indoor |
| kitchen | 438 MB | Indoor |
| room | 377 MB | Indoor |
| counter | 289 MB | Indoor |
| bonsai | 294 MB | Indoor |
| train | 243 MB | Outdoor |

## Vision & Feature-Roadmap

### UI/UX Konzept
- **Hauptmenü:** XFLIGHT Corporate Design (dunkel, minimalistisch, blaues Logo animiert)
- **Map-Auswahl:** Karten-Grid mit Preview-Thumbnails, Splat-Count, Performance-Einschätzung
- **Controller-Setup:** Wizard-basiert (Stick bewegen → Auto-Detect) + Advanced Manual Mode
- **Start-Flow:** Hauptmenü → Controller-Setup → Map-Auswahl → Flug

### Betaflight OSD Overlay
- MSP-Daten von BF SITL (TCP 5761) extrahieren
- Originale OSD-Elemente: Batterie, Flugmodus, Höhe, Speed, Timer, RSSI
- Semi-transparentes OnGUI-Overlay auf FPV-Kamera
- Betaflight-Font für Authentizität

### Sim-to-Real Pipeline (Maggie lernt fliegen)
- **RL-Training:** PPO (Stable-Baselines3) + ML-Agents
- **Custom GS-Maps:** Rudis Haus + Garten als Trainingsumgebung
- **Deployment:** ONNX → TensorRT auf Jetson Orin
