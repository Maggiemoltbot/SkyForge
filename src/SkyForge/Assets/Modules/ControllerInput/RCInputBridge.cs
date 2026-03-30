using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisallowMultipleComponent]
public class RCInputBridge : MonoBehaviour
{
    [Header("References")]
    public FlightDynamicsBridge flightDynamicsBridge;
    [SerializeField] private ControllerConfig config;

    [Header("Status (read-only)")]
    [SerializeField] private string activeController = "None";
    [SerializeField] private bool controllerAvailable;
    [SerializeField] private int packetsSent;

    [Header("Debug")]
    [SerializeField] private bool showChannelPreview;
    [SerializeField] private ushort[] channelPreview = new ushort[16];

    private readonly ushort[] channels = new ushort[16];
    private readonly int[] channelBuffer = new int[16];

    private bool[] auxToggleStates = Array.Empty<bool>();
    private bool[] auxButtonPrevious = Array.Empty<bool>();

    private InputDevice currentDevice;
    private float sendInterval = 0.01f;
    private float sendAccumulator;
    private float lastConfigWarningTime = -10f;

    private void Awake()
    {
        ResetChannels();
        SyncAuxCaches();
        UpdateSendInterval();
    }

    private void OnValidate()
    {
        SyncAuxCaches();
        UpdateSendInterval();
    }

    private void Update()
    {
        if (!EnsureConfig())
        {
            sendAccumulator = 0f;
            return;
        }

        UpdateDevice();
        UpdateSendInterval();

        if (controllerAvailable)
        {
            ReadStickInput();
            ReadAuxButtons();
            ReadAuxAxes();
        }

        sendAccumulator += Time.deltaTime;
        if (sendAccumulator >= sendInterval)
        {
            sendAccumulator -= sendInterval;
            FlushChannels();
        }

        if (showChannelPreview)
        {
            Array.Copy(channels, channelPreview, channels.Length);
        }
    }

    private bool EnsureConfig()
    {
        if (config != null)
        {
            return true;
        }

        if (Time.unscaledTime - lastConfigWarningTime > 5f)
        {
            Debug.LogWarning("[RCInputBridge] No ControllerConfig assigned. Waiting for configuration asset.");
            lastConfigWarningTime = Time.unscaledTime;
        }

        return false;
    }

    private void UpdateDevice()
    {
        InputDevice device = Gamepad.current ?? (InputDevice)Joystick.current;

        if (device == null)
        {
            if (controllerAvailable)
            {
                controllerAvailable = false;
                activeController = "None";
                config.controllerName = activeController;
                ResetChannels();
                Debug.LogWarning("[RCInputBridge] Controller disconnected.");
            }

            currentDevice = null;
            return;
        }

        if (device != currentDevice)
        {
            currentDevice = device;
            controllerAvailable = true;
            activeController = device.displayName;
            config.controllerName = activeController;
            Debug.Log($"[RCInputBridge] Controller detected: {activeController}");
        }
    }

    private void ReadStickInput()
    {
        if (currentDevice is Gamepad gamepad)
        {
            float rollRaw = gamepad.rightStick.x.ReadValue();
            channels[SafeChannel(config.roll.rcChannel)] = AxisToPwm(rollRaw, config.roll);

            float pitchRaw = gamepad.rightStick.y.ReadValue();
            channels[SafeChannel(config.pitch.rcChannel)] = AxisToPwm(pitchRaw, config.pitch);

            float throttleRaw = gamepad.leftStick.y.ReadValue();
            channels[SafeChannel(config.throttle.rcChannel)] = ThrottleToPwm(throttleRaw, config.throttle);

            float yawRaw = gamepad.leftStick.x.ReadValue();
            channels[SafeChannel(config.yaw.rcChannel)] = AxisToPwm(yawRaw, config.yaw);
        }
        else if (currentDevice is Joystick joystick)
        {
            float rollRaw = ReadAxis(joystick, "stick/x", 0f);
            channels[SafeChannel(config.roll.rcChannel)] = AxisToPwm(rollRaw, config.roll);

            float pitchRaw = ReadAxis(joystick, "stick/y", 0f);
            channels[SafeChannel(config.pitch.rcChannel)] = AxisToPwm(pitchRaw, config.pitch);

            float throttleRaw = ReadAxis(joystick, "z", 0f);
            channels[SafeChannel(config.throttle.rcChannel)] = ThrottleToPwm(throttleRaw, config.throttle);

            float yawRaw = ReadAxis(joystick, "rz", 0f);
            channels[SafeChannel(config.yaw.rcChannel)] = AxisToPwm(yawRaw, config.yaw);
        }
    }

    private void ReadAuxButtons()
    {
        if (config.auxMappings == null || config.auxMappings.Length == 0)
        {
            return;
        }

        EnsureAuxToggleCapacity();

        for (int i = 0; i < config.auxMappings.Length; i++)
        {
            var mapping = config.auxMappings[i];
            int channelIndex = SafeChannel(mapping.rcChannel);

            ButtonControl button = ResolveAuxButton(currentDevice, mapping, i);
            if (button == null)
            {
                channels[channelIndex] = auxToggleStates[i] ? (ushort)2000 : (ushort)1000;
                continue;
            }

            bool pressed = button.isPressed;
            if (pressed && !auxButtonPrevious[i])
            {
                auxToggleStates[i] = !auxToggleStates[i];
            }
            auxButtonPrevious[i] = pressed;

            channels[channelIndex] = auxToggleStates[i] ? (ushort)2000 : (ushort)1000;
        }
    }

    private void ReadAuxAxes()
    {
        if (config.auxAxisMappings == null || config.auxAxisMappings.Length == 0 || currentDevice == null)
        {
            return;
        }

        foreach (var mapping in config.auxAxisMappings)
        {
            if (string.IsNullOrEmpty(mapping.axisName))
            {
                continue;
            }

            var control = currentDevice.TryGetChildControl(mapping.axisName) as AxisControl;
            if (control == null)
            {
                continue;
            }

            float value = control.ReadValue();
            if (mapping.invert)
            {
                value = -value;
            }

            ushort pwm = mapping.mode switch
            {
                AuxAxisMode.TwoPosition => value >= mapping.threshold ? (ushort)2000 : (ushort)1000,
                AuxAxisMode.ThreePosition => value switch
                {
                    _ when value > mapping.threshold => (ushort)2000,
                    _ when value < -mapping.threshold => (ushort)1000,
                    _ => (ushort)1500
                },
                _ => (ushort)1500
            };

            channels[SafeChannel(mapping.rcChannel)] = pwm;
        }
    }

    private float ReadAxis(InputDevice device, string controlName, float defaultValue)
    {
        var control = device.TryGetChildControl(controlName) as AxisControl;
        return control != null ? control.ReadValue() : defaultValue;
    }

    private ButtonControl ResolveAuxButton(InputDevice device, AuxButtonMapping mapping, int index)
    {
        if (device is Gamepad gamepad)
        {
            string name = mapping.buttonName?.ToLowerInvariant() ?? string.Empty;

            if (name.Contains("south") || name.Contains("a") || name.Contains("cross"))
                return gamepad.buttonSouth;
            if (name.Contains("east") || name.Contains("b") || name.Contains("circle"))
                return gamepad.buttonEast;
            if (name.Contains("west") || name.Contains("x") || name.Contains("square"))
                return gamepad.buttonWest;
            if (name.Contains("north") || name.Contains("y") || name.Contains("triangle"))
                return gamepad.buttonNorth;
            if (name.Contains("left bumper") || name.Contains("lb"))
                return gamepad.leftShoulder;
            if (name.Contains("right bumper") || name.Contains("rb"))
                return gamepad.rightShoulder;
            if (name.Contains("left trigger") || name.Contains("lt"))
                return gamepad.leftTrigger;
            if (name.Contains("right trigger") || name.Contains("rt"))
                return gamepad.rightTrigger;

            return index switch
            {
                0 => gamepad.buttonSouth,
                1 => gamepad.buttonEast,
                2 => gamepad.buttonWest,
                3 => gamepad.buttonNorth,
                4 => gamepad.leftShoulder,
                5 => gamepad.rightShoulder,
                6 => gamepad.leftTrigger,
                7 => gamepad.rightTrigger,
                _ => null
            };
        }

        if (device is Joystick joystick)
        {
            return joystick.TryGetChildControl($"button{index}") as ButtonControl;
        }

        return null;
    }

    private ushort AxisToPwm(float raw, AxisMapping mapping)
    {
        float processed = ApplyAxisModifiers(raw, mapping);
        float pwm = 1500f + processed * 500f;
        return (ushort)Mathf.Clamp(Mathf.RoundToInt(pwm), 1000, 2000);
    }

    private ushort ThrottleToPwm(float raw, AxisMapping mapping)
    {
        float processed = ApplyAxisModifiers(raw, mapping);
        float normalized = (processed + 1f) * 0.5f;
        float pwm = Mathf.Lerp(1000f, 2000f, Mathf.Clamp01(normalized));
        return (ushort)Mathf.RoundToInt(pwm);
    }

    private float ApplyAxisModifiers(float raw, AxisMapping mapping)
    {
        if (mapping.invert)
        {
            raw = -raw;
        }

        if (Mathf.Abs(raw) < mapping.deadzone)
        {
            raw = 0f;
        }
        else
        {
            float sign = Mathf.Sign(raw);
            float magnitude = (Mathf.Abs(raw) - mapping.deadzone) / Mathf.Max(1e-5f, 1f - mapping.deadzone);
            raw = Mathf.Clamp(sign * magnitude, -1f, 1f);
        }

        if (mapping.expo > 0f)
        {
            raw = Mathf.Lerp(raw, raw * raw * raw, Mathf.Clamp01(mapping.expo));
        }

        return Mathf.Clamp(raw, -1f, 1f);
    }

    private void FlushChannels()
    {
        for (int i = 0; i < channels.Length; i++)
        {
            channelBuffer[i] = channels[i];
        }

        if (flightDynamicsBridge != null)
        {
            flightDynamicsBridge.SendRCChannels(channelBuffer);
            packetsSent++;
        }
    }

    private void ResetChannels()
    {
        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] = 1000;
        }

        if (config != null)
        {
            channels[SafeChannel(config.roll.rcChannel)] = 1500;
            channels[SafeChannel(config.pitch.rcChannel)] = 1500;
            channels[SafeChannel(config.yaw.rcChannel)] = 1500;
            channels[SafeChannel(config.throttle.rcChannel)] = 1000;
        }
    }

    private void SyncAuxCaches()
    {
        int required = config != null && config.auxMappings != null ? config.auxMappings.Length : 0;
        if (auxToggleStates.Length != required)
        {
            Array.Resize(ref auxToggleStates, required);
        }

        if (auxButtonPrevious.Length != required)
        {
            Array.Resize(ref auxButtonPrevious, required);
        }
    }

    private void EnsureAuxToggleCapacity()
    {
        if (config == null)
        {
            auxToggleStates = Array.Empty<bool>();
            auxButtonPrevious = Array.Empty<bool>();
            return;
        }

        if (auxToggleStates.Length != config.auxMappings.Length)
        {
            Array.Resize(ref auxToggleStates, config.auxMappings.Length);
        }

        if (auxButtonPrevious.Length != config.auxMappings.Length)
        {
            Array.Resize(ref auxButtonPrevious, config.auxMappings.Length);
        }
    }

    private int SafeChannel(int channel)
    {
        return Mathf.Clamp(channel, 0, channels.Length - 1);
    }

    private void UpdateSendInterval()
    {
        if (config == null)
        {
            sendInterval = 0.01f;
            return;
        }

        int rate = Mathf.Max(1, config.sendRateHz);
        float desiredInterval = 1f / rate;
        if (!Mathf.Approximately(desiredInterval, sendInterval))
        {
            sendInterval = desiredInterval;
            sendAccumulator = Mathf.Min(sendAccumulator, sendInterval);
        }
    }

    public bool IsConnected => flightDynamicsBridge != null && flightDynamicsBridge.IsConnected;
    public bool HasController => controllerAvailable;
    public string ActiveController => activeController;
    public int PacketsSent => packetsSent;
    public ControllerConfig ControllerConfig => config;

    public ushort GetChannelValue(int channelIndex)
    {
        if (channelIndex < 0 || channelIndex >= channels.Length)
        {
            return 1500;
        }

        return channels[channelIndex];
    }
}
