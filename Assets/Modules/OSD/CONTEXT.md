# OSD Module — CONTEXT.md
**Modul:** OSD
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/OSD
**Beschreibung:** Implementiert den On-Screen-Display für die FPV-Sicht des Drohnen-Simulators. Zeigt Batteriestatus, Flugmodus, Höhe, RSSI und Flugzeit an.

## Architektur
- **OSDController**
  - Verwaltet die Sichtbarkeit und Position des OSD
  - Bindet Daten aus `OSDData` an UI-Elemente (Unity UI Toolkit)
  - Reagiert auf Tastaturbefehle (F5 = OSD toggeln, F2 = FPV-Modus)

- **OSDData (ScriptableObject)**
  - Singleton-Datencontainer für alle OSD-Relevanten Flugdaten
  - Enthält: Batteriespannung/Strom, Flugmodus, Armed-Status, Höhe, vertikale Geschwindigkeit, RSSI, Flugzeit

- **MSPClient**
  - Verbindung zum Betaflight SITL über TCP (Port 5761)
  - Ruft MSP-Pakete (MSP_STATUS, MSP_ANALOG, MSP_ALTITUDE, MSP_RC) ab
  - Parst die empfangenen Daten und aktualisiert `OSDData`
  - Verbindet automatisch bei Verbindungsabbruch

## Dependencies
- Unity UI Toolkit (UIDocument, VisualElement, Bindung)
- Betaflight SITL (lokal laufend auf Port 5761)
- `DroneController.cs` (nicht direkt, über `OSDData`-Aktualisierung indirekt)

## UI-Elemente
- `batteryLabel` — Batteriespannung in Volt, farbcodiert (grün/orange/rot)
- `timerLabel` — Flugzeit im MM:SS-Format
- `modeLabel` — Aktueller Flugmodus (ACRO, ANGLE, etc.)
- `altitudeLabel` — Höhe in Metern und vertikale Geschwindigkeit
- `rssiLabel` — RSSI-Stärke in Prozent
- `armedLabel` — Armed/Disarmed-Status, farbcodiert

## Status
- Alle Kernkomponenten vorhanden und funktionieren
- Keine Fehler im Play-Modus bei angeschlossener SITL-Instanz
- `OSDData` als ScriptableObject ist korrekt implementiert
- `MSPClient` funktioniert mit aktuellem SITL

## Nächste Schritte
- Visual überarbeitung des OSD (Schriftart, Position, Opazität)
- Hinzufügen weiterer Daten (z. B. GPS, Heading)
- Unterstützung für mehrere Kameras/Ansichten