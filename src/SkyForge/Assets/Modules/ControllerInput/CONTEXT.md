# Controller Input Module

This module reads USB game controller input via Unity's new Input System and sends RC channel data to Betaflight SITL over UDP port 9004. It bridges physical controller sticks/buttons to the 16 RC channels that Betaflight expects.

## Components

1. **RCPacket.cs** — Binary struct matching the Betaflight SITL RC packet format: 8-byte double timestamp + 16 × uint16 channels = 40 bytes total. Channel values are PWM microseconds (1000–2000). Native byte order, no header.

2. **ControllerConfig.cs** — ScriptableObject for per-axis mapping, invert, deadzone, expo, network settings, and AUX button assignments.

3. **RCInputBridge.cs** — Main MonoBehaviour. Reads `Gamepad.current` from the new Input System, applies deadzone/expo/invert, maps to RC channels, and sends UDP packets at the configured rate.

## Channel Mapping (Betaflight AETR default)

| Channel | Function | Controller Input     |
|---------|----------|---------------------|
| 0       | Roll     | Right Stick X       |
| 1       | Pitch    | Right Stick Y       |
| 2       | Throttle | Left Stick Y        |
| 3       | Yaw      | Right Stick X       |
| 4–7     | AUX 1–4  | Face Buttons (toggle) |
| 8–15    | AUX 5–12 | Unassigned          |

## Network Protocol

- **Direction:** Unity → BF SITL
- **Transport:** UDP
- **Port:** 9004
- **Packet size:** 40 bytes
- **Send rate:** Configurable (default 100 Hz)
- **Format:** `[double timestamp][uint16 ch0]...[uint16 ch15]`

## Setup

1. Create a ControllerConfig asset: Right-click in Project → Create → SkyForge → ControllerConfig
2. Add RCInputBridge to a GameObject
3. Assign the ControllerConfig asset
4. Connect a USB controller and press Play
5. Start BF SITL: `bash tools/start_sitl.sh`
