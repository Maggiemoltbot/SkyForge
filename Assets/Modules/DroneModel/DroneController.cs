using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    // Referenzen
    [SerializeField] private DroneConfig config;
    
    // Motor PWM Werte (gesetzt von FlightDynamicsBridge)
    public float[] motorPWM = new float[4]; // 0.0-1.0
    
    private Rigidbody rb;
    private Vector3[] motorPositions;
    private bool[] motorDirections = { true, false, false, true }; // FL=CW, FR=CCW, BL=CCW, BR=CW
    private Vector3 lastVelocity;
    private Vector3 cachedLinearAcceleration;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Rigidbody Setup
        if (config != null)
        {
            rb.mass = config.mass;
            rb.linearDamping = config.dragCoefficient;
            rb.angularDamping = config.angularDragCoefficient;
        }
        
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Initialize motor positions
        if (config != null)
        {
            motorPositions = MotorModel.GetMotorPositions(config.armLength);
        }
        else
        {
            motorPositions = MotorModel.GetMotorPositions(0.12f); // Default arm length
        }
    }
    
    void FixedUpdate()
    {
        if (config == null) return;
        
        // Calculate forces and torques
        float[] motorThrusts = new float[4];
        Vector3 totalForce = Vector3.zero;
        Vector3 totalTorque = Vector3.zero;
        
        // 1. Für jeden Motor: thrust = config.maxThrustPerMotor * pwm * pwm
        for (int i = 0; i < 4; i++)
        {
            motorThrusts[i] = MotorModel.CalculateThrust(motorPWM[i], config.maxThrustPerMotor);
            
            // 2. Force-Richtung = transform.up (lokale Y-Achse = Thrust-Richtung)
            Vector3 thrustForce = motorThrusts[i] * transform.up;
            totalForce += thrustForce;
            
            // 3. AddForceAtPosition(thrust * transform.up, motorPosition, ForceMode.Force)
            Vector3 worldMotorPosition = transform.TransformPoint(motorPositions[i]);
            rb.AddForceAtPosition(thrustForce, worldMotorPosition, ForceMode.Force);
        }
        
        // 4. Torque durch Drehrichtungs-Differenz (CW vs CCW)
        Vector3 torque = MotorModel.CalculateTorque(motorThrusts, config.armLength, motorDirections);
        rb.AddTorque(torque, ForceMode.Force);
        
        // Calculate linear acceleration for IMU
        cachedLinearAcceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = rb.linearVelocity;
    }
    
    // Reset-Funktion (R-Taste)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDrone();
        }
    }
    
    public void ResetDrone()
    {
        // Position auf Startposition
        transform.position = Vector3.zero;
        
        // Rotation auf Quaternion.identity
        transform.rotation = Quaternion.identity;
        
        // Velocity/AngularVelocity auf zero
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset motor PWM values
        for (int i = 0; i < motorPWM.Length; i++)
        {
            motorPWM[i] = 0f;
        }
    }
    
    // Public Getter für Bridge
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public Quaternion GetRotation()
    {
        return transform.rotation;
    }
    
    public Vector3 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector3.zero;
    }
    
    public Vector3 GetAngularVelocity()
    {
        return rb != null ? rb.angularVelocity : Vector3.zero;
    }
    
    public Vector3 GetIMUData()
    {
        return cachedLinearAcceleration;
    }

    // Bridge-Access Properties (Prefix 'Current' um Member-Hiding zu vermeiden)
    public Vector3 CurrentPosition => transform.position;
    public Quaternion CurrentAttitude => transform.rotation;
    public Vector3 CurrentVelocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public Vector3 CurrentAngularVelocity => rb != null ? rb.angularVelocity : Vector3.zero;
    public Vector3 CurrentLinearAcceleration => cachedLinearAcceleration;
    public float CurrentPressure => 1013.25f; // Standard sea level hPa (placeholder)
}