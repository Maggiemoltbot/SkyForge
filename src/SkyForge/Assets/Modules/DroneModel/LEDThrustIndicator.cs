using UnityEngine;

/// <summary>
/// Ändert die Farbe der Motor-Sphere basierend auf dem PWM-Wert.
/// Grün (idle) → Gelb (mittel) → Rot (Vollgas)
/// Wird auf jedes Motor-Sphere-GameObject gelegt.
/// </summary>
public class LEDThrustIndicator : MonoBehaviour
{
    [Header("Config")]
    public int motorIndex = 0; // 0=FL, 1=FR, 2=BL, 3=BR
    
    private DroneController droneController;
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    
    void Start()
    {
        droneController = GetComponentInParent<DroneController>();
        meshRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }
    
    void Update()
    {
        if (droneController == null || meshRenderer == null) return;
        
        float pwm = droneController.motorPWM[motorIndex];
        
        // Gradient: Green (0) → Yellow (0.5) → Red (1.0)
        Color color;
        if (pwm < 0.5f)
            color = Color.Lerp(Color.green, Color.yellow, pwm * 2f);
        else
            color = Color.Lerp(Color.yellow, Color.red, (pwm - 0.5f) * 2f);
        
        // Use MaterialPropertyBlock to avoid creating new materials
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", color); // URP uses _BaseColor
        meshRenderer.SetPropertyBlock(propBlock);
    }
}