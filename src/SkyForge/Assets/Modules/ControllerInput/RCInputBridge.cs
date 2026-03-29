using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Reads USB game controller input via the new Unity Input System and
/// sends RC channel data to Betaflight SITL over UDP port 9004.
///
/// Attach to any GameObject, assign a ControllerConfig asset, and press Play.
/// The script auto-detects the first connected Gamepad.
/// </summary>
public class RCInputBridge : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ControllerConfig config;

    [Header("Status (read-only)")]
    [SerializeField] private bool isConnected;
    [SerializeField] private int packetsSent;
    [SerializeField] private string activeController = "None";

    [Header("Debug")]
    [SerializeField] private bool showChannelValues;
    [SerializeField] private ushort[] channelPreview = new ushort[16];

    // UDP
    private UdpClient udpClient;

    // Timing
    private float sendInterval;
    private float timeSinceLastSend;

    // AUX toggle state
    private bool[] auxToggleState;
    private bool[] auxButtonPrevState;

    // Channel values (PWM 1000-2000)
    private ushort[] channels = new ushort[16];

    void OnEnable()
    {
        if (config == null)
        {
            Debug.LogWarning("[RCInputBridge] ControllerConfig is not assigned yet. Waiting...");
            return;
        }

        InitializeUDP();
    }

    void InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.Connect(config.bfSITLIPAddress, config.rcPort);
            isConnected = true;
            Debug.Log($"[RCInputBridge] UDP connected to {config.bfSITLIPAddress}:{config.rcPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RCInputBridge] Failed to connect UDP: {e.Message}");
            isConnected = false;
            return;
        }

        sendInterval = 1f / config.sendRateHz;
        timeSinceLastSend = 0f;

        // Init AUX toggle states
        int auxCount = config.auxMappings != null ? config.auxMappings.Length : 0;
        auxToggleState = new bool[auxCount];
        auxButtonPrevState = new bool[auxCount];

        // Init channels to safe defaults
        ResetChannels();

        // Detect controller
        DetectController();
    }

    void OnDisable()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        isConnected = false;
        packetsSent = 0;
        activeController = "None";
    }

    void Update()
    {
        // Lazy init: if config was assigned after OnEnable
        if (config != null && !isConnected && udpClient == null)
        {
            InitializeUDP();
        }
        
        if (!isConnected || udpClient == null) return;

        // Check for controller — try Gamepad first, then Joystick
        InputDevice device = Gamepad.current;
        if (device == null)
            device = Joystick.current;
        
        if (device == null)
        {
            if (activeController != "None")
            {
                activeController = "None";
                config.controllerName = "None";
                Debug.LogWarning("[RCInputBridge] Controller disconnected");
                ResetChannels();
            }
            return;
        }

        if (activeController != device.displayName)
        {
            activeController = device.displayName;
            config.controllerName = activeController;
            Debug.Log($"[RCInputBridge] Controller detected: {activeController}");
            
            // Log all available controls for debugging
            foreach (var control in device.allControls)
            {
                if (control is AxisControl)
                    Debug.Log($"  Axis: {control.name} = {((AxisControl)control).ReadValue()}");
            }
        }

        // Read stick axes (works with both Gamepad and Joystick)
        ReadStickInputGeneric(device);

        // Read AUX button toggles
        ReadAuxButtonsGeneric(device);

        // Send at configured rate
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend >= sendInterval)
        {
            timeSinceLastSend -= sendInterval;
            SendRCPacket();
        }

        // Debug preview
        if (showChannelValues)
            Array.Copy(channels, channelPreview, 16);
    }

    /// <summary>
    /// Reads stick axes from any InputDevice (Gamepad or Joystick)
    /// For Joystick: uses stick/x, stick/y, rz, z axes (EdgeTX standard)
    /// For Gamepad: uses leftStick/rightStick
    /// </summary>
    private void ReadStickInputGeneric(InputDevice device)
    {
        if (device is Gamepad gamepad)
        {
            float rollRaw = gamepad.rightStick.x.ReadValue();
            channels[config.roll.rcChannel] = AxisToPWM(rollRaw, config.roll);
            float pitchRaw = gamepad.rightStick.y.ReadValue();
            channels[config.pitch.rcChannel] = AxisToPWM(pitchRaw, config.pitch);
            float throttleRaw = gamepad.leftStick.y.ReadValue();
            channels[config.throttle.rcChannel] = ThrottleToPWM(throttleRaw, config.throttle);
            float yawRaw = gamepad.leftStick.x.ReadValue();
            channels[config.yaw.rcChannel] = AxisToPWM(yawRaw, config.yaw);
        }
        else if (device is Joystick joystick)
        {
            // EdgeTX/OpenTX USB Joystick: axes are stick/x, stick/y, stick/z, stick/rz etc.
            // TX16S Channel mapping: Axis0=Roll, Axis1=Pitch, Axis2=Throttle, Axis3=Yaw (AETR)
            var stick = joystick.stick;
            float rollRaw = ReadAxisSafe(device, "stick/x", 0);     // Axis 0 - Aileron
            float pitchRaw = ReadAxisSafe(device, "stick/y", 0);    // Axis 1 - Elevator
            float throttleRaw = ReadAxisSafe(device, "z", 0);       // Axis 2 - Throttle
            float yawRaw = ReadAxisSafe(device, "rz", 0);           // Axis 3 - Rudder
            
            channels[config.roll.rcChannel] = AxisToPWM(rollRaw, config.roll);
            channels[config.pitch.rcChannel] = AxisToPWM(pitchRaw, config.pitch);
            channels[config.throttle.rcChannel] = ThrottleToPWM(throttleRaw, config.throttle);
            channels[config.yaw.rcChannel] = AxisToPWM(yawRaw, config.yaw);
        }
    }

    private float ReadAxisSafe(InputDevice device, string controlName, float defaultValue)
    {
        var control = device.TryGetChildControl(controlName) as AxisControl;
        if (control != null)
            return control.ReadValue();
        return defaultValue;
    }

    /// <summary>
    /// Reads AUX buttons from any InputDevice
    /// </summary>
    private void ReadAuxButtonsGeneric(InputDevice device)
    {
        if (config.auxMappings == null) return;

        for (int i = 0; i < config.auxMappings.Length; i++)
        {
            ButtonControl btn = null;
            if (device is Gamepad gamepad)
                btn = GetAuxButton(gamepad, i);
            else if (device is Joystick)
                btn = device.TryGetChildControl($"button{i}") as ButtonControl;

            if (btn == null) continue;

            bool pressed = btn.isPressed;
            if (pressed && !auxButtonPrevState[i])
            {
                auxToggleState[i] = !auxToggleState[i];
                channels[config.auxMappings[i].rcChannel] = auxToggleState[i] ? (ushort)2000 : (ushort)1000;
            }
            auxButtonPrevState[i] = pressed;
        }
    }

    /// <summary>
    /// Reads gamepad stick axes and maps to RC channels
    /// </summary>
    private void ReadStickInput(Gamepad gamepad)
    {
        // Right stick horizontal → Roll (Aileron)
        float rollRaw = gamepad.rightStick.x.ReadValue();
        channels[config.roll.rcChannel] = AxisToPWM(rollRaw, config.roll);

        // Right stick vertical → Pitch (Elevator)
        float pitchRaw = gamepad.rightStick.y.ReadValue();
        channels[config.pitch.rcChannel] = AxisToPWM(pitchRaw, config.pitch);

        // Left stick vertical → Throttle
        // Throttle: -1 (bottom) = 1000, +1 (top) = 2000
        float throttleRaw = gamepad.leftStick.y.ReadValue();
        channels[config.throttle.rcChannel] = ThrottleToPWM(throttleRaw, config.throttle);

        // Left stick horizontal → Yaw (Rudder)
        float yawRaw = gamepad.leftStick.x.ReadValue();
        channels[config.yaw.rcChannel] = AxisToPWM(yawRaw, config.yaw);
    }

    /// <summary>
    /// Reads AUX button toggles — press to toggle between 1000 and 2000
    /// </summary>
    private void ReadAuxButtons(Gamepad gamepad)
    {
        if (config.auxMappings == null) return;

        for (int i = 0; i < config.auxMappings.Length; i++)
        {
            ButtonControl button = GetAuxButton(gamepad, i);
            if (button == null) continue;

            bool pressed = button.isPressed;
            // Toggle on rising edge
            if (pressed && !auxButtonPrevState[i])
            {
                auxToggleState[i] = !auxToggleState[i];
            }
            auxButtonPrevState[i] = pressed;

            channels[config.auxMappings[i].rcChannel] = auxToggleState[i] ? (ushort)2000 : (ushort)1000;
        }
    }

    /// <summary>
    /// Returns the gamepad button for an AUX mapping index
    /// Maps index 0-3 to South/East/West/North, 4-5 to shoulders, 6-7 to triggers
    /// </summary>
    private ButtonControl GetAuxButton(Gamepad gamepad, int index)
    {
        switch (index)
        {
            case 0: return gamepad.buttonSouth;
            case 1: return gamepad.buttonEast;
            case 2: return gamepad.buttonWest;
            case 3: return gamepad.buttonNorth;
            case 4: return gamepad.leftShoulder;
            case 5: return gamepad.rightShoulder;
            case 6: return gamepad.leftTrigger;
            case 7: return gamepad.rightTrigger;
            default: return null;
        }
    }

    /// <summary>
    /// Converts a stick axis value (-1..+1) to PWM (1000-2000) with deadzone and expo
    /// Center = 1500
    /// </summary>
    private ushort AxisToPWM(float raw, AxisMapping mapping)
    {
        if (mapping.invert) raw = -raw;

        // Apply deadzone
        if (Mathf.Abs(raw) < mapping.deadzone)
            raw = 0f;
        else
            raw = Mathf.Sign(raw) * (Mathf.Abs(raw) - mapping.deadzone) / (1f - mapping.deadzone);

        // Apply expo: output = (1-expo)*input + expo*input^3
        if (mapping.expo > 0f)
            raw = (1f - mapping.expo) * raw + mapping.expo * raw * raw * raw;

        // Map -1..+1 to 1000..2000
        float pwm = 1500f + raw * 500f;
        return (ushort)Mathf.Clamp(Mathf.RoundToInt(pwm), 1000, 2000);
    }

    /// <summary>
    /// Converts throttle axis (-1..+1) to PWM (1000-2000)
    /// Unlike other axes: -1 = 1000 (idle), +1 = 2000 (full)
    /// </summary>
    private ushort ThrottleToPWM(float raw, AxisMapping mapping)
    {
        if (mapping.invert) raw = -raw;

        // Apply deadzone
        if (Mathf.Abs(raw) < mapping.deadzone)
            raw = 0f;
        else
            raw = Mathf.Sign(raw) * (Mathf.Abs(raw) - mapping.deadzone) / (1f - mapping.deadzone);

        // Apply expo
        if (mapping.expo > 0f)
            raw = (1f - mapping.expo) * raw + mapping.expo * raw * raw * raw;

        // Map -1..+1 to 1000..2000 (bottom to top)
        float pwm = 1500f + raw * 500f;
        return (ushort)Mathf.Clamp(Mathf.RoundToInt(pwm), 1000, 2000);
    }

    /// <summary>
    /// Sends the current channel state as an RC packet to BF SITL
    /// </summary>
    private float lastSendErrorTime;

    private void SendRCPacket()
    {
        try
        {
            RCPacket packet = RCPacket.CreateDefault(Time.timeAsDouble);
            for (int i = 0; i < 16; i++)
                packet.SetChannel(i, channels[i]);

            byte[] data = StructToBytes(packet);
            udpClient.Send(data, data.Length);
            packetsSent++;
        }
        catch (Exception e)
        {
            // Rate-limit error logging to once every 5 seconds
            if (Time.time - lastSendErrorTime > 5f)
            {
                Debug.LogWarning($"[RCInputBridge] Send error (BF SITL not running?): {e.Message}");
                lastSendErrorTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Resets all channels to safe defaults
    /// </summary>
    private void ResetChannels()
    {
        for (int i = 0; i < 16; i++)
            channels[i] = 1000;

        // Center sticks (roll, pitch, yaw)
        channels[config.roll.rcChannel] = 1500;
        channels[config.pitch.rcChannel] = 1500;
        channels[config.yaw.rcChannel] = 1500;
        // Throttle stays at 1000 (idle)
    }

    /// <summary>
    /// Detects and logs the first connected gamepad
    /// </summary>
    private void DetectController()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            activeController = gamepad.displayName;
            config.controllerName = activeController;
            Debug.Log($"[RCInputBridge] Controller detected: {activeController}");
        }
        else
        {
            Debug.LogWarning("[RCInputBridge] No controller connected. Waiting for input device...");
        }
    }

    /// <summary>
    /// Converts a struct to byte array using Marshal
    /// </summary>
    private byte[] StructToBytes<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    // --- Public API ---

    public bool IsConnected => isConnected;
    public int PacketsSent => packetsSent;
    public string ActiveController => activeController;

    /// <summary>
    /// Gets the current PWM value for a channel (0-15)
    /// </summary>
    public ushort GetChannelValue(int channel)
    {
        if (channel < 0 || channel >= 16) return 1500;
        return channels[channel];
    }
}
