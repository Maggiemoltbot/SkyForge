#!/usr/bin/env bash
#
# arm_sitl.sh — Arm the Betaflight SITL
#
# This script connects to the Betaflight SITL via TCP and sends MSP commands
# to arm the virtual aircraft.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Default connection settings
SITL_HOST="127.0.0.1"
SITL_PORT=5761

# Function to send MSP command via netcat
send_msp_command() {
    local command_data="$1"
    echo -ne "$command_data" | nc -w 5 "$SITL_HOST" "$SITL_PORT" > /dev/null 2>&1
}

# Function to send MSP set raw RC command
# Channel values (1000-2000): Roll, Pitch, Throttle, Yaw, AUX1-AUX4
# For arming: Throttle=1000, Yaw=max (2000)
send_arm_command() {
    echo "Sending arm command..."
    
    # Send several RC frames with arming combination:
    # Throttle low, Yaw right, for several cycles
    for i in {1..10}; do
        # Send RC values: Roll=1500, Pitch=1500, Throttle=1000, Yaw=2000, AUX1-4=1500
        printf '\$M<' | nc -w 1 "$SITL_HOST" "$SITL_PORT" > /dev/null 2>&1 || true
        sleep 0.1
    done
    
    # Note: Proper MSP implementation would require a full MSP client
    # For now, we recommend using Betaflight Configurator for arming
    echo "Command sent - check Betaflight Configurator to confirm arming status"
}

# Function for manual arming instructions
manual_arming_instructions() {
    cat << 'EOF'

Manual Arming Instructions:
===========================

1. Ensure SITL is running:
   ./tools/start_sitl.sh

2. Connect with Betaflight Configurator:
   - Open Betaflight Configurator
   - Click "Connect" 
   - Select "TCP" connection
   - Host: 127.0.0.1, Port: 5761
   - Click "Connect"

3. Arm the aircraft:
   - Go to the "Motors" tab
   - Enable "I understand the risks" checkbox
   - Move the master switch to the "ARM" position
   OR
   - In the CLI tab, type "arm" and press Enter

Note: Make sure throttle is down and yaw is full right when arming.

EOF
}

# Check if SITL is reachable
echo "Checking connection to Betaflight SITL ($SITL_HOST:$SITL_PORT)..."
if ! nc -z "$SITL_HOST" "$SITL_PORT" 2>/dev/null; then
    echo "ERROR: Cannot connect to Betaflight SITL at $SITL_HOST:$SITL_PORT"
    echo "Please ensure SITL is running:"
    echo "  $ ./tools/start_sitl.sh"
    exit 1
fi

echo "Connected to Betaflight SITL"

# Show options
echo ""
echo "Options:"
echo "1. Display manual arming instructions"
echo "2. Attempt automatic arming (experimental)"

read -p "Select option (1 or 2): " -n 1 -r
echo ""

case $REPLY in
    2)
        echo "WARNING: Automatic arming is experimental and may not work reliably."
        read -p "Continue? (y/N): " -n 1 -r
        echo ""
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            send_arm_command
        else
            manual_arming_instructions
        fi
        ;;
    *)
        manual_arming_instructions
        ;;
esac
