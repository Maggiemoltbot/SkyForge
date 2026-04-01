# ControllerInput Module

This module handles input from game controllers and translates it into RC channels for the Betaflight SITL.

## Features
- Supports physical gamepads and joysticks
- Keyboard input emulation for debugging
- Dynamic channel mapping
- Deadzone and debouncing

## Configuration
Input axis mapping is defined in `ProjectSettings/InputManager.asset`. See `InputManager` documentation.

## Usage
Ensure the input manager has the required axes defined (Throttle, Yaw, Pitch, Roll).

## Development
- Next step: Migrate to Unity Input System (USS)
- Add support for multiple controller profiles
- Implement auto-detection routine