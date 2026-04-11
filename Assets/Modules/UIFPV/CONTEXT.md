# UIFPV Module — CONTEXT.md
**Modul:** UIFPV
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/UIFPV
**Beschreibung:** FPV (First Person View) UI-Modul für den Drohnensimulator. Rendert die Kamerasicht der Drohne mit OSD-Overlay, Analog-Rausch-Effekt und konfigurierbare Kamera-Einstellungen. Simuliert eine realistische FPV-Brille (z.B. DJI O3, Walksnail Avatar).

## Architektur

### Kernkomponenten (geplant)
- **FPVViewController.cs**
  - Haupt-Controller für die FPV-Ansicht
  - Verwaltet Kamerawechsel (FPV ↔ Freecam ↔ Chase)
  - Toggled via Hotkey (F2) oder UI-Button
  - Steuert Fullscreen-FPV vs. PiP-Mode (Picture-in-Picture)

- **FPVCameraSettings.cs (ScriptableObject)**
  - FOV (Standard: 120° für Weitwinkel, typisch FPV)
  - Aspect Ratio (4:3 Standard, 16:9 optional)
  - Lens Distortion (Barrel/Pincushion)
  - Exposure, White Balance
  - Analog-Rausch-Intensität

- **FPVPostProcess.cs**
  - Custom Render Feature (URP)
  - Simuliert Analog-Video-Rauschen (Scan Lines, Static Noise)
  - Simuliert RSSI-abhängige Bildstörungen (→ Daten aus OSDData)
  - Color Grading für realistisches FPV-Gefühl
  - Optional: VHS/Retro-Effekt für Nostalgie

- **FPVOverlay.uxml / FPVOverlay.uss**
  - UI-Toolkit basiertes Overlay für FPV-spezifische Anzeigen
  - Crosshair (zentriert, konfigurierbar)
  - Kamera-Tilt-Anzeige
  - Recording-Indicator
  - Latenz-Anzeige (simuliert)

### Kamera-Modi
| Modus | Beschreibung | Hotkey |
|-------|-------------|--------|
| FPV | Drohnen-Kamera, fixiert auf Drone-Forward | F2 |
| Chase | Verfolgerkamera hinter der Drohne | F3 |
| Freecam | Freie Kamera (WASD + Maus) | F4 |
| PiP | Kleines FPV-Fenster in Ecke (mit Freecam als Hauptansicht) | F7 |

## Integration
- **FPVCamera** (bestehend in DroneModel) — Kamera-Transform und Rendering
- **OSDData** — RSSI-Wert für Rausch-Simulation, Batterie für Overlay
- **OSDController** — OSD-Elemente werden über FPV-Overlay gerendert
- **DroneOverlayFeature** (bestehend in Rendering/) — Custom Render Feature Basis

## Dependencies
- Unity URP (Universal Render Pipeline)
- UI Toolkit (UIDocument, UXML, USS)
- `FPVCamera.cs` aus DroneModel-Modul
- `OSDData.cs` aus OSD-Modul
- `DroneOverlayFeature.cs` aus Rendering/

## Konfiguration
- FPV-Kamera-Parameter über `FPVCameraSettings` ScriptableObject
- Post-Processing-Effekte über Inspector togg- und parametrisierbar
- Standard-Startmodus konfigurierbar (FPV oder Freecam)

## Status
- **Phase:** Konzept/Design erstellt
- Basis-FPVCamera existiert bereits im DroneModel-Modul
- DroneOverlayFeature als Custom Render Feature vorhanden
- Vollständiges UI-System und Post-Processing noch nicht implementiert

## Nächste Schritte
- [ ] `FPVViewController.cs` implementieren (Kamera-Modi-Switching)
- [ ] `FPVCameraSettings.cs` ScriptableObject erstellen
- [ ] `FPVPostProcess.cs` als URP Render Feature
- [ ] `FPVOverlay.uxml` + `.uss` erstellen
- [ ] Analog-Rausch-Shader implementieren (HLSL/ShaderGraph)
- [ ] RSSI-basierte Bildstörung (Daten aus MSPClient)
- [ ] PiP-Mode mit RenderTexture
