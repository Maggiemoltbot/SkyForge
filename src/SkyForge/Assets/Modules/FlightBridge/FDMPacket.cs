using System;
using System.Runtime.InteropServices;

/// <summary>
/// Flight Dynamics Model packet structure for communication with Betaflight SITL
/// Unity → BF SITL, UDP Port 9002
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FDMPacket
{
    public double timestamp;           // Sekunden seit Start
    
    // IMU Angular Velocity (rad/s, NED)
    public float imuAngularVelX;
    public float imuAngularVelY;
    public float imuAngularVelZ;
    
    // IMU Linear Acceleration (m/s², NED)
    public float imuLinearAccX;
    public float imuLinearAccY;
    public float imuLinearAccZ;
    
    // Attitude Quaternion (NED)
    public float quatW;
    public float quatX;
    public float quatY;
    public float quatZ;
    
    // Velocity (m/s, NED)
    public float velX;
    public float velY;
    public float velZ;
    
    // Position (m, NED)
    public float posX;
    public float posY;
    public float posZ;
    
    // Barometric Pressure (hPa)
    public float pressure;
}

/// <summary>
/// PWM packet structure for receiving motor commands from Betaflight SITL
/// BF SITL → Unity, UDP Port 9003
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PWMPacket
{
    // Motor values (0.0-1.0)
    public float motor1;
    public float motor2;
    public float motor3;
    public float motor4;
}