using UnityEngine;
using System.IO;

public class ControllerConfigPanel : MonoBehaviour
{
    [Header("References")]
    public ControllerConfig config;
    public RCInputBridge rcBridge;
    
    private bool showPanel = false;
    private Vector2 scrollPosition;
    
    // Temp values für Slider (OnGUI braucht float-Felder)
    private float rollDeadzoneTemp;
    private float rollExpoTemp;
    private float pitchDeadzoneTemp;
    private float pitchExpoTemp;
    private float throttleDeadzoneTemp;
    private float throttleExpoTemp;
    private float yawDeadzoneTemp;
    private float yawExpoTemp;
    
    private int windowID = 1;
    private Rect windowRect = new Rect(20, 20, 400, 600);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
            showPanel = !showPanel;
    }
    
    void OnGUI()
    {
        if (!showPanel) return;
        
        windowRect = GUILayout.Window(windowID, windowRect, DrawWindow, "Controller Configuration");
    }
    
    void DrawWindow(int windowID)
    {
        GUILayout.Space(10);
        
        // Controller info
        GUILayout.Label("Controller Info", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label($"Active Controller: {(rcBridge != null ? rcBridge.ActiveController : "N/A")}");
        GUILayout.Label($"Connected: {(rcBridge != null ? rcBridge.IsConnected.ToString() : "N/A")}");
        
        GUILayout.Space(10);
        
        // Scroll view for axis settings
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
        
        // Roll axis
        DrawAxisSettings("Roll", config.roll, ref rollDeadzoneTemp, ref rollExpoTemp);
        
        // Pitch axis
        DrawAxisSettings("Pitch", config.pitch, ref pitchDeadzoneTemp, ref pitchExpoTemp);
        
        // Throttle axis
        DrawAxisSettings("Throttle", config.throttle, ref throttleDeadzoneTemp, ref throttleExpoTemp);
        
        // Yaw axis
        DrawAxisSettings("Yaw", config.yaw, ref yawDeadzoneTemp, ref yawExpoTemp);
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        // Save/Load buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Configuration"))
        {
            SaveConfiguration();
        }
        if (GUILayout.Button("Load Configuration"))
        {
            LoadConfiguration();
        }
        GUILayout.EndHorizontal();
        
        // Test mode button
        GUILayout.Space(5);
        if (GUILayout.Button("Toggle Test Mode"))
        {
            ToggleTestMode();
        }
        
        GUI.DragWindow();
    }
    
    void DrawAxisSettings(string axisName, AxisMapping axisMapping, ref float deadzoneTemp, ref float expoTemp)
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Label(axisName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        
        // Live PWM value
        if (rcBridge != null)
        {
            ushort liveValue = rcBridge.GetChannelValue(axisMapping.rcChannel);
            GUILayout.Label($"Live Value: {liveValue} (Channel {axisMapping.rcChannel})");
        }
        
        // Invert toggle
        bool newInvert = GUILayout.Toggle(axisMapping.invert, "Invert Axis");
        if (newInvert != axisMapping.invert)
        {
            axisMapping.invert = newInvert;
        }
        
        // Deadzone slider
        GUILayout.Label($"Deadzone: {axisMapping.deadzone:F3}");
        deadzoneTemp = axisMapping.deadzone; // Sync temp value
        deadzoneTemp = GUILayout.HorizontalSlider(deadzoneTemp, 0f, 0.5f);
        if (Mathf.Abs(deadzoneTemp - axisMapping.deadzone) > 0.001f)
        {
            axisMapping.deadzone = deadzoneTemp;
        }
        
        // Expo slider
        GUILayout.Label($"Expo: {axisMapping.expo:F3}");
        expoTemp = axisMapping.expo; // Sync temp value
        expoTemp = GUILayout.HorizontalSlider(expoTemp, 0f, 1f);
        if (Mathf.Abs(expoTemp - axisMapping.expo) > 0.001f)
        {
            axisMapping.expo = expoTemp;
        }
        
        // RC Channel selection
        GUILayout.Label("RC Channel:");
        string[] channels = new string[16];
        for (int i = 0; i < 16; i++)
        {
            channels[i] = i.ToString();
        }
        
        int currentChannel = axisMapping.rcChannel;
        int newChannel = GUILayout.SelectionGrid(currentChannel, channels, 8);
        if (newChannel != currentChannel)
        {
            axisMapping.rcChannel = newChannel;
        }
    }
    
    void SaveConfiguration()
    {
        if (config == null) return;
        
        string filePath = Path.Combine(Application.persistentDataPath, "ControllerMapping.json");
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"Controller configuration saved to: {filePath}");
    }
    
    void LoadConfiguration()
    {
        if (config == null) return;
        
        string filePath = Path.Combine(Application.persistentDataPath, "ControllerMapping.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, config);
            Debug.Log($"Controller configuration loaded from: {filePath}");
            
            // Sync temp values after loading
            SyncTempValues();
        }
        else
        {
            Debug.LogWarning($"Configuration file not found: {filePath}");
        }
    }
    
    void ToggleTestMode()
    {
        if (rcBridge != null)
        {
            // This would typically toggle some debug visualization or special mode
            // For now we'll just log it
            Debug.Log("Test mode toggled");
        }
    }
    
    void SyncTempValues()
    {
        // Sync temp values with current config values
        rollDeadzoneTemp = config.roll.deadzone;
        rollExpoTemp = config.roll.expo;
        pitchDeadzoneTemp = config.pitch.deadzone;
        pitchExpoTemp = config.pitch.expo;
        throttleDeadzoneTemp = config.throttle.deadzone;
        throttleExpoTemp = config.throttle.expo;
        yawDeadzoneTemp = config.yaw.deadzone;
        yawExpoTemp = config.yaw.expo;
    }
    
    void OnEnable()
    {
        SyncTempValues();
    }
}