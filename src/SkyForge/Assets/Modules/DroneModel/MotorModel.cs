using UnityEngine;

public class MotorModel
{
    // Quadratisches Thrust-Modell: thrust = maxThrust * pwm²
    // PWM Input: 0.0-1.0 (von Betaflight SITL via Bridge)
    // Output: Force in Newton
    
    // 4 Motor-Positionen relativ zum Zentrum (X-Config):
    // FrontLeft:  (+armLength, 0, +armLength)
    // FrontRight: (+armLength, 0, -armLength)
    // BackLeft:   (-armLength, 0, +armLength)
    // BackRight:  (-armLength, 0, -armLength)
    
    // Drehmoment durch differentielle Thrust berechnen
    
    public static float CalculateThrust(float pwm, float maxThrustPerMotor)
    {
        // Quadratic thrust model
        return maxThrustPerMotor * pwm * pwm;
    }
    
    public static Vector3[] GetMotorPositions(float armLength)
    {
        return new Vector3[]
        {
            new Vector3(armLength, 0, armLength),    // FrontLeft
            new Vector3(armLength, 0, -armLength),   // FrontRight
            new Vector3(-armLength, 0, armLength),   // BackLeft
            new Vector3(-armLength, 0, -armLength)   // BackRight
        };
    }
    

    /// <summary>
    /// Remaps Betaflight SITL motor outputs to the Unity motor ordering (FL, FR, BL, BR).
    /// SITL sends motor_speed[3]=BF0, [0]=BF1, [1]=BF2, [2]=BF3.
    /// </summary>
    /// <param name="target">Destination array in Unity ordering: FL, FR, BL, BR.</param>
    /// <param name="motor1">SITL motor_speed[0] (BF1).</param>
    /// <param name="motor2">SITL motor_speed[1] (BF2).</param>
    /// <param name="motor3">SITL motor_speed[2] (BF3).</param>
    /// <param name="motor4">SITL motor_speed[3] (BF0).</param>
    public static void RemapSitlMotorOrder(float[] target, float motor1, float motor2, float motor3, float motor4)
    {
        if (target == null || target.Length < 4)
        {
            Debug.LogError("RemapSitlMotorOrder expects an array with four entries.");
            return;
        }

        target[0] = motor3; // FrontLeft  <- BF3
        target[1] = motor4; // FrontRight <- BF0
        target[2] = motor2; // BackLeft   <- BF2
        target[3] = motor1; // BackRight  <- BF1
    }

    public static Vector3 CalculateTorque(float[] motorThrusts, float armLength, bool[] motorDirections)
    {
        // Calculate torque based on differential thrust
        // Motor directions: true = CW, false = CCW
        Vector3 torque = Vector3.zero;
        
        Vector3[] positions = GetMotorPositions(armLength);
        
        for (int i = 0; i < 4; i++)
        {
            // Calculate torque contribution from this motor
            Vector3 force = motorThrusts[i] * Vector3.up;
            Vector3 momentArm = positions[i];
            
            // Cross product gives us the torque
            Vector3 motorTorque = Vector3.Cross(momentArm, force);
            
            // Add rotational torque based on motor direction
            float rotationalDirection = motorDirections[i] ? 1f : -1f;
            motorTorque += new Vector3(0, rotationalDirection * motorThrusts[i] * 0.01f, 0);
            
            torque += motorTorque;
        }
        
        return torque;
    }
}