import socket, struct, time, threading

"""
COMPLETE FIX:
- Send FDM packets on port 9003 with gravity (-9.80665 on Z)
- Send RC packets on port 9004 with correct AETR order
- AUX1 starts LOW, then switches HIGH to arm
"""

sock_rc = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_fdm = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
SITL_RC_PORT = 9004
SITL_FDM_PORT = 9003

stop_sender = False
current_throttle = 1000
current_aux1 = 1000  # START LOW!

def send_rc_and_fdm():
    """Send both RC and FDM at 50Hz"""
    while not stop_sender:
        # RC packet: AETR order
        rc_channels = [1500, 1500, current_throttle, 1500, current_aux1] + [1000]*11
        rc_pkt = struct.pack('<d16H', time.time(), *rc_channels)
        sock_rc.sendto(rc_pkt, ('127.0.0.1', SITL_RC_PORT))
        
        # FDM packet: 144 bytes
        # Gravity in body frame NED: when level, Z acceleration = -9.80665
        timestamp = time.time()
        angular_vel = [0.0, 0.0, 0.0]        # no rotation
        linear_acc = [0.0, 0.0, -9.80665]     # gravity on Z (NED body frame, level)
        orientation = [1.0, 0.0, 0.0, 0.0]    # quaternion identity (level)
        velocity = [0.0, 0.0, 0.0]            # stationary
        position = [0.0, 0.0, 0.0]            # origin
        pressure = 101325.0                     # sea level Pa
        
        fdm_pkt = struct.pack('<d3d3d4d3d3dd',
            timestamp,
            *angular_vel,
            *linear_acc,
            *orientation,
            *velocity,
            *position,
            pressure
        )
        sock_fdm.sendto(fdm_pkt, ('127.0.0.1', SITL_FDM_PORT))
        
        time.sleep(0.02)

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
# START
# ============================================

print("Phase 0: Starting RC+FDM sender (AUX1=LOW, Throttle=LOW)...", flush=True)
sender_thread = threading.Thread(target=send_rc_and_fdm, daemon=True)
sender_thread.start()

# Wait for SITL
print("Waiting for SITL...", flush=True)
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
    print("SITL not ready!", flush=True)
    stop_sender = True
    exit(1)

# Let FDM + RC stabilize for 5 seconds
print("Letting FDM+RC stabilize (5s)...", flush=True)
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

# Check RC values
resp = msp_request(msp, 105)
if resp[0:3] == b'$M>':
    d = resp[5:5+resp[3]]
    for i in range(min(resp[3]//2, 6)):
        val = struct.unpack('<H', d[i*2:i*2+2])[0]
        names = ['Roll', 'Pitch', 'Yaw', 'Throttle', 'AUX1', 'AUX2']
        print(f"  RC {names[i]}: {val}", flush=True)

msp.close()

if not active or active == []:
    can_arm = True
elif active == ['ARM_SWITCH']:
    # ARM_SWITCH means we haven't switched AUX1 high yet - that's expected
    can_arm = True
else:
    can_arm = len(active) <= 1  # Try anyway if only 1 blocker

print(f"\nBlockers remaining: {active}", flush=True)
print(f"Attempting to arm: {'YES' if can_arm else 'NO - too many blockers'}", flush=True)

# ARM!
print("\n=== ARMING: AUX1 -> 1800 ===", flush=True)
current_aux1 = 1800
time.sleep(5)

# Check arming
msp = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
msp.settimeout(5)
msp.connect(('127.0.0.1', 5761))
time.sleep(0.5)

resp = msp_request(msp, 150)
if resp[0:3] == b'$M>':
    d = resp[5:5+resp[3]]
    armed, flags, active = parse_arming_flags(d)
    print(f"\nAFTER ARM COMMAND: Armed={armed}, Flags={flags:#010x}, Blockers={active}", flush=True)

if armed:
    print("\n🚁🎯🚁 DRONE IS ARMED! FLYING! 🚁🎯🚁", flush=True)
    
    # Ramp throttle
    print("Throttle ramp up...", flush=True)
    for thr in range(1000, 1600, 20):
        current_throttle = thr
        time.sleep(0.1)
    
    print(f"Hovering at throttle={current_throttle}...", flush=True)
    time.sleep(5)
    
    # Check status mid-flight
    resp = msp_request(msp, 150)
    if resp[0:3] == b'$M>':
        d = resp[5:5+resp[3]]
        armed2, flags2, active2 = parse_arming_flags(d)
        print(f"MID-FLIGHT: Armed={armed2}, Blockers={active2}", flush=True)
    
    # Land and disarm
    print("Landing...", flush=True)
    for thr in range(1600, 1000, -20):
        current_throttle = thr
        time.sleep(0.1)
    
    current_throttle = 1000
    time.sleep(1)
    current_aux1 = 1000
    time.sleep(2)
    
    resp = msp_request(msp, 150)
    if resp[0:3] == b'$M>':
        d = resp[5:5+resp[3]]
        armed3, flags3, active3 = parse_arming_flags(d)
        print(f"AFTER DISARM: Armed={armed3}", flush=True)
    
    print("\n✅ SUCCESSFUL FLIGHT COMPLETE! ✅", flush=True)
else:
    print(f"\n❌ Still not armed. Remaining blockers: {active}", flush=True)
    print("Need to investigate further.", flush=True)

msp.close()
stop_sender = True
sock_rc.close()
sock_fdm.close()
print("\nDone!", flush=True)
