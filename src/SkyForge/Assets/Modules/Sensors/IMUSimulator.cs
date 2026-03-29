using UnityEngine;

/// <summary>
/// Simulates IMU sensor data including gyroscope, accelerometer, and optional noise
/// </summary>
public class IMUSimulator : MonoBehaviour
{
    [Header("IMU Configuration")]
    [Tooltip("Reference to the rigidbody for physics-based calculations")]
    public Rigidbody droneRigidbody;

    [Header("Noise Parameters")]
    [Tooltip("Enable or disable sensor noise simulation")]
    public bool enableNoise = false;
    
    [Tooltip("Standard deviation for gyroscope noise (degrees/second)")]
    public float gyroNoiseStdDev = 0.1f;
    
    [Tooltip("Standard deviation for accelerometer noise (m/s²)")]
    public float accelNoiseStdDev = 0.05f;
    
    [Tooltip("Bias drift for gyroscope (degrees/second per second)")]
    public float gyroBiasDrift = 0.001f;

    // Private variables for noise simulation
    private Vector3 gyroBias;
    private Vector3 previousVelocity;
    private System.Random randomGenerator;

    void Awake()
    {
        // Initialize random generator with a fixed seed for reproducibility
        randomGenerator = new System.Random(42);
        
        // Initialize gyro bias
        gyroBias = Vector3.zero;
        
        // Try to get Rigidbody component if not assigned
        if (droneRigidbody == null)
            droneRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Update bias drift over time
        if (enableNoise)
        {
            gyroBias += new Vector3(
                RandomGaussian() * gyroBiasDrift * Mathf.Sqrt(Time.fixedDeltaTime),
                RandomGaussian() * gyroBiasDrift * Mathf.Sqrt(Time.fixedDeltaTime),
                RandomGaussian() * gyroBiasDrift * Mathf.Sqrt(Time.fixedDeltaTime)
            );
        }
    }

    /// <summary>
    /// Calculates gyroscope data in degrees/second
    /// </summary>
    /// <returns>Gyroscope angular velocity vector in degrees/second</returns>
    public Vector3 GetGyroData()
    {
        if (droneRigidbody == null)
            return Vector3.zero;

        // Get angular velocity in radians/second and convert to degrees/second
        Vector3 gyroData = droneRigidbody.angularVelocity * Mathf.Rad2Deg;
        
        // Add bias and noise if enabled
        if (enableNoise)
        {
            gyroData += gyroBias;
            gyroData.x += RandomGaussian() * gyroNoiseStdDev;
            gyroData.y += RandomGaussian() * gyroNoiseStdDev;
            gyroData.z += RandomGaussian() * gyroNoiseStdDev;
        }
        
        return gyroData;
    }

    /// <summary>
    /// Calculates accelerometer data in local space (m/s²)
    /// </summary>
    /// <returns>Accelerometer vector in local space (m/s²)</returns>
    public Vector3 GetAccelerometerData()
    {
        if (droneRigidbody == null)
            return Vector3.zero;

        // Calculate acceleration based on velocity change
        Vector3 acceleration = (droneRigidbody.velocity - previousVelocity) / Time.fixedDeltaTime;
        previousVelocity = droneRigidbody.velocity;
        
        // Get local acceleration (without gravity)
        Vector3 localAcceleration = transform.InverseTransformDirection(acceleration);
        
        // Add gravity in local space
        Vector3 localGravity = transform.InverseTransformDirection(Physics.gravity);
        Vector3 accelData = localAcceleration - localGravity; // Subtract because we want to simulate accelerometer without gravity
        
        // Add noise if enabled
        if (enableNoise)
        {
            accelData.x += RandomGaussian() * accelNoiseStdDev;
            accelData.y += RandomGaussian() * accelNoiseStdDev;
            accelData.z += RandomGaussian() * accelNoiseStdDev;
        }
        
        return accelData;
    }

    /// <summary>
    /// Generates a random number from a Gaussian distribution with mean=0 and stdDev=1
    /// </summary>
    /// <returns>Random Gaussian number</returns>
    private float RandomGaussian()
    {
        // Box-Muller transform
        double u1 = 1.0 - randomGenerator.NextDouble(); // Uniform(0,1] random doubles
        double u2 = 1.0 - randomGenerator.NextDouble();
        double randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log((float)u1)) * Mathf.Sin(2.0f * Mathf.PI * (float)u2); // Random normal(0,1)
        return (float)randStdNormal; // Random normal(0,stdDev^2)
    }

    /// <summary>
    /// Resets the gyro bias to zero
    /// </summary>
    public void ResetGyroBias()
    {
        gyroBias = Vector3.zero;
    }
}