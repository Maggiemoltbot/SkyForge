# Betaflight SITL Build — macOS ARM64

## Overview

Betaflight SITL (Software In The Loop) runs the full Betaflight flight controller firmware as a native process on macOS. It communicates with a flight simulator via UDP and exposes MSP (MultiWii Serial Protocol) over TCP for Betaflight Configurator.

**Build date:** 2026-03-28
**Betaflight version:** 2026.6.0-alpha (master branch)
**Platform:** macOS 26.3 / Apple Silicon (M4 Pro, arm64)
**Compiler:** Apple clang 21.0.0

## Prerequisites

- Xcode Command Line Tools (`xcode-select --install`)
- GNU Make (included with Xcode CLI tools)
- Python 3 (for build scripts)
- No ARM cross-compiler needed — SITL compiles with the host compiler

## Build

```bash
cd /Users/rudi/Projects/SkyForge/betaflight
make TARGET=SITL
```

Output: `obj/main/betaflight_SITL.elf` (Mach-O 64-bit executable arm64, ~333KB)

### Build Notes

- macOS ARM64 is natively supported since PR #14284 ("Fix SITL for MacOS M1")
- The build produces only deprecation warnings (`-Ofast` → `-O3 -ffast-math`) and minor float promotion warnings — all harmless
- No patches required
- Clean rebuild: `make TARGET=SITL clean && make TARGET=SITL`

### macOS-specific Build Adaptations (automatic)

The SITL Makefile (`src/platform/SIMULATOR/mk/SITL.mk`) automatically detects macOS ARM64 and:
- Removes `-lrt` (not available on macOS)
- Removes the linker script (macOS uses different memory layout)
- Disables `-Werror` (some warnings are Apple-clang-specific)
- Disables `-Wunsafe-loop-optimizations` and `-fuse-linker-plugin` (GCC-only flags)

## Network Architecture

```
                    UDP                    UDP
 Simulator ───────────────► SITL ◄──────────────── RC Controller
 (XFlight)   FDM @ 9003    │  │    RC @ 9004
                            │  │
              UDP           │  │           TCP
 Simulator ◄───────────────┘  └──────────────────► BF Configurator
              PWM @ 9002                MSP @ 5761
```

### UDP Ports

| Port | Direction | Protocol | Purpose |
|------|-----------|----------|---------|
| 9001 | SITL → Sim | UDP | PWM Raw Output (RealFlight bridge) |
| 9002 | SITL → Sim | UDP | PWM Output (Gazebo/XFlight) |
| 9003 | Sim → SITL | UDP | FDM/State Input (position, IMU, pressure) |
| 9004 | Ctrl → SITL | UDP | RC Channel Input |

### TCP Ports (UART Emulation)

| Port | Protocol | Purpose |
|------|----------|---------|
| 5761 | TCP | UART1 — MSP (Betaflight Configurator) |
| 5762 | TCP | UART2 |
| 5763 | TCP | UART3 |
| 5764-5768 | TCP | UART4-8 |

UARTs bind on `tcp://127.0.0.1:576x` when the port is opened.

## FDM Packet Format (Port 9003 → SITL)

```c
typedef struct {
    double timestamp;                       // seconds
    double imu_angular_velocity_rpy[3];     // rad/s
    double imu_linear_acceleration_xyz[3];  // m/s^2 NED, body frame
    double imu_orientation_quat[4];         // w, x, y, z
    double velocity_xyz[3];                 // m/s, ENU for virtual GPS
    double position_xyz[3];                 // meters, Lon/Lat/Alt for virtual GPS
    double pressure;                        // Pa
} fdm_packet;  // 200 bytes
```

## PWM Output Packet Format (SITL → Port 9002)

```c
typedef struct {
    float motor_speed[4];   // [0.0, 1.0] normal, [-1.0, 1.0] 3D mode
} servo_packet;  // 16 bytes
```

## PWM Raw Output Packet Format (SITL → Port 9001)

```c
typedef struct {
    uint16_t motorCount;
    float pwm_output_raw[16];   // Raw PWM 1100-1900
} servo_packet_raw;  // 66 bytes
```

## RC Input Packet Format (Port 9004 → SITL)

```c
typedef struct {
    double timestamp;           // seconds
    uint16_t channels[16];      // RC channel values
} rc_packet;  // 40 bytes
```

## Usage

### Quick Start

```bash
./tools/start_sitl.sh
```

### With Custom Simulator IP

```bash
./tools/start_sitl.sh --ip 192.168.1.100
```

### Rebuild and Start

```bash
./tools/start_sitl.sh --rebuild
```

### Manual Start

```bash
cd /Users/rudi/Projects/SkyForge/tools/sitl_workdir
/Users/rudi/Projects/SkyForge/betaflight/obj/main/betaflight_SITL.elf
```

### Command Line Options

```
--ip <address>   Simulator IP address (default: 127.0.0.1)
--config <file>  Load CLI config file, save to EEPROM, and exit
--help, -h       Show help
```

## Connecting Betaflight Configurator

1. Start SITL: `./tools/start_sitl.sh`
2. Open Betaflight Configurator
3. In the connection dropdown, select **Manual** / **TCP**
4. Enter: `127.0.0.1:5761`
5. Click **Connect**

The SITL will identify as board "SITL" with MCU type "SIMULATOR".

## Files

- `betaflight/` — Betaflight source (git clone)
- `betaflight/obj/main/betaflight_SITL.elf` — Compiled binary
- `tools/start_sitl.sh` — Start script
- `tools/sitl_workdir/eeprom.bin` — Persistent config (created on first run, 32KB)

## Troubleshooting

### Port already in use
```bash
# Find and kill existing SITL
pkill -f betaflight_SITL
# Or check what's using the port
lsof -i :9003
```

### Configurator won't connect
- Ensure SITL is running (check `ps aux | grep betaflight_SITL`)
- Ensure port 5761 is listening: `lsof -i :5761`
- Use TCP connection mode in Configurator, not Serial

### Reset config
```bash
rm tools/sitl_workdir/eeprom.bin
# Restart SITL — fresh config will be created
```
