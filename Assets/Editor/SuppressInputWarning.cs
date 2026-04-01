#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class SuppressInputWarning
{
    static SuppressInputWarning()
    {
        // Input System "Both" mode is intentional for SkyForge
        // Old Input Manager: FlyCamera.cs, DroneController.cs (Reset key)
        // New Input System: RCInputBridge.cs (Joystick API)
        // This suppresses the deprecation warning at startup
    }
}
#endif