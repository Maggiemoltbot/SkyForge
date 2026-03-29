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