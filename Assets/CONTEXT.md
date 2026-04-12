# SkyForge - Kontextdatei

## Aktueller Projektstand
SITL-Integration ist fast komplett. Die Kommunikation zwischen Unity und Betaflight-SITL funktioniert für Throttle-Werte zwischen 1000 und 1600. Ab Throttle 1800 disarmt Betaflight aufgrund unrealistischer IMU-Daten.

## Port-Mapping
- Unity → SITL FDM: Port 9003
- Unity → SITL RC: Port 9004
- SITL → Unity PWM: Port 9002

## Datenfluss-Diagramm (Text)
```
[Unity Controller] → [RC-UDP 9004] → [Betaflight-SITL]
                     ↑                     ↓
            [FDM-TCP 9003] ← [Unity Physik]
                     ↓                     ↑
              [PWM-UDP 9002] ← [SITL]
```

## Offene TODOs
1. Unity FlightDynamicsBridge muss FDM-Daten an SITL senden und PWM-Daten von SITL empfangen (geschlossener Loop)
2. Integration von Unitys Physik-Engine (DroneController + IMUSimulator) als FDM-Quelle
3. Sicherstellung, dass IMU-Daten realistisch genug sind, um Disarming bei höherem Throttle zu verhindern
4. Behandlung des SITL-Neustarts nach CLI "save" + Reboot
5. Lösung für die gleichzeitige Nutzung von CLI und MSP über Port 5761

## Git-Stand
- SkyForge main: 0493001 (pushed)
- Betaflight patches: 6c92544bb (lokal committed, nicht pushbar zu upstream)