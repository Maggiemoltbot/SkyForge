#!/usr/bin/env python3
"""
SkyForge First Flight — RC Test Sender v2
Fixed channel mapping: BF map is [0,1,3,2,4,5,6,7]
So raw channels must be: [Roll, Pitch, Yaw, Throttle, AUX1, ...]
(AERT order, NOT AETR!)

rc_packet layout (40 bytes):
  double timestamp;                       // 8 bytes
  uint16_t channels[16];                  // 32 bytes
"""

import socket
import struct
import time

SITL_HOST = "127.0.0.1"
SITL_PORT = 9004
RC_FMT = "<d16H"

def make_rc_packet(channels, t=None):
    if t is None:
        t = time.time()
    ch = list(channels) + [1000] * (16 - len(channels))
    return struct.pack(RC_FMT, t, *ch[:16])

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    
    print(f"[RC-SENDER-v2] Sending to {SITL_HOST}:{SITL_PORT}")
    print(f"[RC-SENDER-v2] Channel order: AERT (Roll, Pitch, Yaw, Throttle)")
    print(f"[RC-SENDER-v2] BF map: [0,1,3,2,4,5,6,7] → Throttle=raw[3], Yaw=raw[2]")
    
    start = time.time()
    phase = ""
    
    PHASE_IDLE_END     = 1.0
    PHASE_ARM_END      = 4.0    # give it 3 seconds to arm
    PHASE_THROTTLE_END = 14.0
    PHASE_CRUISE_END   = 19.0
    PHASE_LAND_END     = 24.0
    PHASE_DISARM_END   = 26.0
    
    TARGET_THROTTLE = 1500
    
    try:
        while True:
            now = time.time()
            elapsed = now - start
            
            roll     = 1500
            pitch    = 1500
            yaw      = 1500  # center
            throttle = 1000  # low
            aux1     = 1000  # disarmed
            
            if elapsed < PHASE_IDLE_END:
                if phase != "IDLE":
                    phase = "IDLE"
                    print(f"[{elapsed:.1f}s] Phase: IDLE")
                throttle = 1000
                aux1 = 1000
                
            elif elapsed < PHASE_ARM_END:
                if phase != "ARM":
                    phase = "ARM"
                    print(f"[{elapsed:.1f}s] Phase: ARM — AUX1=1800, Throttle=1000 (raw ch3)")
                throttle = 1000
                aux1 = 1800
                
            elif elapsed < PHASE_THROTTLE_END:
                if phase != "RAMP":
                    phase = "RAMP"
                    print(f"[{elapsed:.1f}s] Phase: THROTTLE RAMP → {TARGET_THROTTLE}")
                progress = (elapsed - PHASE_ARM_END) / (PHASE_THROTTLE_END - PHASE_ARM_END)
                throttle = int(1000 + progress * (TARGET_THROTTLE - 1000))
                aux1 = 1800
                
            elif elapsed < PHASE_CRUISE_END:
                if phase != "CRUISE":
                    phase = "CRUISE"
                    print(f"[{elapsed:.1f}s] Phase: CRUISE — Throttle={TARGET_THROTTLE}")
                throttle = TARGET_THROTTLE
                aux1 = 1800
                
            elif elapsed < PHASE_LAND_END:
                if phase != "LAND":
                    phase = "LAND"
                    print(f"[{elapsed:.1f}s] Phase: LANDING")
                progress = (elapsed - PHASE_CRUISE_END) / (PHASE_LAND_END - PHASE_CRUISE_END)
                throttle = int(TARGET_THROTTLE - progress * (TARGET_THROTTLE - 1000))
                aux1 = 1800
                
            elif elapsed < PHASE_DISARM_END:
                if phase != "DISARM":
                    phase = "DISARM"
                    print(f"[{elapsed:.1f}s] Phase: DISARM")
                throttle = 1000
                aux1 = 1000
            else:
                print(f"[{elapsed:.1f}s] DONE!")
                break
            
            # CRITICAL: Channel order is AERT for this BF config
            # raw[0]=Roll, raw[1]=Pitch, raw[2]=Yaw, raw[3]=Throttle
            # BF maps: rcData[0]←raw[0], rcData[1]←raw[1], rcData[2]←raw[3], rcData[3]←raw[2]
            channels = [
                roll,       # raw[0] → rcData[0] (Roll)
                pitch,      # raw[1] → rcData[1] (Pitch)
                yaw,        # raw[2] → rcData[3] (Yaw)
                throttle,   # raw[3] → rcData[2] (Throttle) ← THIS IS THE FIX
                aux1,       # raw[4] → rcData[4] (AUX1=ARM)
                1000, 1000, 1000,
                1000, 1000, 1000, 1000,
                1000, 1000, 1000, 1000,
            ]
            
            pkt = make_rc_packet(channels, elapsed)
            sock.sendto(pkt, (SITL_HOST, SITL_PORT))
            
            if int(elapsed * 2) != int((elapsed - 0.02) * 2):
                print(f"  [{elapsed:.1f}s] T(raw3)={throttle} Y(raw2)={yaw} AUX1={aux1} Phase={phase}")
            
            time.sleep(0.02)
            
    except KeyboardInterrupt:
        print("\n[RC-SENDER] Interrupted — DISARM")
        channels = [1500, 1500, 1500, 1000, 1000] + [1000] * 11
        pkt = make_rc_packet(channels)
        for _ in range(10):
            sock.sendto(pkt, (SITL_HOST, SITL_PORT))
            time.sleep(0.02)
    finally:
        sock.close()
        print("[RC-SENDER] Done")

if __name__ == "__main__":
    main()
