using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Increases Input System event buffer to prevent
/// "Exceeded budget for maximum input events" errors
/// from high-frequency gamepads (DJI Controller, TX16S).
/// </summary>
public static class InputSystemFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IncreaseEventBuffer()
    {
        // Default is 5MB (5242880) — increase to 20MB for gamepad flood
        InputSystem.settings.maxEventBytesPerUpdate = 20 * 1024 * 1024;
        InputSystem.settings.maxQueuedEventsPerUpdate = 2000;
        Debug.Log("[SkyForge] Input System event buffer increased to 20MB / 2000 events");
    }
}
