#!/usr/bin/env bash
#
# stop_sitl.sh — Stop Betaflight SITL for SkyForge

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORK_DIR="$PROJECT_DIR/tools/sitl_workdir"
PID_FILE="$WORK_DIR/sitl.pid"

echo "Attempting to stop Betaflight SITL..."

# Check if PID file exists
if [[ -f "$PID_FILE" ]]; then
    SITL_PID=$(cat "$PID_FILE")
    
    # Check if process is still running
    if kill -0 $SITL_PID 2>/dev/null; then
        echo "Stopping SITL process with PID $SITL_PID..."
        kill $SITL_PID
        
        # Wait for process to terminate
        TIMEOUT=10
        COUNT=0
        while kill -0 $SITL_PID 2>/dev/null; do
            sleep 0.5
            COUNT=$((COUNT + 1))
            if [ $COUNT -gt $((TIMEOUT * 2)) ]; then
                echo "WARNING: SITL process did not terminate gracefully, forcing..."
                kill -9 $SITL_PID 2>/dev/null || true
                break
            fi
        done
        
        echo "SITL process stopped."
    else
        echo "No running SITL process found with PID $SITL_PID (already stopped?)"
    fi
    
    # Remove PID file
    rm -f "$PID_FILE"
else
    # Try to find and kill any SITL processes
    if pgrep -f betaflight_SITL > /dev/null 2>&1; then
        echo "Found SITL processes, stopping them..."
        pkill -f betaflight_SITL
        sleep 1
        
        # Force kill if still running
        if pgrep -f betaflight_SITL > /dev/null 2>&1; then
            pkill -9 -f betaflight_SITL
        fi
        
        echo "SITL processes stopped."
    else
        echo "No SITL process found (already stopped?)."
    fi
fi

echo "SITL stopped successfully."
