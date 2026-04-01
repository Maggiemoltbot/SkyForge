# Sensors Module — CONTEXT.md
**Modul:** Sensors
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/Sensors
**Beschreibung:** Simuliert IMU-Sensordaten (Gyroskop, Beschleunigungssensor) für die Integration mit Betaflight Firmware. Kann Sensorrauschen und Bias-Drift simulieren.

## Architektur
- **IMUSimulator**
  - MonoBehaviour, das an die Drohnen-Kinematik gekoppelt ist
  - Berechnet Gyroskop- und Beschleunigungsdaten basierend auf Rigidbody-Physik
  - Fügt optional Rauschen und Bias-Drift hinzu, um reale Bedingungen zu simulieren
  - Exportiert Daten im Unity-Koordinatensystem (Y-up, Linkshänder) für Unity-interne Nutzung
  - Für Unity-NED-Transformation siehe `CoordinateConverter` in `FlightBridge`

### Datenberechnung
- **Gyroskopdaten**
  - Direkt aus `Rigidbody.angularVelocity` abgeleitet (in rad/s)
  - Optional: Bias-Drift und weißes Gauß-Rauschen hinzufügen (nach Konfiguration im Inspector)
  - Ausgabe in Grad/Sekunde für Kompatibilität mit üblichen IMU-Datenformaten

- **Beschleunigungsdaten**
  - Lineare Beschleunigung wird über `Rigidbody.velocity` und `FixedUpdate`-Differenz berechnet
  - Transformiert in lokales Drohnen-Koordinatensystem
  - Addiert Gravitationsvektor (in lokaler Rotation) hinzu
  - Subtrahiert Gravitation für reine lineare Beschleunigung (typisches IMU-Signal)
  - Optional: Gauß-Rauschen hinzufügen

## Dependencies
- `DroneController.cs` (über `Rigidbody`-Referenz)
- `FlightBridge/FDMPacket.cs` (als Zielformat für NED-Transformation, geschieht aber in `FlightDynamicsBridge`)
- Unity Physics System

## Konfigurierbare Parameter (Inspector)
- `enableNoise` — Schaltet Simulation von Rauschen und Bias-Drift ein/aus
- `gyroNoiseStdDev` — Standardabweichung des Gyro-Rauschens (Grad/s)
- `accelNoiseStdDev` — Standardabweichung des Beschleunigungs-Rauschens (m/s²)
- `gyroBiasDrift` — Bias-Drift über die Zeit (Grad/s²)

## Output-Formate
- `GetGyroData()` → `Vector3` in Grad/Sekunde
- `GetAccelerometerData()` → `Vector3` in m/s²

## Status
- Funktioniert korrekt in Verbindung mit `FlightDynamicsBridge`
- Rauschen und Bias sind parametrisiert und können getestet werden
- Keine Fehler im Play-Modus

## Nächste Schritte
- Magnetometer-Simulation hinzufügen (für Heading)
- Temperaturabhängigkeit des Sensorrauschens simulieren
- Kalibrierungsroutine implementieren (Bias-Reset bei Idle)