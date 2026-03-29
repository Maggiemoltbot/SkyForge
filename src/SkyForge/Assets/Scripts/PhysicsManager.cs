using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    [Header("Physics Settings")]
    [Range(100, 8000)]
    public int physicsRate = 1000; // Hz
    
    void Awake()
    {
        Time.fixedDeltaTime = 1f / physicsRate;
        Time.maximumDeltaTime = 0.05f; // Max 50ms catch-up
        Physics.simulationMode = SimulationMode.FixedUpdate;
        Debug.Log($"[SkyForge] Physics rate set to {physicsRate} Hz (dt={Time.fixedDeltaTime:F6}s)");
    }
}