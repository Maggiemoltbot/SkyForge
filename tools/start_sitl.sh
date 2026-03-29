#!/usr/bin/env bash
#
# start_sitl.sh — Start Betaflight SITL for SkyForge
#
# Usage:
#   ./tools/start_sitl.sh [--ip <address>] [--config <file>] [--rebuild]
#
# Ports:
#   UDP 9001  — PWM Raw Output (RealFlight)
#   UDP 9002  — PWM Output (Gazebo/XFlight)
#   UDP 9003  — FDM/State Input (from simulator)
#   UDP 9004  — RC Input (from controller)
#   TCP 5761  — UART1/MSP (Betaflight Configurator)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BF_DIR="$PROJECT_DIR/betaflight"
SITL_BIN="$BF_DIR/obj/main/betaflight_SITL.elf"
WORK_DIR="$PROJECT_DIR/tools/sitl_workdir"

# Parse arguments
REBUILD=false
EXTRA_ARGS=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --rebuild)
            REBUILD=true
            shift
            ;;
        *)
            EXTRA_ARGS+=("$1")
            shift
            ;;
    esac
done

# Check if betaflight repo exists
if [[ ! -d "$BF_DIR" ]]; then
    echo "ERROR: Betaflight repository not found at $BF_DIR"
    echo "Clone it first: cd $PROJECT_DIR && git clone https://github.com/betaflight/betaflight.git"
    exit 1
fi

# Rebuild if requested or binary missing
if [[ "$REBUILD" == true ]] || [[ ! -f "$SITL_BIN" ]]; then
    echo "Building Betaflight SITL..."
    cd "$BF_DIR"
    make TARGET=SITL
    echo "Build complete."
fi

# Verify binary exists
if [[ ! -x "$SITL_BIN" ]]; then
    echo "ERROR: SITL binary not found at $SITL_BIN"
    echo "Run with --rebuild to compile."
    exit 1
fi

# Kill any existing SITL instance
if pgrep -f betaflight_SITL > /dev/null 2>&1; then
    echo "Stopping existing SITL instance..."
    pkill -f betaflight_SITL
    sleep 1
fi

# Create working directory (for eeprom.bin etc.)
mkdir -p "$WORK_DIR"
cd "$WORK_DIR"

echo "========================================="
echo " Betaflight SITL — SkyForge"
echo "========================================="
echo " Binary:  $SITL_BIN"
echo " Workdir: $WORK_DIR"
echo " Version: $(basename "$(ls "$BF_DIR/obj/betaflight_"*"_SITL" 2>/dev/null | head -1)" 2>/dev/null || echo 'unknown')"
echo ""
echo " UDP Ports:"
echo "   9001 → PWM Raw Output (RealFlight)"
echo "   9002 → PWM Output (Gazebo/XFlight)"
echo "   9003 ← FDM/State Input (from simulator)"
echo "   9004 ← RC Input"
echo " TCP Ports:"
echo "   5761 → UART1/MSP (Configurator)"
echo ""
echo " Configurator: Connect via TCP → 127.0.0.1:5761"
echo "========================================="
echo ""

# Start SITL
exec "$SITL_BIN" "${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}"
