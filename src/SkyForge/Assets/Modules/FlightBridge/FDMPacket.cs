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
    public double imuAngularVelX;
    public double imuAngularVelY;
    public double imuAngularVelZ;
    
    // IMU Linear Acceleration (m/s², NED)
    public double imuLinearAccX;
    public double imuLinearAccY;
    public double imuLinearAccZ;
    
    // Attitude Quaternion (NED)
    public double quatW;
    public double quatX;
    public double quatY;
    public double quatZ;
    
    // Velocity (m/s, NED)
    public double velX;
    public double velY;
    public double velZ;
    
    // Position (m, NED)
    public double posX;
    public double posY;
    public double posZ;
    
    // Barometric Pressure (hPa)
    public double pressure;
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