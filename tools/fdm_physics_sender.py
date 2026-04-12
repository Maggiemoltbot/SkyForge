import socket
import struct
import time
import threading
import math
import numpy as np

# Configuration
FDM_PORT = 9003
RC_PORT = 9004
PWM_PORT = 9002
SITL_IP = '127.0.0.1'

# Drone parameters
mass = 0.5  # kg
maxThrustPerMotor = 4.0  # N
armLength = 0.12  # m
gravity = 9.81  # m/s^2

# Motor positions (in body frame)
# Front-right (CW), back-right (CCW), back-left (CW), front-left (CCW)
motor_positions = np.array([
    [ armLength,  armLength, 0],  # front-right
    [ armLength, -armLength, 0],  # back-right 
    [-armLength, -armLength, 0],  # back-left
    [-armLength,  armLength, 0]   # front-left
])

# Motor rotation directions (CW = +1, CCW = -1)
motor_dirs = np.array([1, -1, 1, -1])

# State variables
time_us = int(time.time() * 1_000_000)
pwm_values = [1000] * 4  # motor values from SITL
pwm_lock = threading.Lock()

# Physics state
pos = np.array([0.0, 0.0, 0.0])
vel = np.array([0.0, 0.0, 0.0])
quat = np.array([1.0, 0.0, 0.0, 0.0])  # w, x, y, z
omega = np.array([0.0, 0.0, 0.0])  # angular velocity

# Moments of inertia (approximate)
Ixx = Iyy = 2 * mass * armLength**2
Izz = 2 * mass * (2 * armLength**2)
inertia = np.array([Ixx, Iyy, Izz])

# UDP sockets
fdm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
rc_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
pwm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
try:
    pwm_sock.bind((SITL_IP, PWM_PORT))
except OSError as e:
    print(f'Port {PWM_PORT} already in use: {e}')
    print('Check for running SITL or other processes. Exiting.')
    raise
pwm_sock.setblocking(False)

# Background thread to receive PWM values
def pwm_listener():
    global pwm_values
    while True:
        try:
            data, _ = pwm_sock.recvfrom(1024)
            if len(data) == 16:  # 4 floats
                with pwm_lock:
                    pwm_values = list(struct.unpack('4f', data))
        except BlockingIOError:
            time.sleep(0.001)  # No data, yield
        except Exception as e:
            print(f'PWM receive error: {e}')
            time.sleep(0.001)

# Start listener thread
t = threading.Thread(target=pwm_listener, daemon=True)
t.start()

# Quaternion multiplication
def quat_mul(q1, q2):
    w1, x1, y1, z1 = q1
    w2, x2, y2, z2 = q2
    return np.array([
        w1*w2 - x1*x2 - y1*y2 - z1*z2,
        w1*x2 + x1*w2 + y1*z2 - z1*y2,
        w1*y2 - x1*z2 + y1*w2 + z1*x2,
        w1*z2 + x1*y2 - y1*x2 + z1*w2
    ])

# Quaternion to rotation matrix
def quat_to_rot(q):
    w, x, y, z = q
    return np.array([
        [1-2*y*y-2*z*z, 2*x*y-2*z*w, 2*x*z+2*y*w],
        [2*x*y+2*z*w, 1-2*x*x-2*z*z, 2*y*z-2*x*w],
        [2*x*z-2*y*w, 2*y*z+2*x*w, 1-2*x*x-2*y*y]
    ])

# Rotate vector by quaternion
def rotate_vec(q, v):
    q_conj = np.array([q[0], -q[1], -q[2], -q[3]])
    qv = np.array([0, v[0], v[1], v[2]])
    qv_rot = quat_mul(quat_mul(q, qv), q_conj)
    return qv_rot[1:]

# Angular acceleration from torque
# tau = I * alpha  => alpha = I^-1 * tau
# But in body frame: alpha = I^-1 * (tau - omega x (I * omega))
def angular_accel(omega, tau):
    Iomega = inertia * omega
    omega_cross = np.cross(omega, Iomega)
    return (tau - omega_cross) / inertia

# Run physics simulation
def simulate_physics(dt):
    global pos, vel, quat, omega

    with pwm_lock:
        motor_vals = pwm_values.copy()
    
    # Convert motor values [0..1] to thrust (quadratic)
    # 0 = 0N, 1 = maxThrustPerMotor
    thrusts = [maxThrustPerMotor * val*val for val in motor_vals]
    
    # Total thrust in body frame (up = +z)
    total_thrust_body = np.array([0, 0, sum(thrusts)])
    
    # Rotation matrix body->world
    R = quat_to_rot(quat)
    
    # Transform thrust to world frame
    thrust_world = R.T @ total_thrust_body
    
    # Gravity in world frame
    gravity_world = np.array([0, 0, -mass * gravity])
    
    # Total force
    force_world = thrust_world + gravity_world
    
    # Linear acceleration
    accel_world = force_world / mass
    
    # Update linear velocity and position
    vel += accel_world * dt
    pos += vel * dt
    
    # Clamp to ground
    if pos[2] <= 0:
        pos[2] = 0
        vel[2] = 0
        if vel[0] != 0 or vel[1] != 0:
            # Damp horizontal velocity on ground
            vel *= 0.95
    
    # Compute torques in body frame
    roll_torque = 0.0
    pitch_torque = 0.0
    yaw_torque = 0.0
    
    for i in range(4):
        thrust = thrusts[i]
        pos = motor_positions[i]
        dir_z = motor_dirs[i]
        
        # Torque from thrust: r x F
        # F is in +z direction
        torque = np.cross(pos, np.array([0, 0, thrust]))
        roll_torque += torque[0]
        pitch_torque += torque[1]
        
        # Yaw torque from motor drag (proportional to thrust * direction)
        # Approximate motor reaction torque
        yaw_torque += thrust * dir_z * 0.05  # scale factor
    
    torque_body = np.array([roll_torque, pitch_torque, yaw_torque])
    
    # Angular acceleration in body frame
    alpha = angular_accel(omega, torque_body)
    
    # Update angular velocity
    omega += alpha * dt
    
    # Gyro reading = angular velocity (rad/s)
    gyro = omega.copy()
    
    # Update orientation
    # Quaternion derivative: dq/dt = 0.5 * q * omega_quat
    # omega as quaternion [0, omega_x, omega_y, omega_z]
    omega_quat = np.array([0, omega[0], omega[1], omega[2]])
    quat_deriv = 0.5 * quat_mul(quat, omega_quat)
    quat += quat_deriv * dt
    
    # Normalize quaternion
    quat /= np.linalg.norm(quat)
    
    # Accel measurement: (thrust - gravity) / mass in body frame
    # But measured by IMU, so we output in body frame
    # thrust_body is upward (+z), gravity is downward (-z)
    accel_body = (total_thrust_body + np.array([0,0,-mass*gravity])) / mass
    
    # Add noise?
    # For now, ideal
    
    return accel_world, gyro, quat, vel, pos

# Send RC command
def send_rc(sock, timestamp, roll=1500, pitch=1500, throttle=1000, yaw=1500, aux1=900):
    rc_data = struct.pack('<d 16H', timestamp/1e6, 
                          int(roll), int(pitch), int(throttle), int(yaw),
                          int(aux1), *[1500]*11)
    sock.sendto(rc_data, (SITL_IP, RC_PORT))

# Send FDM packet
def send_fdm(sock, timestamp, gyro, accel, quat, vel, pos, pressure=101325.0):
    fdm_data = struct.pack('<d ddd ddd dddd ddd ddd d', 
                           timestamp/1e6,
                           gyro[0], gyro[1], gyro[2],
                           accel[0], accel[1], accel[2],
                           quat[0], quat[1], quat[2], quat[3],
                           vel[0], vel[1], vel[2],
                           pos[0], pos[1], pos[2],
                           pressure)
    sock.sendto(fdm_data, (SITL_IP, FDM_PORT))

# Main loop
def main():
    global time_us

    # Initial RC: disarm
    send_rc(rc_sock, time_us, aux1=900)
    time.sleep(0.1)
    
    last_send = time.time()
    
    # Stabilization delay
    print('Stabilizing for 5 seconds...')
    time.sleep(5.0)
    
    # Arm
    print('Arming...')
    send_rc(rc_sock, time_us, aux1=1800)  # ARM
    time.sleep(0.5)
    
    # Throttle ramp
    ramps = [
        (1000, 4.0),
        (1200, 4.0),
        (1400, 4.0),
        (1600, 4.0),
        (1800, 4.0),
        (2000, 4.0),
        (1500, 4.0),
        (1000, 4.0)
    ]
    
    print('Starting throttle ramp...')
    for throttle, duration in ramps:
        print(f'  Throttle: {throttle} (for {duration}s)')
        
        start_time = time.time()
        while time.time() - start_time < duration:
            now = time.time()
            dt = now - last_send
            if dt >= 0.002:  # 500 Hz
                last_send = now
                time_us = int(now * 1_000_000)
                
                # Simulate physics
                accel_world, gyro, quat, vel, pos = simulate_physics(dt)
                
                # Send RC (with current throttle)
                send_rc(rc_sock, time_us, throttle=throttle)
                
                # Send FDM
                send_fdm(fdm_sock, time_us, gyro, accel_world, quat, vel, pos)
                
        # Log position
        print(f'    Position: {pos}')
        
    print('Test complete.')
    
    # Disarm
    send_rc(rc_sock, int(time.time() * 1_000_000), aux1=900)
    
    fdm_sock.close()
    rc_sock.close()
    pwm_sock.close()

if __name__ == '__main__':
    main()