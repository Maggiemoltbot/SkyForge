#!/usr/bin/env python3
"""
SkyForge Closed-Loop FDM Simulator
===================================
Standalone Python physics loop that replaces Unity for SITL testing.
Sends FDM state → SITL processes → reads PWM back → updates physics → repeat.

Usage:
    python3 closed_loop_sim.py [--duration 30] [--arm] [--verbose]

Ports (matching BridgeConfig):
    FDM Send:    UDP 9003 (→ SITL)
    PWM Receive: UDP 9002 (← SITL)  
    RC Send:     UDP 9004 (→ SITL)
"""

import socket
import struct
import time
import math
import argparse
import threading
import sys
import signal
import numpy as np
from scipy.spatial.transform import Rotation as R

# Betaflight SITL FDM packet format (fdm_packet_t)
# See: src/main/target/SITL/sitl_state.c
FDM_STRUCT = struct.Struct('<'  # little-endian
    'd'    # timestamp (double, seconds)
    'ddd'  # imu_angular_velocity_rpy (rad/s) [roll, pitch, yaw]
    'ddd'  # imu_linear_acceleration_xyz (m/s²) [x, y, z]  
    'dddd' # quaternion [x, y, z, w]
    'ddd'  # velocity [x, y, z] (m/s)
    'ddd'  # position [x, y, z] (m)
    'd'    # pressure (Pa)
)

# PWM packet from SITL: 16 x float32 motor values
PWM_STRUCT = struct.Struct('<' + 'f' * 16)

# RC packet: 16 x uint16 channels
RC_STRUCT = struct.Struct('<' + 'H' * 16)


class DronePhysics:
    """Simple quadrotor physics with realistic parameters."""
    
    def __init__(self):
        # Physical parameters
        self.mass = 0.5          # kg
        self.arm_length = 0.12   # m (motor to center)
        self.gravity = 9.81      # m/s²
        
        # Motor parameters
        self.max_thrust_per_motor = 3.0  # N (each motor)
        self.motor_kv = 2300     # RPM/V
        self.prop_diameter = 0.076  # m (3 inch)
        
        # Inertia tensor (approximated as rectangular box)
        w, h, d_body = 0.24, 0.03, 0.24  # width, height, depth
        self.Ixx = (1/12) * self.mass * (h**2 + d_body**2)
        self.Iyy = (1/12) * self.mass * (w**2 + d_body**2)
        self.Izz = (1/12) * self.mass * (w**2 + h**2)
        
        # Drag coefficients
        self.linear_drag = 0.1
        self.angular_drag = 0.5
        
        # State
        self.position = np.array([0.0, 0.0, 1.0])  # Start at 1m height
        self.velocity = np.array([0.0, 0.0, 0.0])
        self.orientation = R.from_quat([0, 0, 0, 1])  # identity (x,y,z,w)
        self.angular_velocity = np.array([0.0, 0.0, 0.0])  # body frame rad/s
        
        # Motor PWM (0.0 to 1.0)
        self.motor_pwm = np.array([0.0, 0.0, 0.0, 0.0])
        
    def motor_thrust(self, pwm_val):
        """Convert PWM (0-1) to thrust (N)."""
        # Quadratic thrust curve
        return self.max_thrust_per_motor * pwm_val ** 2
    
    def step(self, dt):
        """Advance physics by dt seconds."""
        # Motor thrusts
        thrusts = np.array([self.motor_thrust(p) for p in self.motor_pwm])
        total_thrust = np.sum(thrusts)
        
        # Torques from motor differential thrust
        # BetaFlight motor order: FR(0), BL(1), FL(2), BR(3) — props-in config
        # Roll  = right - left
        # Pitch = front - back  
        # Yaw   = CW - CCW
        L = self.arm_length
        torque_x = L * (thrusts[0] + thrusts[3] - thrusts[1] - thrusts[2])  # roll
        torque_y = L * (thrusts[0] + thrusts[2] - thrusts[1] - thrusts[3])  # pitch
        torque_z = 0.01 * (thrusts[0] + thrusts[1] - thrusts[2] - thrusts[3])  # yaw (reaction torque)
        torques = np.array([torque_x, torque_y, torque_z])
        
        # Angular acceleration (body frame): tau = I * alpha
        angular_accel = np.array([
            torques[0] / self.Ixx,
            torques[1] / self.Iyy,
            torques[2] / self.Izz
        ])
        
        # Angular drag
        angular_accel -= self.angular_drag * self.angular_velocity
        
        # Update angular velocity
        self.angular_velocity += angular_accel * dt
        
        # Update orientation
        angle_change = self.angular_velocity * dt
        if np.linalg.norm(angle_change) > 1e-10:
            rotation_delta = R.from_rotvec(angle_change)
            self.orientation = self.orientation * rotation_delta
        
        # Forces in world frame
        rot_matrix = self.orientation.as_matrix()
        thrust_body = np.array([0.0, 0.0, total_thrust])  # thrust along body Z
        thrust_world = rot_matrix @ thrust_body
        
        gravity_world = np.array([0.0, 0.0, -self.mass * self.gravity])
        drag_world = -self.linear_drag * self.velocity
        
        total_force = thrust_world + gravity_world + drag_world
        
        # Linear acceleration
        linear_accel = total_force / self.mass
        
        # Update velocity and position
        self.velocity += linear_accel * dt
        self.position += self.velocity * dt
        
        # Ground collision
        if self.position[2] < 0:
            self.position[2] = 0
            self.velocity[2] = max(0, self.velocity[2])
            # Dampen horizontal velocity on ground
            self.velocity[0] *= 0.95
            self.velocity[1] *= 0.95
        
        return linear_accel
    
    def get_imu_data(self, linear_accel_world):
        """Get IMU readings in body frame."""
        rot_inv = self.orientation.inv().as_matrix()
        
        # Linear acceleration in body frame (includes gravity as accelerometer would)
        # Accelerometer measures: accel - gravity (in body frame)
        gravity_world = np.array([0.0, 0.0, -self.gravity])
        specific_force = linear_accel_world - gravity_world  # What the accelerometer "feels"
        accel_body = rot_inv @ specific_force
        
        return self.angular_velocity.copy(), accel_body


class ClosedLoopSim:
    """Main simulation loop connecting Python physics ↔ Betaflight SITL."""
    
    def __init__(self, host='127.0.0.1', fdm_port=9003, pwm_port=9002, rc_port=9004):
        self.host = host
        self.fdm_port = fdm_port
        self.pwm_port = pwm_port
        self.rc_port = rc_port
        
        self.physics = DronePhysics()
        self.running = False
        self.start_time = 0
        
        # Stats
        self.fdm_sent = 0
        self.pwm_received = 0
        self.rc_sent = 0
        
        # PWM receive buffer (thread-safe)
        self.latest_pwm = np.zeros(16)
        self.pwm_lock = threading.Lock()
        
        # Setup sockets
        self.fdm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.fdm_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        self.pwm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.pwm_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.pwm_sock.bind(('0.0.0.0', pwm_port))
        self.pwm_sock.settimeout(0.001)  # non-blocking
        
        self.rc_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.rc_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
    def send_fdm(self):
        """Pack and send FDM state to SITL."""
        q = self.physics.orientation.as_quat()  # [x, y, z, w]
        
        packet = FDM_STRUCT.pack(
            time.time(),                                              # timestamp
            self.physics.angular_velocity[0],                         # gyro roll
            self.physics.angular_velocity[1],                         # gyro pitch
            self.physics.angular_velocity[2],                         # gyro yaw
            self.physics.last_accel[0] if hasattr(self.physics, 'last_accel') else 0,  # accel x
            self.physics.last_accel[1] if hasattr(self.physics, 'last_accel') else 0,  # accel y
            self.physics.last_accel[2] if hasattr(self.physics, 'last_accel') else 0,  # accel z
            q[0], q[1], q[2], q[3],                                 # quaternion
            self.physics.velocity[0],                                 # vel x
            self.physics.velocity[1],                                 # vel y
            self.physics.velocity[2],                                 # vel z
            self.physics.position[0],                                 # pos x
            self.physics.position[1],                                 # pos y
            self.physics.position[2],                                 # pos z
            101325.0 - (self.physics.position[2] * 12.0)            # pressure (Pa, approx)
        )
        
        self.fdm_sock.sendto(packet, (self.host, self.fdm_port))
        self.fdm_sent += 1
        
    def receive_pwm(self):
        """Non-blocking receive of PWM data from SITL."""
        try:
            data, addr = self.pwm_sock.recvfrom(256)
            if len(data) >= PWM_STRUCT.size:
                values = PWM_STRUCT.unpack_from(data)
                with self.pwm_lock:
                    self.latest_pwm[:] = values
                self.pwm_received += 1
                return True
        except socket.timeout:
            pass
        except OSError:
            pass
        return False
    
    def send_rc(self, channels):
        """Send RC channel data to SITL."""
        ch = list(channels) + [1500] * (16 - len(channels))
        ch = [max(1000, min(2000, int(c))) for c in ch[:16]]
        packet = RC_STRUCT.pack(*ch)
        self.rc_sock.sendto(packet, (self.host, self.rc_port))
        self.rc_sent += 1
        
    def run(self, duration=30.0, arm=False, verbose=False):
        """Main simulation loop."""
        self.running = True
        self.start_time = time.time()
        dt = 1.0 / 400  # 400 Hz to match BridgeConfig.updateFrequency
        
        print(f"╔═══════════════════════════════════════════╗")
        print(f"║  SkyForge Closed-Loop FDM Simulator      ║")
        print(f"╠═══════════════════════════════════════════╣")
        print(f"║  FDM → SITL:  {self.host}:{self.fdm_port}           ║")
        print(f"║  PWM ← SITL:  0.0.0.0:{self.pwm_port}              ║")
        print(f"║  RC  → SITL:  {self.host}:{self.rc_port}           ║")
        print(f"║  Physics dt:  {dt*1000:.1f} ms ({1/dt:.0f} Hz)              ║")
        print(f"║  Duration:    {duration:.0f}s                        ║")
        print(f"║  Auto-arm:    {'YES' if arm else 'NO'}                        ║")
        print(f"╚═══════════════════════════════════════════╝")
        print()
        
        last_print = time.time()
        loop_count = 0
        arm_phase = 0  # 0=idle, 1=disarmed-wait, 2=arming, 3=armed
        arm_start_time = 0
        
        try:
            while self.running and (time.time() - self.start_time) < duration:
                loop_start = time.time()
                
                # 1. Receive PWM from SITL
                for _ in range(5):  # drain buffer
                    if not self.receive_pwm():
                        break
                
                # 2. Update motor speeds from PWM
                with self.pwm_lock:
                    # SITL sends motor values as floats 0-1
                    self.physics.motor_pwm = np.clip(self.latest_pwm[:4], 0, 1)
                
                # 3. Step physics
                accel = self.physics.step(dt)
                self.physics.last_accel = self.physics.get_imu_data(accel)[1]
                
                # 4. Send FDM to SITL
                self.send_fdm()
                
                # 5. Send RC (arming sequence or neutral)
                if arm:
                    elapsed = time.time() - self.start_time
                    if arm_phase == 0 and elapsed > 0.5:
                        # Phase 1: Send neutral for a bit
                        arm_phase = 1
                        arm_start_time = time.time()
                        print("→ Sending neutral RC...")
                    elif arm_phase == 1:
                        self.send_rc([1500, 1500, 1000, 1500, 1000])  # neutral, throttle low
                        if time.time() - arm_start_time > 2.0:
                            arm_phase = 2
                            arm_start_time = time.time()
                            print("→ Arming: throttle low + yaw right...")
                    elif arm_phase == 2:
                        # Arm: hold throttle low + yaw full right
                        self.send_rc([1500, 1500, 1000, 2000, 1800])  # yaw right, AUX1 high
                        if time.time() - arm_start_time > 1.5:
                            arm_phase = 3
                            print("→ Armed! Sending hover throttle...")
                    elif arm_phase == 3:
                        # Armed: gentle throttle
                        t = time.time() - arm_start_time
                        throttle = min(1500, 1200 + int(t * 100))
                        self.send_rc([1500, 1500, throttle, 1500, 1800])
                else:
                    # Just send neutral
                    if loop_count % 40 == 0:  # ~10 Hz RC rate
                        self.send_rc([1500, 1500, 1000, 1500, 1000])
                
                loop_count += 1
                
                # 6. Print status every second
                now = time.time()
                if now - last_print >= 1.0:
                    pos = self.physics.position
                    vel = self.physics.velocity
                    euler = self.physics.orientation.as_euler('xyz', degrees=True)
                    pwm = self.physics.motor_pwm
                    
                    elapsed = now - self.start_time
                    
                    status = (
                        f"[{elapsed:5.1f}s] "
                        f"Pos:({pos[0]:+6.2f},{pos[1]:+6.2f},{pos[2]:+6.2f}) "
                        f"Vel:({vel[0]:+5.2f},{vel[1]:+5.2f},{vel[2]:+5.2f}) "
                        f"RPY:({euler[0]:+5.1f}°,{euler[1]:+5.1f}°,{euler[2]:+5.1f}°) "
                        f"M:[{pwm[0]:.2f},{pwm[1]:.2f},{pwm[2]:.2f},{pwm[3]:.2f}] "
                        f"FDM:{self.fdm_sent} PWM:{self.pwm_received} RC:{self.rc_sent}"
                    )
                    
                    if verbose:
                        print(status)
                    else:
                        sys.stdout.write(f"\r{status}")
                        sys.stdout.flush()
                    
                    last_print = now
                
                # 7. Sleep to maintain loop rate
                elapsed_loop = time.time() - loop_start
                sleep_time = dt - elapsed_loop
                if sleep_time > 0:
                    time.sleep(sleep_time)
                    
        except KeyboardInterrupt:
            print("\n\nInterrupted by user.")
        finally:
            self.running = False
            elapsed = time.time() - self.start_time
            print(f"\n\n{'='*50}")
            print(f"Simulation complete!")
            print(f"Duration: {elapsed:.1f}s")
            print(f"FDM sent: {self.fdm_sent} ({self.fdm_sent/max(1,elapsed):.0f}/s)")
            print(f"PWM recv: {self.pwm_received} ({self.pwm_received/max(1,elapsed):.0f}/s)")
            print(f"RC sent:  {self.rc_sent}")
            print(f"Final pos: ({self.physics.position[0]:.3f}, {self.physics.position[1]:.3f}, {self.physics.position[2]:.3f})")
            print(f"{'='*50}")
            
            self.fdm_sock.close()
            self.pwm_sock.close()
            self.rc_sock.close()


def main():
    parser = argparse.ArgumentParser(description='SkyForge Closed-Loop FDM Simulator')
    parser.add_argument('--duration', type=float, default=30.0, help='Simulation duration (seconds)')
    parser.add_argument('--arm', action='store_true', help='Auto-arm the drone and attempt hover')
    parser.add_argument('--verbose', action='store_true', help='Print each status line (vs overwrite)')
    parser.add_argument('--host', default='127.0.0.1', help='SITL host')
    args = parser.parse_args()
    
    sim = ClosedLoopSim(host=args.host)
    
    # Handle SIGINT gracefully
    def handler(sig, frame):
        sim.running = False
    signal.signal(signal.SIGINT, handler)
    
    sim.run(duration=args.duration, arm=args.arm, verbose=args.verbose)


if __name__ == '__main__':
    main()
