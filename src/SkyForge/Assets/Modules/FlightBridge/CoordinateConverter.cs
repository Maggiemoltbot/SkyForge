using UnityEngine;

/// <summary>
/// Utility class for converting between Unity and Betaflight NED coordinate systems
/// Unity (Y-up, left-handed) → BF NED (Z-down, right-handed)
/// </summary>
public static class CoordinateConverter
{
    /// <summary>
    /// Converts a position vector from Unity coordinates to NED coordinates
    /// X_ned =  Z_unity
    /// Y_ned =  X_unity  
    /// Z_ned = -Y_unity
    /// </summary>
    /// <param name="unityPosition">Position in Unity coordinates</param>
    /// <returns>Position in NED coordinates</returns>
    public static Vector3 UnityToNED(Vector3 unityPosition)
    {
        return new Vector3(
            unityPosition.z,    // X_ned = Z_unity
            unityPosition.x,    // Y_ned = X_unity
            -unityPosition.y    // Z_ned = -Y_unity
        );
    }
    
    /// <summary>
    /// Converts a position vector from NED coordinates to Unity coordinates
    /// X_unity =  Y_ned
    /// Y_unity = -Z_ned
    /// Z_unity =  X_ned
    /// </summary>
    /// <param name="nedPosition">Position in NED coordinates</param>
    /// <returns>Position in Unity coordinates</returns>
    public static Vector3 NEDToUnity(Vector3 nedPosition)
    {
        return new Vector3(
            nedPosition.y,      // X_unity = Y_ned
            -nedPosition.z,     // Y_unity = -Z_ned
            nedPosition.x       // Z_unity = X_ned
        );
    }
    
    /// <summary>
    /// Converts an angular velocity vector from Unity coordinates to NED coordinates
    /// Uses the same transformation as position
    /// </summary>
    /// <param name="unityAngularVelocity">Angular velocity in Unity coordinates</param>
    /// <returns>Angular velocity in NED coordinates</returns>
    public static Vector3 AngularVelocityUnityToNED(Vector3 unityAngularVelocity)
    {
        return UnityToNED(unityAngularVelocity);
    }
    
    /// <summary>
    /// Converts a linear acceleration vector from Unity coordinates to NED coordinates
    /// Uses the same transformation as position
    /// </summary>
    /// <param name="unityLinearAcceleration">Linear acceleration in Unity coordinates</param>
    /// <returns>Linear acceleration in NED coordinates</returns>
    public static Vector3 LinearAccelerationUnityToNED(Vector3 unityLinearAcceleration)
    {
        return UnityToNED(unityLinearAcceleration);
    }
    
    /// <summary>
    /// Converts a quaternion from Unity coordinates to NED coordinates
    /// Accounts for the change in handedness and axis orientation
    /// </summary>
    /// <param name="unityQuaternion">Quaternion in Unity coordinates</param>
    /// <returns>Quaternion in NED coordinates</returns>
    public static Quaternion QuaternionUnityToNED(Quaternion unityQuaternion)
    {
        // Convert Unity quaternion to NED by reordering components
        // Unity: RHS with Y-up
        // NED: RHS with Z-down
        return new Quaternion(
            unityQuaternion.z,    // X_NED
            unityQuaternion.x,    // Y_NED
            -unityQuaternion.y,   // Z_NED
            unityQuaternion.w     // W (scalar part remains the same)
        );
    }
    
    /// <summary>
    /// Converts a quaternion from NED coordinates to Unity coordinates
    /// </summary>
    /// <param name="nedQuaternion">Quaternion in NED coordinates</param>
    /// <returns>Quaternion in Unity coordinates</returns>
    public static Quaternion QuaternionNEDToUnity(Quaternion nedQuaternion)
    {
        // Reverse the transformation
        return new Quaternion(
            nedQuaternion.y,      // X_Unity
            -nedQuaternion.z,     // Y_Unity
            nedQuaternion.x,      // Z_Unity
            nedQuaternion.w       // W (scalar part remains the same)
        );
    }
}