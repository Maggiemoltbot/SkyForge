using System.Runtime.InteropServices;

/// <summary>
/// RC packet structure for sending controller input to Betaflight SITL
/// Unity → BF SITL, UDP Port 9004
///
/// Format: 8 bytes timestamp (double) + 16 channels × 2 bytes (uint16) = 40 bytes total
/// Channel values are PWM microseconds: 1000 (min) – 1500 (center) – 2000 (max)
/// Byte order: native (little-endian on x86/ARM)
/// No header or magic bytes — raw binary struct
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RCPacket
{
    public double timestamp;        // Sekunden seit Start

    // Standard AETR channel order (Betaflight default)
    public ushort channel0;         // Aileron  (Roll)
    public ushort channel1;         // Elevator (Pitch)
    public ushort channel2;         // Throttle
    public ushort channel3;         // Rudder   (Yaw)
    public ushort channel4;         // AUX1
    public ushort channel5;         // AUX2
    public ushort channel6;         // AUX3
    public ushort channel7;         // AUX4
    public ushort channel8;         // AUX5
    public ushort channel9;         // AUX6
    public ushort channel10;        // AUX7
    public ushort channel11;        // AUX8
    public ushort channel12;        // AUX9
    public ushort channel13;        // AUX10
    public ushort channel14;        // AUX11
    public ushort channel15;        // AUX12

    /// <summary>
    /// Sets a channel value by index (0-15)
    /// </summary>
    public void SetChannel(int index, ushort value)
    {
        switch (index)
        {
            case 0:  channel0  = value; break;
            case 1:  channel1  = value; break;
            case 2:  channel2  = value; break;
            case 3:  channel3  = value; break;
            case 4:  channel4  = value; break;
            case 5:  channel5  = value; break;
            case 6:  channel6  = value; break;
            case 7:  channel7  = value; break;
            case 8:  channel8  = value; break;
            case 9:  channel9  = value; break;
            case 10: channel10 = value; break;
            case 11: channel11 = value; break;
            case 12: channel12 = value; break;
            case 13: channel13 = value; break;
            case 14: channel14 = value; break;
            case 15: channel15 = value; break;
        }
    }

    /// <summary>
    /// Gets a channel value by index (0-15)
    /// </summary>
    public ushort GetChannel(int index)
    {
        switch (index)
        {
            case 0:  return channel0;
            case 1:  return channel1;
            case 2:  return channel2;
            case 3:  return channel3;
            case 4:  return channel4;
            case 5:  return channel5;
            case 6:  return channel6;
            case 7:  return channel7;
            case 8:  return channel8;
            case 9:  return channel9;
            case 10: return channel10;
            case 11: return channel11;
            case 12: return channel12;
            case 13: return channel13;
            case 14: return channel14;
            case 15: return channel15;
            default: return 1500;
        }
    }

    /// <summary>
    /// Creates a default packet with all channels centered (1500) and throttle low (1000)
    /// </summary>
    public static RCPacket CreateDefault(double time)
    {
        var packet = new RCPacket
        {
            timestamp = time,
            channel0 = 1500,    // Roll center
            channel1 = 1500,    // Pitch center
            channel2 = 1000,    // Throttle low
            channel3 = 1500,    // Yaw center
            channel4 = 1000,    // AUX1 low
            channel5 = 1000,    // AUX2 low
            channel6 = 1000,    // AUX3 low
            channel7 = 1000,    // AUX4 low
            channel8 = 1000,
            channel9 = 1000,
            channel10 = 1000,
            channel11 = 1000,
            channel12 = 1000,
            channel13 = 1000,
            channel14 = 1000,
            channel15 = 1000
        };
        return packet;
    }
}
