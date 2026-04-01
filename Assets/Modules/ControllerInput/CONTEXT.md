# ControllerInput Module — CONTEXT.md
**Modul:** ControllerInput
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/ControllerInput
**Beschreibung:** Verarbeitet Eingaben von Gamecontrollern (Joystick, RC-Controller) und transformiert diese in RC-Kanäle, die an die Betaflight SITL gesendet werden. Implementiert auch Tastatur-Emulation für Debugging.

## Architektur
- **ControllerInputModule**
  - Verwaltet die Zuordnung von physischen Controller-Achsen zu logischen RC-Kanälen (Throttle, Yaw, Pitch, Roll)
  - Nutzt Unitys Input System oder altes Input Manager API (je nach Legacy-Unterstützung)
  - Normalisiert und debounced Eingaben

- **ChannelMapper**
  - Stellt sicher, dass 16 RC-Kanäle (1000-2000 µs) generiert werden
  - Mappt Throttle (Achse 2) mit negativer Polarität um
  - Invertiert Pitch (Achse 1), um typisches RC-Verhalten zu erreichen

- **RCChannelSender**
  - Empfängt die generierten Kanaldaten und sendet sie via UDP an Betaflight SITL
  - Portkonfiguration (Standard: 9001) über `BridgeConfig`
  - Thread-safe, nutzt Locking für Thread-sicheren Zugriff

## Eingabemethoden
- **Gamepad/Joystick**
  - Standardzuordnung: Throttle (Axis 3, Rechtsstick Y), Yaw (Axis 2), Pitch (Axis 1, Linkstick Y), Roll (Axis 0)
  - Deadzone 0.05
- **Tastatur (Debug)**
  - Q/E → Roll links/rechts
  - A/D → Pitch vor/zurück
  - W/S → Throttle hoch/runter
  - C/V → Yaw links/rechts

## Dependencies
- `FlightDynamicsBridge.cs` (für `SendRCChannels`)
- Unity Legacy Input System (für `Input.GetAxis("Throttle")`)
- `DroneModel` (indirekt, über Flugzeugsteuerung)
- `BridgeConfig` (für UDP-Konfiguration)

## Konfiguration
- Achsenzuordnung ist in `ProjectSettings/InputManager.asset` definiert
- Siehe auch Diagnoseszene `ThrottleAxisDiagnostics.unity` zur Überprüfung der Mapping-Konfiguration

## Status
- Basisimplementierung vorhanden
- Throttle-Achse wurde in Sprint 4 im `InputManager` hinzugefügt
- Diagnoseszene funktioniert korrekt

## Nächste Schritte
- Unterstützung für Unity Input System (USS) hinzufügen
- Automatisierten Controller-Detection-Mechanismus implementieren
- Profil-System für verschiedene Controller-Typen (FrSky, Spektrum, Taranis)