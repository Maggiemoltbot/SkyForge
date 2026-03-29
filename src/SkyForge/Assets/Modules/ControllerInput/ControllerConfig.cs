using UnityEngine;

/// <summary>
/// ScriptableObject for configuring game controller to RC channel mapping
/// Supports per-axis invert, deadzone, and expo settings
/// </summary>
[CreateAssetMenu(fileName = "ControllerConfig", menuName = "SkyForge/ControllerConfig", order = 1)]
public class ControllerConfig : ScriptableObject
{
    [Header("Network")]
    [Tooltip("IP address of the Betaflight SITL simulator")]
    public string bfSITLIPAddress = "127.0.0.1";

    [Tooltip("UDP port for RC input to Betaflight SITL")]
    public int rcPort = 9004;

    [Tooltip("Send rate in Hz (how often RC packets are sent)")]
    [Range(10, 500)]
    public int sendRateHz = 100;

    [Header("Controller Info")]
    [Tooltip("Detected controller name (auto-filled at runtime)")]
    public string controllerName = "None";

    [Header("Stick Mapping — RC Channel per Axis")]
    [Tooltip("Which RC channel (0-15) for each stick axis")]
    public AxisMapping roll = new AxisMapping
    {
        rcChannel = 0,
        invert = false,
        deadzone = 0.05f,
        expo = 0.0f
    };

    public AxisMapping pitch = new AxisMapping
    {
        rcChannel = 1,
        invert = false,
        deadzone = 0.05f,
        expo = 0.0f
    };

    public AxisMapping throttle = new AxisMapping
    {
        rcChannel = 2,
        invert = false,
        deadzone = 0.02f,
        expo = 0.0f
    };

    public AxisMapping yaw = new AxisMapping
    {
        rcChannel = 3,
        invert = false,
        deadzone = 0.05f,
        expo = 0.0f
    };

    [Header("AUX Channel Buttons")]
    [Tooltip("Map gamepad buttons to AUX channels (toggle between 1000/2000)")]
    public AuxButtonMapping[] auxMappings = new AuxButtonMapping[]
    {
        new AuxButtonMapping { rcChannel = 4, buttonName = "Button South (A/Cross)" },
        new AuxButtonMapping { rcChannel = 5, buttonName = "Button East (B/Circle)" },
        new AuxButtonMapping { rcChannel = 6, buttonName = "Button West (X/Square)" },
        new AuxButtonMapping { rcChannel = 7, buttonName = "Button North (Y/Triangle)" },
    };

    [Header("AUX Channel Axes")]
    [Tooltip("Map gamepad axes to AUX channels (continuous values)")]
    public AuxAxisMapping[] auxAxisMappings = new AuxAxisMapping[]
    {
        new AuxAxisMapping { rcChannel = 4, axisName = "" }, // AUX1 (M/S/N Switch)
        new AuxAxisMapping { rcChannel = 5, axisName = "" }, // AUX2 (Rechter Kippschalter)
    };
}

/// <summary>
/// Mapping configuration for a single stick axis
/// </summary>
[System.Serializable]
public class AxisMapping
{
    [Tooltip("Target RC channel index (0 = Roll, 1 = Pitch, 2 = Throttle, 3 = Yaw, 4+ = AUX)")]
    [Range(0, 15)]
    public int rcChannel;

    [Tooltip("Invert this axis")]
    public bool invert;

    [Tooltip("Deadzone — stick values below this are treated as zero")]
    [Range(0f, 0.5f)]
    public float deadzone = 0.05f;

    [Tooltip("Expo curve — 0 = linear, 1 = maximum expo (softer center, sharper edges)")]
    [Range(0f, 1f)]
    public float expo = 0.0f;
}

/// <summary>
/// Mapping configuration for an AUX button
/// </summary>
[System.Serializable]
public class AuxButtonMapping
{
    [Tooltip("Target RC channel index")]
    [Range(4, 15)]
    public int rcChannel = 4;

    [Tooltip("Description of the mapped button")]
    public string buttonName = "";
}

/// <summary>
/// Mapping configuration for an AUX axis
/// </summary>
[System.Serializable]
public class AuxAxisMapping
{
    [Tooltip("Target RC channel index")]
    [Range(4, 15)]
    public int rcChannel = 4;

    [Tooltip("Name of the axis control (e.g., 'stick/z' or gamepad axis)")]
    public string axisName = "";

    [Tooltip("Invert this axis")]
    public bool invert = false;

    [Tooltip("Threshold for digital mode - value above which channel is considered active")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("Axis mode - determines how analog values are mapped to PWM")]
    public AuxAxisMode mode = AuxAxisMode.ThreePosition;
}

public enum AuxAxisMode 
{ 
    TwoPosition, 
    ThreePosition 
}
