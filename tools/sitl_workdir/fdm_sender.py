#!/usr/bin/env python3

import socket
import struct
import time
import math
import numpy as np
from scipy.spatial.transform import Rotation as R

class FDMSender:
    def __init__(self, host='127.0.0.1', fdm_port=9003, pwm_port=9002):
        self.host = host
        self.fdm_port = fdm_port
        self.pwm_port = pwm_port
        
        # Drone configuration
        self.mass = 0.5  # kg
        self.max_thrust_per_motor = 4.0  # N
        self.arm_length = 0.12  # m
        self.gravity = 9.81  # m/s^2
        
        # State variables
        self.position = np.array([0.0, 0.0, 0.0])  # x, y, z
        self.velocity = np.array([0.0, 0.0, 0.0])  # vx, vy, vz
        self.orientation = R.from_quat([0, 0, 0, 1])  # quaternion (x, y, z, w)
        self.angular_velocity = np.array([0.0, 0.0, 0.0])  # wx, wy, wz
        
        # Motor speeds (0.0 to 1.0)
        self.motor_speeds = np.array([0.0, 0.0, 0.0, 0.0])
        
        # Initialize sockets
        self.fdm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.pwm_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
        # PID controller gains for stabilization
        self.pid_roll = {'kp': 0.5, 'ki': 0.01, 'kd': 0.1}
        self.pid_pitch = {'kp': 0.5, 'ki': 0.01, 'kd': 0.1}
        self.pid_yaw = {'kp': 0.3, 'ki': 0.005, 'kd': 0.05}
        
        # Integral terms for PID controllers
        self.integral_roll = 0.0
        self.integral_pitch = 0.0
        self.integral_yaw = 0.0
        
        # Previous errors for derivative terms
        self.prev_error_roll = 0.0
        self.prev_error_pitch = 0.0
        self.prev_error_yaw = 0.0
        
    def send_fdm_packet(self):
        """Send FDM packet to SITL"""
        timestamp = time.time()
        
        # Calculate acceleration based on motor speeds and orientation
        thrust = np.sum(self.motor_speeds) * self.max_thrust_per_motor
        gravity_force = np.array([0.0, 0.0, -self.mass * self.gravity])
        
        # Rotate forces to world frame
        rotation_matrix = self.orientation.as_matrix()
        thrust_vector = rotation_matrix @ np.array([0.0, 0.0, thrust])
        total_force = thrust_vector + gravity_force
        
        # Calculate acceleration (F = ma)
        acceleration = total_force / self.mass
        
        # Update angular velocity based on motor speeds
        # Simple model: motors 0 and 2 cause roll, 1 and 3 cause pitch, opposite pairs cause yaw
        roll_torque = (self.motor_speeds[0] - self.motor_speeds[2]) * 0.1
        pitch_torque = (self.motor_speeds[1] - self.motor_speeds[3]) * 0.1
        yaw_torque = (self.motor_speeds[0] + self.motor_speeds[2] - self.motor_speeds[1] - self.motor_speeds[3]) * 0.05
        
        # Apply torques to angular velocity (simplified)
        self.angular_velocity[0] += roll_torque * 0.01
        self.angular_velocity[1] += pitch_torque * 0.01
        self.angular_velocity[2] += yaw_torque * 0.01
        
        # Update orientation using gyroscope data
        dt = 0.01
        angle_change = self.angular_velocity * dt
        rotation_delta = R.from_rotvec(angle_change)
        self.orientation = self.orientation * rotation_delta
        
        # Update position and velocity
        self.velocity += acceleration * dt
        self.position += self.velocity * dt
        
        # Get quaternion from orientation
        quat = self.orientation.as_quat()  # [x, y, z, w]
        
        # Create FDM packet
        packet = struct.pack('d' + 'd'*3 + 'd'*3 + 'd'*4 + 'd'*3 + 'd'*3 + 'd',
                           timestamp,
                           self.angular_velocity[0], self.angular_velocity[1], self.angular_velocity[2],
                           acceleration[0], acceleration[1], acceleration[2],
                           quat[0], quat[1], quat[2], quat[3],
                           self.velocity[0], self.velocity[1], self.velocity[2],
                           self.position[0], self.position[1], self.position[2],
                           101325.0)  # Pressure (Pa)
        
        # Send packet
        self.fdm_sock.sendto(packet, (self.host, self.fdm_port))
        
    def send_rc_packet(self, roll=1500, pitch=1500, throttle=1000, yaw=1500, aux1=1000):
        """Send RC packet to SITL"""
        timestamp = time.time()
        
        # Create RC packet (16 channels, uint16 each)
        channels = [1500] * 16  # Initialize with neutral values
        channels[0] = roll      # Channel 1: Roll
        channels[1] = pitch     # Channel 2: Pitch
        channels[2] = throttle  # Channel 3: Throttle
        channels[3] = yaw       # Channel 4: Yaw
        channels[4] = aux1      # Channel 5: AUX1
        
        # Pack channels as unsigned shorts
        channel_data = struct.pack('H' * 16, *channels)
        
        # Create full packet
        packet = struct.pack('d', timestamp) + channel_data
        
        # Send packet
        self.fdm_sock.sendto(packet, (self.host, 9004))  # RC port is 9004
        
    def set_motor_speeds(self, speeds):
        """Set motor speeds (0.0 to 1.0) and send to SITL"""
        self.motor_speeds = np.array(speeds)
        
        # Convert to servo packet format (4 floats, 0.0-1.0)
        packet = struct.pack('f' * 4, *speeds)
        
        # Send packet to PWM port
        self.pwm_sock.sendto(packet, (self.host, self.pwm_port))
        
    def stabilize_drone(self, target_roll=0, target_pitch=0, target_yaw=0):
        """Simple stabilization using PID controllers"""
        dt = 0.01
        
        # Get current Euler angles
        euler = self.orientation.as_euler('xyz')
        current_roll, current_pitch, current_yaw = euler
        
        # Calculate errors
        error_roll = target_roll - current_roll
        error_pitch = target_pitch - current_pitch
        error_yaw = target_yaw - current_yaw
        
        # Update integral terms
        self.integral_roll += error_roll * dt
        self.integral_pitch += error_pitch * dt
        self.integral_yaw += error_yaw * dt
        
        # Calculate derivative terms
        derivative_roll = (error_roll - self.prev_error_roll) / dt
        derivative_pitch = (error_pitch - self.prev_error_pitch) / dt
        derivative_yaw = (error_yaw - self.prev_error_yaw) / dt
        
        # Calculate PID outputs
        roll_output = (self.pid_roll['kp'] * error_roll +
                       self.pid_roll['ki'] * self.integral_roll +
                       self.pid_roll['kd'] * derivative_roll)
        
        pitch_output = (self.pid_pitch['kp'] * error_pitch +
                        self.pid_pitch['ki'] * self.integral_pitch +
                        self.pid_pitch['kd'] * derivative_pitch)
        
        yaw_output = (self.pid_yaw['kp'] * error_yaw +
                      self.pid_yaw['ki'] * self.integral_yaw +
                      self.pid_yaw['kd'] * derivative_yaw)
        
        # Update previous errors
        self.prev_error_roll = error_roll
        self.prev_error_pitch = error_pitch
        self.prev_error_yaw = error_yaw
        
        # Apply outputs to motor speeds (simplified mixing)
        # This is a very basic implementation - in practice, you'd want a proper mixer
        base_thrust = 0.5  # Base thrust level
        self.motor_speeds[0] = base_thrust + roll_output - pitch_output - yaw_output  # Front right
        self.motor_speeds[1] = base_thrust - roll_output - pitch_output + yaw_output  # Back right
        self.motor_speeds[2] = base_thrust - roll_output + pitch_output - yaw_output  # Front left
        self.motor_speeds[3] = base_thrust + roll_output + pitch_output + yaw_output  # Back left
        
        # Clamp motor speeds to valid range
        self.motor_speeds = np.clip(self.motor_speeds, 0.0, 1.0)
        
        # Send updated motor speeds
        self.set_motor_speeds(self.motor_speeds.tolist())
        
    def takeoff_test(self):
        """Perform a simple takeoff test"""
        print("Starting takeoff test...")
        
        # Arm the drone (set throttle to minimum for arming)
        for i in range(100):
            self.send_rc_packet(throttle=1000)
            time.sleep(0.01)
            
        # Slowly increase throttle to 1500
        for i in range(500):
            throttle = 1000 + i
            self.send_rc_packet(throttle=throttle)
            self.send_fdm_packet()
            time.sleep(0.01)
            
        # Hold at 1500 for a bit
        for i in range(200):
            self.send_rc_packet(throttle=1500)
            self.send_fdm_packet()
            time.sleep(0.01)
            
        # Try to lift off further (throttle to 1800)
        for i in range(300):
            throttle = 1500 + i
            self.send_rc_packet(throttle=throttle)
            self.send_fdm_packet()
            time.sleep(0.01)
            
        # Hold at 1800
        for i in range(500):
            self.send_rc_packet(throttle=1800)
            self.send_fdm_packet()
            time.sleep(0.01)
            
        print("Takeoff test completed.")

if __name__ == "__main__":
    sender = FDMSender()
    
    # Test sending some packets
    for i in range(1000):
        sender.send_fdm_packet()
        sender.send_rc_packet(throttle=1000)
        time.sleep(0.01)
        
    print("Test completed.")