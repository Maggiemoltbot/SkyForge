#!/usr/bin/env python3
"""
fly_unity_loop.py — RC-only controller for Unity physics loop test

This script sends ONLY RC commands to BF SITL (port 9004).
Unity's FlightDynamicsBridge handles FDM sending/receiving.

Flow:
  Controller (this script) → RC → SITL (port 9004)
  Unity → FDM → SITL (port 9003) → PWM → Unity (port 9002)

Usage:
  python3 fly_unity_loop.py
"""

import socket, struct, time, threading

sock_rc = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
SITL_RC_PORT = 9004

stop_sender = False
current_throttle = 1000
current_aux1 = 1000  # START LOW for arm safety

def send_rc():
    """Send RC at 100Hz — only RC, no FDM (Unity sends FDM)"""
    while not stop_sender:
        # AETR order: Aileron(Roll), Elevator(Pitch), Throttle, Rudder(Yaw), AUX1, ...
        rc_channels = [1500, 1500, current_throttle, 1500, current_aux1] + [1000]*11
        rc_pkt = struct.pack('<d16H', time.time(), *rc_channels)
        sock_rc.sendto(rc_pkt, ('127.0.0.1', SITL_RC_PORT))
        time.sleep(0.01)  # 100Hz

def msp_request(msp_sock, cmd):
    checksum = 0 ^ cmd
    msp_sock.send(b'$M<' + bytes([0, cmd, checksum]))
    time.sleep(0.5)
    return msp_sock.recv(4096)

def parse_arming_flags(data):
    offset = 0
    offset += 2  # cycleTime
    offset += 2  # i2cErrors
    offset += 2  # sensors
    flags32 = struct.unpack_from('<I', data, offset)[0]; offset += 4
    offset += 1  # pidProfile
    offset += 2  # systemLoad
    offset += 1  # PID_PROFILE_COUNT
    offset += 1  # currentRateProfile
    extra_bytes = data[offset]; offset += 1
    offset += extra_bytes
    arming_count = data[offset]; offset += 1
    arming32 = struct.unpack_from('<I', data, offset)[0]; offset += 4
    
    armed = bool(flags32 & 0x01)
    
    bf_flags = [
        'NOGYRO', 'FAILSAFE', 'RXLOSS', 'NOT_DISARMED', 
        'BOXFAILSAFE', 'RUNAWAY', 'CRASH', 'THROTTLE',
        'ANGLE', 'BOOTGRACE', 'NOPREARM', 'LOAD',
        'CALIBRATING', 'CLI', 'CMS', 'BST',
        'MSP', 'PARALYZE', 'GPS', 'RESC',
        'RPMFILTER', 'REBOOT_REQD', 'DSHOT_BBANG', 'NO_ACC_CAL',
        'MOTOR_PROTO', 'ARM_SWITCH'
    ]
    active = []
    for i in range(26):
        if arming32 & (1 << i):
            name = bf_flags[i] if i < len(bf_flags) else f'BIT{i}'
            active.append(name)
    
    return armed, arming32, active

# ============================================
# MAIN
# ============================================

print("=" * 60)
print("  SkyForge Unity Physics Loop Test")
print("  RC-only mode (Unity handles FDM)")
print("=" * 60)

print("\nPhase 0: Starting RC sender (AUX1=LOW, Throttle=LOW)...", flush=True)
sender_thread = threading.Thread(target=send_rc, daemon=True)
sender_thread.start()

# Wait for SITL
print("Waiting for SITL TCP on port 5761...", flush=True)
for attempt in range(30):
    try:
        test = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        test.settimeout(1)
        test.connect(('127.0.0.1', 5761))
        test.close()
        print(f"SITL ready!", flush=True)
        break
    except:
        time.sleep(1)
else:
    print("SITL not ready after 30s!", flush=True)
    stop_sender = True
    exit(1)

# Let RC stabilize
print("Letting RC stabilize (5s)...", flush=True)
time.sleep(5)

# Check status
msp = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
msp.settimeout(5)
msp.connect(('127.0.0.1', 5761))
time.sleep(0.5)

resp = msp_request(msp, 150)
if resp[0:3] == b'$M>':
    d = resp[5:5+resp[3]]
    armed, flags, active = parse_arming_flags(d)
    print(f"\nBEFORE ARM: Armed={armed}, Flags={flags:#010x}, Blockers={active}", flush=True)

# Check RC
resp = msp_request(msp, 105)
if resp[0:3] == b'$M>':
    d = resp[5:5+resp[3]]
    for i in range(min(resp[3]//2, 6)):
        val = struct.unpack('<H', d[i*2:i*2+2])[0]
        names = ['Roll', 'Pitch', 'Yaw', 'Throttle', 'AUX1', 'AUX2']
        print(f"  RC {names[i]}: {val}", flush=True)

msp.close()

# ARM
print(f"\nBlockers before arm: {active}", flush=True)
print("\n=== ARMING: AUX1 -> 1800 ===", flush=True)
current_aux1 = 1800
time.sleep(5)

# Check arming
msp = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
msp.settimeout(5)
msp.connect(('127.0.0.1', 5761))
time.sleep(0.5)

resp = msp_request(msp, 150)
armed = False
if resp[0:3] == b'$M>':
    d = resp[5:5+resp[3]]
    armed, flags, active = parse_arming_flags(d)
    print(f"\nAFTER ARM: Armed={armed}, Flags={flags:#010x}, Blockers={active}", flush=True)

if armed:
    print("\n🚁🎯🚁 DRONE IS ARMED! Starting throttle ramp... 🚁🎯🚁", flush=True)
    
    # Gentle throttle ramp
    print("Throttle ramp (1000 → 1500)...", flush=True)
    for thr in range(1000, 1500, 10):
        current_throttle = thr
        time.sleep(0.05)
    
    print(f"Hovering at throttle={current_throttle}...", flush=True)
    
    # Hold for 10 seconds and monitor
    for check in range(10):
        time.sleep(1)
        
        # Check motor values via MSP_MOTOR (cmd 104)
        try:
            resp = msp_request(msp, 104)
            if resp[0:3] == b'$M>':
                d = resp[5:5+resp[3]]
                motors = []
                for i in range(min(resp[3]//2, 4)):
                    val = struct.unpack('<H', d[i*2:i*2+2])[0]
                    motors.append(val)
                print(f"  [t+{check+1}s] Motors: {motors}", flush=True)
        except:
            pass
    
    # Land and disarm
    print("\nLanding...", flush=True)
    for thr in range(1500, 1000, -10):
        current_throttle = thr
        time.sleep(0.05)
    
    current_throttle = 1000
    time.sleep(1)
    current_aux1 = 1000
    time.sleep(2)
    
    resp = msp_request(msp, 150)
    if resp[0:3] == b'$M>':
        d = resp[5:5+resp[3]]
        armed3, flags3, active3 = parse_arming_flags(d)
        print(f"AFTER DISARM: Armed={armed3}", flush=True)
    
    print("\n✅ UNITY PHYSICS LOOP TEST COMPLETE! ✅", flush=True)
else:
    print(f"\n❌ Still not armed. Blockers: {active}", flush=True)
    print("Check: Is Unity in Play mode? Is FlightDynamicsBridge active?", flush=True)
    print("The loop requires: Unity FDM → SITL → PWM → Unity", flush=True)

msp.close()
stop_sender = True
sock_rc.close()
print("\nDone!", flush=True)
