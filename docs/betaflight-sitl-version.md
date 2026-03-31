# Betaflight SITL — Version Documentation

## Installierte Version

| Parameter | Wert |
|-----------|------|
| **Version** | Betaflight 4.5.1 |
| **Git Tag** | `4.5.1` (Commit `77d01ba3b`) |
| **Release Datum** | 27. Juli 2024 |
| **Installiert am** | 31. März 2026 |
| **Kompatibel mit** | Betaflight Configurator 10.10.x (native macOS App) |
| **Binary** | `beta/obj/main/betaflight_SITL.elf` |
| **MSP Port** | TCP 5761 (Configurator: `127.0.0.1:5761`) |

## Warum 4.5.1?

- Betaflight Configurator **10.10.x** unterstützt nur Firmware **4.2.x bis 4.5.x**
- Die neue Betaflight-Versionsschema (2026.6.0-alpha) ist **nicht** kompatibel mit Configurator 10.10
- 4.5.1 war der stabile Release von Juli 2024 (~1 Jahr alt, wie gewünscht)
- 4.5.2 (März 2025) enthält "Fix SITL build" — aber 4.5.1 lässt sich mit den macOS-Patches ebenfalls bauen

## Backup der vorherigen Version

Die vorherige SITL-Binary (master/2026.6.0-alpha) wurde gesichert:
```
beta/obj/backup_master_2026-03-31/betaflight_SITL_master_4.5.0-949.elf
```

Um zur alten Version zurückzukehren:
```bash
cp beta/obj/backup_master_2026-03-31/betaflight_SITL_master_4.5.0-949.elf \
   beta/obj/main/betaflight_SITL.elf
```

## Durchgeführte Änderungen

### 1. Git Checkout
```bash
cd beta && git checkout 4.5.1
```

### 2. macOS Build-Patches (in `beta/mk/mcu/SITL.mk`)
- Linux-Linker-Flags (`-gc-sections`, `--cref`, `-lrt`) durch macOS-kompatible ersetzt
- `-Ofast` → `-O3 -ffast-math` (Apple Clang 21.x Kompatibilität)
- macOS-Block mit `ifeq ($(OSFAMILY), macosx)` hinzugefügt

### 3. Makefile-Patches (in `beta/Makefile`)
- `-fuse-linker-plugin` für macOS entfernt (nicht von Apple Clang unterstützt)
- `OBJCOPY` → `/opt/homebrew/opt/llvm@20/bin/llvm-objcopy` für macOS
- `$(MACOS_CFLAGS_EXTRA)` Hook für zusätzliche Flags

### 4. `beta/mk/local.mk` (neu erstellt)
- `GCC_REQUIRED_VERSION := 13.3.1` (Override für arm-gnu-toolchain 13.3)
- `-Wno-error -Wno-unknown-warning-option -Wno-deprecated` für Apple Clang

### 5. `tools/start_sitl.sh`
- Toolchain-Pfad wird automatisch gefunden und in PATH gesetzt beim `--rebuild`

## Build-Anleitung

```bash
cd /Users/rudi/Projects/SkyForge/beta
export PATH="$PWD/tools/arm-gnu-toolchain-13.3.rel1-darwin-arm64-arm-none-eabi/bin:$PATH"
make TARGET=SITL
```

Oder mit dem Start-Script (baut automatisch wenn Binary fehlt):
```bash
cd /Users/rudi/Projects/SkyForge
./tools/start_sitl.sh --rebuild
```

## Configurator Verbindung

1. Betaflight Configurator 10.10 (native macOS App) öffnen
2. Verbindungstyp: **TCP**
3. Host: `127.0.0.1`
4. Port: `5761`
5. Verbinden klicken

## Ports Übersicht

| Port | Protokoll | Beschreibung |
|------|-----------|--------------|
| 5761 | TCP | UART1/MSP — Betaflight Configurator |
| 9001 | UDP | PWM Raw Output (RealFlight) |
| 9002 | UDP | PWM Output (Gazebo/XFlight) |
| 9003 | UDP | FDM/State Input (vom Simulator) |
| 9004 | UDP | RC Input (vom Controller) |
