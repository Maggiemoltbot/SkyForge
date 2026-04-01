using UnityEngine;

/// <summary>
/// Rotiert ein Propeller-Disc-Objekt proportional zum Motor-PWM.
/// Wird als Child jedes Motors erstellt.
/// </summary>
public class PropellerRotation : MonoBehaviour
{
    [Header("Config")]
    public int motorIndex = 0;
    public bool clockwise = true; // Motor-Drehrichtung
    public float maxRPM = 3000f; // Visuelle Max-Drehzahl
    
    private DroneController droneController;
    
    void Start()
    {
        droneController = GetComponentInParent<DroneController>();
    }
    
    void Update()
    {
        if (droneController == null) return;
        
        float pwm = droneController.motorPWM[motorIndex];
        float rpm = pwm * maxRPM;
        float degreesPerSecond = rpm * 6f; // 360° / 60s = 6°/RPM
        
        float direction = clockwise ? 1f : -1f;
        transform.Rotate(Vector3.up, direction * degreesPerSecond * Time.deltaTime);
    }
}