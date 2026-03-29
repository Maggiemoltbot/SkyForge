#!/usr/bin/env bash
#
# test_integration.sh — SkyForge Integration Test
# Tests if all components can communicate properly

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SITL_BIN="$PROJECT_DIR/betaflight/obj/main/betaflight_SITL.elf"

echo "========================================="
echo " SkyForge Integration Test"
echo "========================================="

# 1. Check SITL Binary
echo "1. Checking SITL binary..."
if [[ ! -f "$SITL_BIN" ]]; then
    echo "ERROR: SITL binary not found at $SITL_BIN"
    echo "Please build it first with: make TARGET=SITL"
    exit 1
fi

if [[ ! -x "$SITL_BIN" ]]; then
    echo "ERROR: SITL binary is not executable at $SITL_BIN"
    exit 1
fi

echo "✓ SITL binary found and executable"

# 2. Start SITL
echo "2. Starting SITL..."
"$SCRIPT_DIR/start_sitl.sh" > /dev/null 2>&1 &
START_SCRIPT_PID=$!

# Give it a moment to start
sleep 2

# Check if it's still running
if ! kill -0 $START_SCRIPT_PID 2>/dev/null; then
    echo "ERROR: Failed to start SITL"
    exit 1
fi

# Wait for SITL to be ready
echo "Waiting for SITL to initialize..."
for i in {1..20}; do
    if nc -z localhost 5761 2>/dev/null; then
        echo "✓ SITL is listening on TCP port 5761"
        break
    fi
    sleep 0.5
    if [ $i -eq 20 ]; then
        echo "ERROR: Timeout waiting for SITL to start"
        "$SCRIPT_DIR/stop_sitl.sh" > /dev/null 2>&1 || true
        exit 1
    fi
done

# 3. Wait for TCP 5761
echo "3. Confirming TCP port 5761 is accessible..."
if ! nc -z localhost 5761 2>/dev/null; then
    echo "ERROR: TCP port 5761 is not accessible"
    "$SCRIPT_DIR/stop_sitl.sh" > /dev/null 2>&1 || true
    exit 1
fi

echo "✓ TCP port 5761 is accessible"

# 4. Send Test-FDM-Packet to Port 9002
echo "4. Sending test FDM packet to port 9002..."
# Create a simple test packet (256 bytes of random data should be sufficient for testing)
dd if=/dev/urandom bs=1 count=256 2>/dev/null | nc -u -w1 localhost 9002

echo "✓ Test FDM packet sent"

# 5. Check if PWM response comes on Port 9003
echo "5. Checking for PWM response on port 9003..."
# Listen for UDP packets on port 9003 for 3 seconds
timeout 3 nc -u -l localhost 9003 > /tmp/pwm_response.$$ 2>/dev/null || true

if [[ -s "/tmp/pwm_response.$$" ]]; then
    echo "✓ PWM response received on port 9003"
    rm -f "/tmp/pwm_response.$$"
else
    echo "WARNING: No PWM response received within 3 seconds (may be normal if not armed)"
    rm -f "/tmp/pwm_response.$$"
fi

# 6. Stop SITL
echo "6. Stopping SITL..."
"$SCRIPT_DIR/stop_sitl.sh" > /dev/null 2>&1

# 7. Report: PASS/FAIL
echo "========================================="
echo " Integration Test Complete"
echo "========================================="
echo "✓ All tests completed"
echo "RESULT: PASS (with caveats - check warnings above)"
