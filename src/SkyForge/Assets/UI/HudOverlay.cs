using UnityEngine;

public class HudOverlay : MonoBehaviour
{
    [Header("References")]
    public DroneController drone;
    public CameraManager cameraManager;
    public RCInputBridge rcBridge;
    
    [Header("Battery")]
    [SerializeField] private float batteryPercent = 100f;
    [SerializeField] private float drainRatePerSecond = 0.1f; // ~1% per 10s
    
    private bool isArmed = false;
    private Rigidbody droneRb;
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    
    void Start()
    {
        if (drone != null)
            droneRb = drone.GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        // Check armed status (any motor PWM > 0.1)
        if (drone != null)
        {
            isArmed = false;
            for (int i = 0; i < drone.motorPWM.Length; i++)
            {
                if (drone.motorPWM[i] > 0.1f)
                {
                    isArmed = true;
                    break;
                }
            }
        }
        
        // Drain battery when armed
        if (isArmed && batteryPercent > 0)
            batteryPercent -= drainRatePerSecond * Time.deltaTime;
    }
    
    void OnGUI()
    {
        // Debug: always show something even if references are null
        GUI.Label(new Rect(10, 10, 200, 30), "HUD ACTIVE", new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = Color.magenta } });
        
        // Setup styles once
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = Color.green; // HUD green
            labelStyle.fontStyle = FontStyle.Bold;
            
            boxStyle = new GUIStyle(GUI.skin.box);
            // Semi-transparent dark background
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.6f)); // Dark semi-transparent
            backgroundTexture.Apply();
            boxStyle.normal.background = backgroundTexture;
        }
        
        float width = 200;
        float height = 160;
        float x = Screen.width - width - 10;
        float y = Screen.height - height - 10;
        
        // Background box
        GUI.Box(new Rect(x, y, width, height), "", boxStyle);
        
        // Content
        float lineY = y + 5;
        float lineH = 22;
        
        // ALT
        float alt = drone != null ? drone.transform.position.y : 0;
        GUI.Label(new Rect(x + 10, lineY, width, lineH), $"ALT: {alt:F1}m", labelStyle);
        lineY += lineH;
        
        // SPD
        float spd = droneRb != null ? droneRb.linearVelocity.magnitude : 0; // Unity 6: linearVelocity statt velocity
        GUI.Label(new Rect(x + 10, lineY, width, lineH), $"SPD: {spd:F1} m/s", labelStyle);
        lineY += lineH;
        
        // MODE
        string mode = cameraManager != null ? cameraManager.CurrentMode.ToString() : "N/A";
        GUI.Label(new Rect(x + 10, lineY, width, lineH), $"MODE: {mode}", labelStyle);
        lineY += lineH;
        
        // ARM
        string armText = isArmed ? "ARMED" : "DISARMED";
        Color armColor = isArmed ? Color.red : Color.green;
        GUIStyle armStyle = new GUIStyle(labelStyle);
        armStyle.normal.textColor = armColor;
        GUI.Label(new Rect(x + 10, lineY, width, lineH), armText, armStyle);
        lineY += lineH;
        
        // BAT
        Color batColor = batteryPercent > 30 ? Color.green : (batteryPercent > 10 ? Color.yellow : Color.red);
        GUIStyle batStyle = new GUIStyle(labelStyle);
        batStyle.normal.textColor = batColor;
        GUI.Label(new Rect(x + 10, lineY, width, lineH), $"BAT: {batteryPercent:F0}%", batStyle);
        lineY += lineH;
        
        // SITL
        bool connected = rcBridge != null && rcBridge.IsConnected;
        string sitlText = connected ? "SITL: CONNECTED" : "SITL: DISCONNECTED";
        GUIStyle sitlStyle = new GUIStyle(labelStyle);
        sitlStyle.normal.textColor = connected ? Color.green : Color.red;
        GUI.Label(new Rect(x + 10, lineY, width, lineH), sitlText, sitlStyle);
    }
}