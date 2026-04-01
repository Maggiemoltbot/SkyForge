# Flight Dynamics Bridge Module

This module provides a bridge between Unity and the Betaflight SITL simulator for flight dynamics modeling. It sends aircraft state data from Unity to the simulator via UDP and receives motor control signals back from the simulator.

## Components

1. **FDMPacket.cs** - Defines the data structure for sending flight dynamics data (position, velocity, attitude, etc.) from Unity to Betaflight SITL.

2. **PWMPacket.cs** - Defines the data structure for receiving motor control signals from Betaflight SITL.

3. **CoordinateConverter.cs** - Handles coordinate system transformation between Unity's Y-up left-handed system and Betaflight's NED Z-down right-handed system.

4. **BridgeConfig.cs** - ScriptableObject for configuring network settings and update frequency.

5. **FlightDynamicsBridge.cs** - Main MonoBehaviour component that handles UDP communication, data serialization, and integration with the drone controller.

## Features

- Sends FDM packets at 400Hz in FixedUpdate
- Receives PWM packets in a separate thread with thread-safe queuing
- Automatic connection detection with 2-second timeout
- Editor inspector for monitoring connection status and packet counters
- Full coordinate system conversion including quaternion transformations
- Defensive error handling for all socket operations

## Network Protocol

- FDM packets sent to: UDP port 9002
- PWM packets received from: UDP port 9003
- Default IP: 127.0.0.1 (localhost)