using UnityEngine;

[CreateAssetMenu(fileName = "DroneConfig", menuName = "SkyForge/DroneConfig")]
public class DroneConfig : ScriptableObject
{
    [Tooltip("Mass of the drone in kilograms")]
    public float mass = 0.5f;           // kg (typischer 5" Quad)
    
    [Tooltip("Maximum thrust per motor in Newton")]
    public float maxThrustPerMotor = 4f; // Newton
    
    [Tooltip("Distance from center to motor in meters")]
    public float armLength = 0.12f;      // Meter (Motor-Abstand von Zentrum)
    
    [Tooltip("Linear drag coefficient")]
    public float dragCoefficient = 0.1f;
    
    [Tooltip("Angular drag coefficient")]
    public float angularDragCoefficient = 0.5f;
    
    // Motor-Layout: X-Config
    // FL=CW, FR=CCW, BL=CCW, BR=CW (Betaflight Standard)
}