using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Reflection;

public class ControllerDebugWindow : EditorWindow
{
    private RCInputBridge rcInputBridge;
    private Vector2 scrollPosition;
    
    [MenuItem("SkyForge/Controller Debug")]
    public static void ShowWindow()
    {
        GetWindow<ControllerDebugWindow>("Controller Debug");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Controller Debug Window", EditorStyles.boldLabel);
        
        // Find RCInputBridge in the scene
        if (rcInputBridge == null)
        {
            rcInputBridge = FindObjectOfType<RCInputBridge>();
            if (rcInputBridge == null)
            {
                EditorGUILayout.HelpBox("No RCInputBridge found in scene", MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                {
                    rcInputBridge = FindObjectOfType<RCInputBridge>();
                }
                return;
            }
        }
        
        // Display controller info
        FieldInfo configField = typeof(RCInputBridge).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);
        ControllerConfig config = configField?.GetValue(rcInputBridge) as ControllerConfig;
        if (config != null)
        {
            GUILayout.Label("Controller Configuration", EditorStyles.boldLabel);
            EditorGUILayout.TextField("Controller Name", config.controllerName);
            EditorGUILayout.IntField("Send Rate (Hz)", config.sendRateHz);
        }

        FlightDynamicsBridge bridge = rcInputBridge.flightDynamicsBridge;
        if (bridge != null)
        {
            FieldInfo bridgeConfigField = typeof(FlightDynamicsBridge).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);
            BridgeConfig bridgeConfig = bridgeConfigField?.GetValue(bridge) as BridgeConfig;
            if (bridgeConfig != null)
            {
                GUILayout.Label("Bridge Configuration", EditorStyles.boldLabel);
                EditorGUILayout.TextField("BF SITL IP", bridgeConfig.bfSITLIPAddress);
                EditorGUILayout.IntField("FDM Port", bridgeConfig.fdmSendPort);
                EditorGUILayout.IntField("PWM Port", bridgeConfig.pwmReceivePort);
                EditorGUILayout.IntField("RC Port", bridgeConfig.rcSendPort);
            }
        }
        
        EditorGUILayout.Space();
        
        // Display RC channels
        GUILayout.Label("RC Channels", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        // Try to get channel values using reflection
        MethodInfo getChannelMethod = typeof(RCInputBridge).GetMethod("GetChannelValue", BindingFlags.Public | BindingFlags.Instance);
        if (getChannelMethod != null)
        {
            for (int i = 0; i < 16; i++)
            {
                object result = getChannelMethod.Invoke(rcInputBridge, new object[] { i });
                ushort value = (ushort)result;
                EditorGUILayout.IntSlider($"Channel {i}", value, 1000, 2000);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Unable to access channel values", MessageType.Warning);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Display input device info
        GUILayout.Label("Input Device Information", EditorStyles.boldLabel);
        InputDevice device = (InputDevice)Gamepad.current;
        if (device == null)
            device = (InputDevice)Joystick.current;
            
        if (device != null)
        {
            EditorGUILayout.TextField("Device Name", device.displayName);
            EditorGUILayout.TextField("Device Type", device.GetType().Name);
            
            // List all controls
            EditorGUILayout.LabelField("Available Controls:", EditorStyles.boldLabel);
            foreach (var control in device.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.AxisControl axisControl)
                {
                    float value = axisControl.ReadValue();
                    EditorGUILayout.TextField($"  {axisControl.name}", value.ToString("F3"));
                }
                else if (control is UnityEngine.InputSystem.Controls.ButtonControl buttonControl)
                {
                    bool isPressed = buttonControl.isPressed;
                    EditorGUILayout.Toggle($"  {buttonControl.name}", isPressed);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No input device connected", MessageType.Info);
        }
        
        // Auto-refresh GUI
        Repaint();
    }
}