# SkyForge — PROJECT_CONTEXT.md
**Aktualisiert:** 29. März 2026, 10:35 | **DevForge:** v3.3

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
- **Controller:** RadioMaster TX16S erkannt als USB Joystick (VendorID 0x0483, 8 Achsen, 24 Buttons) ✅
  - Pfad: Assets/Modules/ControllerInput/
  - Dateien: RCInputBridge.cs, ControllerConfig.cs, RCPacket.cs
  - EdgeTX USB Joystick Modus (nicht Mass Storage!)
  - DJI FPV RC 2 wird NICHT als Gamepad erkannt (nur Lademodus per USB-C)
- **BF SITL läuft** und empfängt RC-Daten vom TX16S via Unity Bridge ✅

### Fixes & Learnings
- MCP: .mcp.json Config muss `uvx --from mcpforunityserver` nutzen, nicht `node`
- .gitignore: `/assets/` statt `assets/` — sonst werden Unity Assets/ Ordner ausgeschlossen
- DroneController Properties: `new` keyword reicht nicht — Properties umbenannt zu `Current*` Prefix
- Input System: "Both" Mode bleibt aktiv (Old für FlyCamera, New für RCInputBridge)
- DroneController.cs: `rb.linearAcceleration` existiert nicht → manuell berechnet aus Velocity-Differenz
- Duplicate DroneController in FlightBridge entfernt (Bridge hatte eigenen Placeholder)
- RCInputBridge: `Gamepad.current` findet TX16S nicht → Fallback auf `Joystick.current` nötig
- RCInputBridge: `enabled = false` in OnEnable verursacht Config-Verlust → Lazy Init implementiert
- RCInputBridge: UDP Send Error rate-limited auf alle 5s (Warning statt Error-Spam)
- Nemotron (Nebius): reasoning=true verursacht leeren content → auf reasoning=false gepatcht in OpenClaw Config

## Nächste Session — Was als Nächstes drankommt

### Integration (Welle 2 Abschluss)
1. [ ] **Drone Prefab in Szene** — DroneSetup.cs ausführen, Prefab erstellen und in Welle1Test platzieren
2. [ ] **FlightDynamicsBridge verbinden** — BridgeConfig erstellen, Bridge + DroneController verknüpfen
3. [ ] **BF SITL Arming** — über Configurator (TCP 5761) oder CLI armen, damit Motorwerte kommen
4. [ ] **Erster Flug** — TX16S Throttle hoch → Quad hebt ab in GS-Szene
5. [ ] **Achsen-Kalibrierung** — Prüfen ob Roll/Pitch/Yaw/Throttle richtig gemappt sind

### Welle 3 — UI & Usability
- [ ] Map-Auswahl UI (Runtime Scene Selection mit Metadaten)
- [ ] Controller-Kalibrierungs-UI (Achsen-Mapping, Invert, Deadzone, Expo, Live-Preview)
- [ ] Kollisions-Proxy aus Splat-Positionen
- [ ] FPV OSD Overlay
- [ ] Claude Code + Unity MCP Integration für autonome Editor-Steuerung (Batterie, Flugmodus, Timer)

### Offene Punkte Welle 1
- [ ] Rotation/Koordinatensystem-Fix als Standard verifizieren
- [ ] Performance-Test mit weiteren Maps (bicycle 1.4 GB = härtester Test)

## Vision & Feature-Roadmap

### UI/UX Konzept
- **Hauptmenü:** XFLIGHT Corporate Design (dunkel, minimalistisch, blaues Logo animiert)
- **Map-Auswahl:** Karten-Grid mit Preview-Thumbnails, Splat-Count, Performance-Einschätzung, Typ (Indoor/Outdoor)
- **Smooth Transition:** Ladescreen mit Transition-Effekt beim Map-Wechsel

### Betaflight OSD Overlay
- MSP-Daten von BF SITL (TCP 5761) extrahieren
- Originale OSD-Elemente: Batterie, Flugmodus, Höhe, Speed, GPS, Timer, RSSI
- Semi-transparentes Canvas-Overlay auf FPV-Kamera
- Betaflight-Font für Authentizität
- Referenz: Echte FPV-Brille / BF OSD-Tab

### Replay-System
- Flugaufzeichnung (Position, Rotation, Inputs pro Frame)
- Wiedergabe aus verschiedenen Kameraperspektiven: Third Person, Chase Cam, Freie Kamera
- Flugbahn-Visualisierung (leuchtende Linie im 3D-Raum)
- Zeitlupe / Pause / Zurückspulen

### Screen Recording
- Unity Recorder Extension integrieren
- Ein-Klick-Aufnahme (Record-Button im UI)
- Export: MP4 / GIF, direkt zum Teilen

### Sim-to-Real Pipeline (Maggie lernt fliegen)
- **Trainingsmodus:** Manuell/Acro von Anfang an (KEIN Angle/Horizon Mode)
- **RL-Training:** PPO (Stable-Baselines3) + ML-Agents, tausende Episoden
- **Custom GS-Maps:** Rudis Haus + Garten als fotorealistische Trainingsumgebung
  - Aufnahme: DJI Mini 4 Pro → Tausende Fotos (Haus innen + außen, Garten, alle Zimmer)
  - Processing: GPU Droplet (RunPod/Lambda) → 3DGS Training → PLY Export
  - Ziel: Maggie trainiert in der Map ihres eigenen Zuhauses
- **Deployment:** Trainiertes Policy-Netzwerk als ONNX → TensorRT auf Jetson Orin
  - Inferenz: ~2-3ms pro Frame
  - Kamera → Netzwerk → Steuerbefehle → Flight Controller
  - Live-Video zurück an Maggie (Telepräsenz)
- **Sim-to-Real Vorteil:** GS-Maps sind fotorealistisch → minimaler Sim-to-Real Gap

### Domain Randomization & Robustheits-Training (KRITISCH für Real-World)
Fotorealistische Maps allein reichen NICHT — echte Flüge haben Störungen die trainiert werden müssen:

**Visuelle Störungen (im Training simulieren):**
- [ ] Motion Blur bei hoher Geschwindigkeit (Shader-basiert, abhängig von Velocity)
- [ ] Schlechte Lichtverhältnisse: Dunkelheit, Schatten, Gegenlicht, Blendung
- [ ] Nebel / Staub / Partikel (Volumetric Fog, Particle System)
- [ ] Kamera-Rauschen / Compression-Artefakte (Post-Processing)
- [ ] Framerate-Drops / Video-Lag simulieren (Frame-Skip)
- [ ] Teilweise verdeckte Sicht (Lens Flare, Wassertropfen, Staub auf Linse)

**Sensorische Unsicherheit:**
- [ ] IMU-Rauschen / Drift (Gauss-Noise auf Gyro/Accel-Daten)
- [ ] GPS-Ungenauigkeit / GPS-Ausfall
- [ ] Barometer-Drift (Höhenmessung unzuverlässig)
- [ ] Magnetometer-Störungen (in Gebäuden, unter Brücken)

**Dynamische Umgebung:**
- [ ] Bewegliche Objekte: Autos, Menschen, Tiere, andere Drohnen
- [ ] Wind-Böen (zufällige Kräfte auf Rigidbody)
- [ ] Thermik / Abwind an Gebäudekanten
- [ ] Veränderte Szene: Autos die woanders parken, offene/geschlossene Türen

**Blind-Flight & Intuitions-Training:**
- [ ] "Blindflug-Episoden": Kamerabild wird zeitweise schwarz/unscharf → Agent muss aus IMU + letztem bekannten State weiterfliegen
- [ ] Strategie-Training: Bei Sichtverlust → an Wand/Decke orientieren (wie Rudis Autobahnbrücken-Strategie)
- [ ] Propriozeptives Fliegen: Agent lernt Drohnen-Attitude aus Motorwerten + IMU, unabhängig von Kamerabild
- [ ] Reward für "sichere Notlandung" wenn Sicht komplett ausfällt

**Curriculum Learning Stufen:**
1. Perfekte Bedingungen (klare Sicht, kein Wind, statische Szene)
2. Leichte Störungen (etwas Wind, leichter Motion Blur)
3. Mittlere Störungen (Nebel, Schatten, bewegliche Objekte)
4. Schwere Bedingungen (Staub, Dunkelheit, starker Wind, Blindflug-Phasen)
5. Real-World Ready (alle Störungen gleichzeitig, zufällig kombiniert)

### TODO: Eigene GS-Map erstellen (Haus + Garten)
- [ ] DJI Mini 4 Pro: Haus komplett abfliegen (innen + außen), ~2000-5000 Fotos
- [ ] COLMAP für Structure-from-Motion (Kamerakalibrierung + Sparse Point Cloud)
- [ ] GPU Droplet (RunPod) mieten für 3DGS Training (~30K Iterationen)
- [ ] PLY exportieren und in SkyForge importieren
- [ ] Maggie trainiert in ihrer eigenen Heimat-Map
- [ ] Anleitung für den kompletten Workflow erstellen (DJI → COLMAP → 3DGS → Unity)

## Verfügbare GS-Maps

### mkkellogg (ksplat Format)
Pfad: /Users/rudi/Projects/SkyForge/assets/mkkellogg_gs/[scene]/[file].ksplat

### 3DGS Official Dataset (PLY-Format, 30K Iterationen)
Pfad: /Users/rudi/Projects/SkyForge/assets/3dgs-official/[scene]/point_cloud/iteration_30000/point_cloud.ply
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

## Module

| Modul | Pfad | Status |
|---|---|---|
| GS Renderer | Assets/ (Aras-P Plugin) | ✅ Funktioniert |
| Flight Dynamics Bridge | Assets/Modules/FlightBridge/ | ✅ Code fertig, Integration ausstehend |
| Drone Model | Assets/Modules/DroneModel/ | ✅ Code fertig, Prefab ausstehend |
| Controller Input | Assets/Modules/ControllerInput/ | ✅ TX16S erkannt, RC→BF SITL funktioniert |
| Betaflight SITL | betaflight/ | ✅ Kompiliert + läuft |
| FlyCamera | Assets/Scripts/FlyCamera.cs | ✅ Funktioniert |
| RL Training Engine | Assets/Modules/RLTraining/ | ⏳ Phase 2 |
| MCP Integration | .mcp.json + Packages/manifest.json | ✅ Plugin installiert, Claude Code connected |
| Editor Commands | Assets/Editor/SkyForgeCommandHandler.cs | ✅ 6 Befehle (HealthCheck, Scenes, Build, Prefabs) |
| Batch Runner | tools/unity_batch.sh | ✅ Headless Unity-Steuerung per CLI |

## DevForge v3.3 Modell-Mapping

| Task-Typ | Primär | Fallback |
|---|---|---|
| Unity C# | Codex (Free) | Qwen3-Coder-480B |
| BF Integration | Claude Code | Nemotron 120B |
| GS Shader | Codex (Free) | Qwen3-Coder-480B |
| RL/ML Code | Codex (Free) | Nemotron 120B |
| Architektur | Nemotron 120B (reasoning=false!) | DeepSeek-R1 |
| Code Review | Qwen3-Coder-480B | Claude Code |
| Architektur Fallback | DeepSeek V3.2 (nebius) | Nemotron 120B |

### Kostenvergleich (geschätzt, pro 1M Token)
| Modell | Input | Output | Stärke |
|--------|-------|--------|--------|
| Qwen3-small | ~$0.15 | ~$0.25 | Chat, einfache Tasks |
| DeepSeek V3.2 | ~$0.80 | ~$1.20 | Reasoning, Tool-Use, Mathe |
| Nemotron Super | ~$3.00 | ~$5.00 | 1M Kontext, Multi-Agent |
| Codex | kostenlos | kostenlos | Unity C#, Code-Generierung |

## Selbstoptimierung & Autonome Entwicklung

### Maggies Autonomie-Level
Maggie kann eigenständig an SkyForge arbeiten:
- **Issue → Implementierung → PR** über DevForge-Pipeline
- **Rollenverteilung:** Architekt (Nemotron/DeepSeek V3.2) → Implementierer (Codex/Claude Code) → QA (Qwen3)
- **Claude Code + Unity MCP:** Direkte Unity-Editor-Steuerung über Model Context Protocol (Recherche läuft)

### Grenzen (immer Rudi fragen!)
- Änderungen an Flugsteuerungs-Logik (FlightDynamicsBridge, DroneController)
- Releases und Deployments
- Budget-Überschreitungen
- Ambige Anforderungen / UX-Design-Entscheidungen

### Langfrist-Vision: Capability Evolver
- GEP-Protokoll (Genome Evolution Protocol) für Self-Learning
- Automatische Fehlererkennung → strukturierte Verbesserungsvorschläge
- Capsules: Bewiesene Lösungen werden wiederverwendbar gespeichert

## Quality Gates

1. **G1 Build:** Unity kompiliert fehlerfrei ✅
2. **G2 Test:** NUnit Tests grün (Auto)
3. **G3 Integration:** BF SITL antwortet auf FDM-Packets (Semi-Auto) — NÄCHSTER SCHRITT
4. **G4 Render:** GS rendert >30 FPS ohne Artefakte ✅ BESTANDEN
5. **G5 Fly:** Quad schwebt stabil <0.5m Error (Auto)
6. **G6 RL:** Reward steigt monoton über 1000 Episodes (Auto)

## Pfade

- **Projekt:** `/Users/rudi/Projects/SkyForge`
- **Unity Projekt:** `/Users/rudi/Projects/SkyForge/src/SkyForge/`
- **Betaflight:** `/Users/rudi/Projects/SkyForge/betaflight/`
- **SITL Binary:** `/Users/rudi/Projects/SkyForge/betaflight/obj/main/betaflight_SITL.elf`
- **SITL Workdir:** `/Users/rudi/Projects/SkyForge/tools/sitl_workdir/`
- **DevForge Config:** `/Users/rudi/DevForge/config-v3.3-skyforge.yaml`
- **Architektur-Plan:** `/Users/rudi/.openclaw/workspace/docs/2025-03-27-drone-sim-architektur-plan.md`

## Wichtige Links

- [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) — GS Plugin
- [Betaflight](https://github.com/betaflight/betaflight) — Flight Controller
- [ML-Agents](https://github.com/Unity-Technologies/ml-agents) — Unity RL Framework
- [3DGS Official](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/) — GS Paper + Dataset
