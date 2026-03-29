# Controller Input Setup

## Voraussetzungen

- Unity 2023+ mit Input System Package (v1.19.0+)
- Player Settings → Active Input Handling: "Both" oder "Input System Package (New)"
- USB Game Controller (Xbox, PlayStation, Generic Gamepad)
- Betaflight SITL Binary (`betaflight/obj/main/betaflight_SITL.elf`)

## Schnellstart

### 1. Betaflight SITL starten

```bash
cd /Users/rudi/Projects/SkyForge
bash tools/start_sitl.sh
```

Ports die geöffnet werden:
| Port | Protokoll | Richtung | Funktion |
|------|-----------|----------|----------|
| 9001 | UDP | BF → | PWM Raw Output (RealFlight) |
| 9002 | UDP | BF → | PWM Output (Gazebo/XFlight) |
| 9003 | UDP | → BF | FDM/State Input (Simulator) |
| 9004 | UDP | → BF | RC Input (Controller) |
| 5761 | TCP | ↔ | MSP (Betaflight Configurator) |

### 2. Unity Setup

1. **ControllerConfig Asset erstellen:**
   - Rechtsklick im Project-Fenster → Create → SkyForge → ControllerConfig
   - Default-Werte sind für Mode 2 (Throttle links) vorkonfiguriert

2. **RCInputBridge zum Drone-GameObject hinzufügen:**
   - Add Component → RCInputBridge
   - Config-Feld: das erstellte ControllerConfig Asset zuweisen

3. **Play drücken** — das Script erkennt automatisch den ersten verbundenen Gamepad

### 3. Controller anschließen

Unterstützte Controller (alles was Unity Input System als `Gamepad` erkennt):
- Xbox Controller (USB/Bluetooth)
- PlayStation DualShock 4 / DualSense
- Generic USB Gamepads (Logitech, 8BitDo, etc.)

## Kanalzuordnung

Standard-Mapping (Mode 2 — Throttle links):

```
        LINKS                RECHTS
    ┌───────────┐        ┌───────────┐
    │     ↑     │        │     ↑     │
    │   Throttle│        │   Pitch   │
    │ ←Yaw  → │        │ ←Roll  → │
    │     ↓     │        │     ↓     │
    └───────────┘        └───────────┘
     Ch3   Ch2            Ch0   Ch1
```

| RC Kanal | Funktion | Controller | PWM Bereich |
|----------|----------|-----------|-------------|
| 0 | Roll (Aileron) | Rechter Stick X | 1000–2000, Center 1500 |
| 1 | Pitch (Elevator) | Rechter Stick Y | 1000–2000, Center 1500 |
| 2 | Throttle | Linker Stick Y | 1000–2000, Idle 1000 |
| 3 | Yaw (Rudder) | Linker Stick X | 1000–2000, Center 1500 |
| 4 | AUX1 (Arm) | A / Cross | Toggle 1000/2000 |
| 5 | AUX2 (Mode) | B / Circle | Toggle 1000/2000 |
| 6 | AUX3 | X / Square | Toggle 1000/2000 |
| 7 | AUX4 | Y / Triangle | Toggle 1000/2000 |

## ControllerConfig Einstellungen

### Deadzone
- **Default:** 0.05 (5%)
- Stick-Werte innerhalb der Deadzone werden als 0 behandelt
- Throttle hat eine engere Deadzone (0.02) für feinere Kontrolle

### Expo
- **Default:** 0.0 (linear)
- Formel: `output = (1 - expo) * input + expo * input³`
- Höhere Werte (0.3–0.7) machen die Mitte weicher — empfohlen für Anfänger

### Invert
- Per-Achse invertierbar für verschiedene Controller-Layouts
- Nützlich wenn ein Stick "falsch herum" reagiert

### Send Rate
- **Default:** 100 Hz
- Betaflight SITL verarbeitet Pakete so schnell wie sie kommen
- 50–200 Hz sind sinnvolle Werte

## UDP Paket-Format (Port 9004)

Das RC-Paket ist ein 40-Byte Binary-Struct ohne Header:

```
Offset  Typ       Feld         Beschreibung
0-7     double    timestamp    Sekunden seit Start (IEEE 754)
8-9     uint16    channel[0]   Roll (PWM 1000-2000)
10-11   uint16    channel[1]   Pitch
12-13   uint16    channel[2]   Throttle
14-15   uint16    channel[3]   Yaw
16-17   uint16    channel[4]   AUX1
18-19   uint16    channel[5]   AUX2
20-21   uint16    channel[6]   AUX3
22-23   uint16    channel[7]   AUX4
24-39   uint16    channel[8-15] AUX5-12
```

- Byte-Order: Native (Little-Endian auf x86/ARM)
- Keine Magic-Bytes oder Checksumme
- Werte: PWM Mikrosekunden (1000 = min, 1500 = center, 2000 = max)

## Troubleshooting

**Kein Controller erkannt:**
- Prüfe ob der Controller im OS erkannt wird (System Preferences → Game Controllers)
- Unity muss neu gestartet werden wenn der Controller nach Unity angeschlossen wurde
- Console zeigt "[RCInputBridge] No controller connected. Waiting for input device..."

**SITL reagiert nicht auf Input:**
- Prüfe ob SITL läuft: `pgrep -la betaflight_SITL`
- Prüfe ob Port 9004 offen ist: `lsof -i :9004 -P`
- Enable "Show Channel Values" im Inspector für Live-Debug
- SITL Console zeigt `[SITL] new rc` bei empfangenen Paketen

**Betaflight Configurator verbinden:**
- TCP → 127.0.0.1:5761
- Dort können PID-Werte, Flight Modes, etc. konfiguriert werden
