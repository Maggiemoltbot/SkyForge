#!/usr/bin/env python3
"""
SkyForge First Flight — RC Test Sender
Sends rc_packet structs directly to betaflight SITL on port 9004.

rc_packet layout (40 bytes):
  double timestamp;                       // 8 bytes, seconds
  uint16_t channels[16];                  // 32 bytes

Channel mapping (AETR):
  0: Aileron  (Roll)    — center 1500
  1: Elevator (Pitch)   — center 1500
  2: Throttle           — min 1000, max 2000
  3: Rudder   (Yaw)     — center 1500
  4: AUX1               — ARM switch (>1700 = armed)
  5-15: AUX2-12         — 1000 default
"""

import socket
import struct
import time
import sys

SITL_HOST = "127.0.0.1"
SITL_PORT = 9004

# rc_packet: double + 16x uint16
RC_FMT = "<d16H"  # little-endian: 1 double + 16 unsigned shorts = 40 bytes

def make_rc_packet(channels, t=None):
    """Build a 40-byte rc_packet."""
    if t is None:
        t = time.time()
    # Pad to 16 channels
    ch = list(channels) + [1000] * (16 - len(channels))
    return struct.pack(RC_FMT, t, *ch[:16])

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    
    print(f"[RC-SENDER] Sending to {SITL_HOST}:{SITL_PORT}")
    print(f"[RC-SENDER] Packet size: {struct.calcsize(RC_FMT)} bytes")
    
    start = time.time()
    phase = "IDLE"
    armed = False
    throttle = 1000
    
    # Phase timing
    PHASE_IDLE_END    = 1.0    # 1s idle (establish link)
    PHASE_ARM_END     = 3.0    # ARM at 1s, wait until 3s
    PHASE_THROTTLE_END = 13.0  # Ramp throttle 3s-13s
    PHASE_CRUISE_END  = 18.0   # Hold at target 13s-18s
    PHASE_LAND_END    = 23.0   # Reduce throttle 18s-23s
    PHASE_DISARM_END  = 25.0   # Disarm and stop
    
    TARGET_THROTTLE = 1500     # Should be enough to lift
    
    try:
        while True:
            now = time.time()
            elapsed = now - start
            
            # Default channels: center sticks, no throttle
            aileron  = 1500
            elevator = 1500
            throttle_val = 1000
            rudder   = 1500
            aux1     = 1000  # disarmed
            
            if elapsed < PHASE_IDLE_END:
                # Phase 0: IDLE — send neutral
                if phase != "IDLE":
                    phase = "IDLE"
                    print(f"[{elapsed:.1f}s] Phase: IDLE — sending neutral RC")
                throttle_val = 1000
                aux1 = 1000
                
            elif elapsed < PHASE_ARM_END:
                # Phase 1: ARM — throttle low, AUX1 high
                if phase != "ARM":
                    phase = "ARM"
                    print(f"[{elapsed:.1f}s] Phase: ARM — AUX1=1800, Throttle=1000")
                throttle_val = 1000  # MUST be low for arming!
                aux1 = 1800
                
            elif elapsed < PHASE_THROTTLE_END:
                # Phase 2: THROTTLE RAMP
                if phase != "RAMP":
                    phase = "RAMP"
                    print(f"[{elapsed:.1f}s] Phase: THROTTLE RAMP — going to {TARGET_THROTTLE}")
                progress = (elapsed - PHASE_ARM_END) / (PHASE_THROTTLE_END - PHASE_ARM_END)
                throttle_val = int(1000 + progress * (TARGET_THROTTLE - 1000))
                aux1 = 1800
                
            elif elapsed < PHASE_CRUISE_END:
                # Phase 3: CRUISE — hold throttle
                if phase != "CRUISE":
                    phase = "CRUISE"
                    print(f"[{elapsed:.1f}s] Phase: CRUISE — holding Throttle={TARGET_THROTTLE}")
                throttle_val = TARGET_THROTTLE
                aux1 = 1800
                
            elif elapsed < PHASE_LAND_END:
                # Phase 4: LAND — reduce throttle
                if phase != "LAND":
                    phase = "LAND"
                    print(f"[{elapsed:.1f}s] Phase: LANDING — reducing throttle")
                progress = (elapsed - PHASE_CRUISE_END) / (PHASE_LAND_END - PHASE_CRUISE_END)
                throttle_val = int(TARGET_THROTTLE - progress * (TARGET_THROTTLE - 1000))
                aux1 = 1800
                
            elif elapsed < PHASE_DISARM_END:
                # Phase 5: DISARM
                if phase != "DISARM":
                    phase = "DISARM"
                    print(f"[{elapsed:.1f}s] Phase: DISARM — AUX1=1000")
                throttle_val = 1000
                aux1 = 1000
                
            else:
                print(f"[{elapsed:.1f}s] DONE — Test complete!")
                break
            
            channels = [
                aileron,        # 0: Roll
                elevator,       # 1: Pitch  
                throttle_val,   # 2: Throttle
                rudder,         # 3: Yaw
                aux1,           # 4: AUX1 (ARM)
                1000, 1000, 1000,  # AUX2-4
                1000, 1000, 1000, 1000,  # AUX5-8
                1000, 1000, 1000, 1000,  # AUX9-12
            ]
            
            pkt = make_rc_packet(channels, elapsed)
            sock.sendto(pkt, (SITL_HOST, SITL_PORT))
            
            # Log every 0.5s
            if int(elapsed * 2) != int((elapsed - 0.02) * 2):
                print(f"  [{elapsed:.1f}s] T={throttle_val} AUX1={aux1} Phase={phase}")
            
            time.sleep(0.02)  # 50 Hz
            
    except KeyboardInterrupt:
        print("\n[RC-SENDER] Interrupted — sending DISARM")
        channels = [1500, 1500, 1000, 1500, 1000] + [1000] * 11
        pkt = make_rc_packet(channels)
        for _ in range(10):
            sock.sendto(pkt, (SITL_HOST, SITL_PORT))
            time.sleep(0.02)
    finally:
        sock.close()
        print("[RC-SENDER] Socket closed")

if __name__ == "__main__":
    main()
