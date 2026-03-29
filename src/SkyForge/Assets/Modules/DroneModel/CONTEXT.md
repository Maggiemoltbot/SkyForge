# Drone Model Module

This module implements a realistic quadcopter drone physics model for the SkyForge simulation framework. It provides:

## Components

1. **DroneConfig** - ScriptableObject for drone physical parameters
2. **MotorModel** - PWM to thrust conversion and torque calculation
3. **DroneController** - Main MonoBehaviour handling physics simulation
4. **DroneSetup** - Editor helper for automatic prefab setup
5. **FPVCamera** - First-person camera view controller
6. **PlaceholderMesh** - Runtime mesh generator for visualization

## Physics Model

The drone uses a rigidbody-based physics simulation with:
- Accurate mass, drag, and angular drag properties
- Realistic thrust modeling (thrust = maxThrust * PWM²)
- Torque calculation based on differential thrust
- Proper collision detection and interpolation

## Configuration

The drone configuration follows standard 5" quad specifications with X-configuration motor layout:
- FL (Front Left): CW rotation
- FR (Front Right): CCW rotation
- BL (Back Left): CCW rotation
- BR (Back Right): CW rotation

This follows the Betaflight standard for consistency with real-world flight controllers.