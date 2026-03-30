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
BF_DIR="$PROJECT_DIR/beta"
SITL_BIN="$BF_DIR/obj/main/betaflight_SITL.elf"
WORK_DIR="$PROJECT_DIR/tools/sitl_workdir"
PID_FILE="$WORK_DIR/sitl.pid"

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

# Function to check if ports are free
check_ports_free() {
    local ports=(9001 9002 9003 9004 5761)
    for port in "${ports[@]}"; do
        if lsof -i :$port -t > /dev/null 2>&1; then
            echo "ERROR: Port $port is already in use"
            return 1
        fi
    done
    return 0
}

# Function to wait for TCP port to become available
wait_for_port() {
    local port=$1
    local timeout=${2:-10}
    local count=0
    
    echo "Waiting for SITL to listen on TCP port $port..."
    
    while ! nc -z localhost $port 2>/dev/null; do
        sleep 0.5
        count=$((count + 1))
        if [ $count -gt $((timeout * 2)) ]; then
            echo "ERROR: Timeout waiting for SITL to listen on port $port"
            return 1
        fi
    done
    
    echo "SITL is now listening on TCP port $port"
    return 0
}

# Check if betaflight repo exists
if [[ ! -d "$BF_DIR" ]]; then
    echo "ERROR: Betaflight build directory not found at $BF_DIR"
    echo "Clone it first: cd $PROJECT_DIR && git clone https://github.com/betaflight/betaflight.git beta"
    exit 1
fi

# Rebuild if requested or binary missing
if [[ "$REBUILD" == true ]] || [[ ! -f "$SITL_BIN" ]]; then
    echo "Building Betaflight SITL..."
    cd "$BF_DIR"
    make TARGET=SITL
    echo "Build complete."
fi

# Verify binary exists and is executable
if [[ ! -f "$SITL_BIN" ]]; then
    echo "ERROR: SITL binary not found at $SITL_BIN"
    echo "Run with --rebuild to compile."
    exit 1
fi

if [[ ! -x "$SITL_BIN" ]]; then
    echo "ERROR: SITL binary is not executable at $SITL_BIN"
    exit 1
fi

# Check if ports are free
echo "Checking if required ports are free..."
if ! check_ports_free; then
    echo "Please stop processes using these ports or use different ports."
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

# Start SITL in background
"$SITL_BIN" "${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}" &
SITL_PID=$!

# Save PID to file
echo $SITL_PID > "$PID_FILE"

# Wait for SITL to be ready
if wait_for_port 5761 15; then
    echo "SITL started successfully with PID $SITL_PID"
else
    echo "WARNING: Failed to confirm SITL is listening on port 5761"
    echo "SITL may still be starting or encountered an issue"
fi

echo "SITL is running in the background. Use stop_sitl.sh to stop it."
