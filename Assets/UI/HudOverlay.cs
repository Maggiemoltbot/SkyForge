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
    private GUIStyle hudHeaderStyle;
    private GUIStyle armArmedStyle;
    private GUIStyle armDisarmedStyle;
    private GUIStyle batteryHighStyle;
    private GUIStyle batteryWarningStyle;
    private GUIStyle batteryCriticalStyle;
    private GUIStyle sitlConnectedStyle;
    private GUIStyle sitlDisconnectedStyle;
    private Texture2D boxBackgroundTexture;
    
    void Start()
    {
        if (drone != null)
            droneRb = drone.GetComponent<Rigidbody>();
    }
    
    void OnDestroy()
    {
        if (boxBackgroundTexture != null)
        {
            Destroy(boxBackgroundTexture);
            boxBackgroundTexture = null;
        }
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
    
    void EnsureStylesInitialized()
    {
        if (labelStyle != null)
            return;
        
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.green;
        
        hudHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        hudHeaderStyle.normal.textColor = Color.magenta;
        
        boxStyle = new GUIStyle(GUI.skin.box);
        boxBackgroundTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        boxBackgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
        boxBackgroundTexture.Apply();
        boxStyle.normal.background = boxBackgroundTexture;
        
        armArmedStyle = new GUIStyle(labelStyle);
        armArmedStyle.normal.textColor = Color.red;
        
        armDisarmedStyle = new GUIStyle(labelStyle);
        armDisarmedStyle.normal.textColor = Color.green;
        
        batteryHighStyle = new GUIStyle(labelStyle);
        batteryHighStyle.normal.textColor = Color.green;
        
        batteryWarningStyle = new GUIStyle(labelStyle);
        batteryWarningStyle.normal.textColor = Color.yellow;
        
        batteryCriticalStyle = new GUIStyle(labelStyle);
        batteryCriticalStyle.normal.textColor = Color.red;
        
        sitlConnectedStyle = new GUIStyle(labelStyle);
        sitlConnectedStyle.normal.textColor = Color.green;
        
        sitlDisconnectedStyle = new GUIStyle(labelStyle);
        sitlDisconnectedStyle.normal.textColor = Color.red;
    }
    
    void OnGUI()
    {
        EnsureStylesInitialized();
        
        // Debug: always show something even if references are null
        GUI.Label(new Rect(10, 10, 200, 30), "HUD ACTIVE", hudHeaderStyle);
        
        float width = 200f;
        float height = 160f;
        float x = Screen.width - width - 10f;
        float y = Screen.height - height - 10f;
        
        // Background box
        GUI.Box(new Rect(x, y, width, height), string.Empty, boxStyle);
        
        // Content
        float lineY = y + 5f;
        const float lineH = 22f;
        
        // ALT
        float alt = drone != null ? drone.transform.position.y : 0f;
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), $"ALT: {alt:F1}m", labelStyle);
        lineY += lineH;
        
        // SPD
        float spd = droneRb != null ? droneRb.linearVelocity.magnitude : 0f; // Unity 6: linearVelocity statt velocity
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), $"SPD: {spd:F1} m/s", labelStyle);
        lineY += lineH;
        
        // MODE
        string mode = cameraManager != null ? cameraManager.CurrentMode.ToString() : "N/A";
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), $"MODE: {mode}", labelStyle);
        lineY += lineH;
        
        // ARM
        string armText = isArmed ? "ARMED" : "DISARMED";
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), armText, isArmed ? armArmedStyle : armDisarmedStyle);
        lineY += lineH;
        
        // BAT
        GUIStyle batteryStyle = batteryPercent > 30f
            ? batteryHighStyle
            : (batteryPercent > 10f ? batteryWarningStyle : batteryCriticalStyle);
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), $"BAT: {batteryPercent:F0}%", batteryStyle);
        lineY += lineH;
        
        // SITL
        bool connected = rcBridge != null && rcBridge.IsConnected;
        GUI.Label(new Rect(x + 10f, lineY, width, lineH), connected ? "SITL: CONNECTED" : "SITL: DISCONNECTED", connected ? sitlConnectedStyle : sitlDisconnectedStyle);
    }
}