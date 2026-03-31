using UnityEngine;

/// <summary>
/// ScriptableObject for configuring the Flight Dynamics Bridge
/// Contains network settings and update frequency
/// </summary>
[CreateAssetMenu(fileName = "BridgeConfig", menuName = "FlightBridge/BridgeConfig", order = 1)]
public class BridgeConfig : ScriptableObject
{
    [Header("Network Settings")]
    [Tooltip("IP address of the Betaflight SITL simulator")]
    public string bfSITLIPAddress = "127.0.0.1";
    
    [Tooltip("UDP port for sending FDM packets to Betaflight SITL")]
    public int fdmSendPort = 9003;
    
    [Tooltip("UDP port for receiving PWM packets from Betaflight SITL")]
    public int pwmReceivePort = 9002;
    
    [Header("Update Settings")]
    [Tooltip("Frequency for sending FDM packets (Hz)")]
    public int updateFrequency = 1000;
}