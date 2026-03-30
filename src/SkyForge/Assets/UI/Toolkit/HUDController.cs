using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    public VisualTreeAsset uiAsset;
    public UIDocument uiDocument;
    
    [Header("Drone References")]
    public DroneController drone;
    public CameraManager cameraManager;
    public RCInputBridge rcBridge;
    
    [Header("Battery Settings")]
    [SerializeField] private float batteryPercent = 100f;
    [SerializeField] private float drainRatePerSecond = 0.1f; // ~1% per 10s
    
    private bool isArmed = false;
    private Rigidbody droneRb;
    private VisualElement root;
    
    // UI Elements
    private Label altitudeLabel;
    private Label speedLabel;
    private Label cameraModeLabel;
    private Label armedLabel;
    private Label batteryLabel;
    private Label sitlStatusLabel;
    
    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
            
        root = uiDocument.rootVisualElement;
        
        // Find references if not assigned
        if (drone == null)
            drone = Object.FindFirstObjectByType<DroneController>();
            
        if (cameraManager == null)
            cameraManager = Object.FindFirstObjectByType<CameraManager>();
            
        if (rcBridge == null)
            rcBridge = Object.FindFirstObjectByType<RCInputBridge>();
            
        if (drone != null)
            droneRb = drone.GetComponent<Rigidbody>();
    }
    
    void OnEnable()
    {
        SetupUI();
        UpdateHUD();
    }
    
    void SetupUI()
    {
        // Get references to UI elements
        altitudeLabel = root.Q<Label>("altitude-label");
        speedLabel = root.Q<Label>("speed-label");
        cameraModeLabel = root.Q<Label>("camera-mode-label");
        armedLabel = root.Q<Label>("armed-label");
        batteryLabel = root.Q<Label>("battery-label");
        sitlStatusLabel = root.Q<Label>("sitl-status");
        
        // Validate all elements are found
        if (altitudeLabel == null) Debug.LogError("Altitude label not found");
        if (speedLabel == null) Debug.LogError("Speed label not found");
        if (cameraModeLabel == null) Debug.LogError("Camera mode label not found");
        if (armedLabel == null) Debug.LogError("Armed label not found");
        if (batteryLabel == null) Debug.LogError("Battery label not found");
        if (sitlStatusLabel == null) Debug.LogError("SITL status label not found");
    }
    
    void Update()
    {
        UpdateHUD();
    }
    
    void UpdateHUD()
    {
        // Update armed status
        UpdateArmedStatus();
        
        // Update battery
        UpdateBattery();
        
        // Update flight data
        UpdateFlightData();
        
        // Update connection status
        UpdateConnectionStatus();
    }
    
    void UpdateArmedStatus()
    {
        if (drone == null)
        {
            isArmed = false;
            return;
        }
        
        // Check if any motor PWM is above threshold
        isArmed = false;
        for (int i = 0; i < drone.motorPWM.Length; i++)
        {
            if (drone.motorPWM[i] > 0.1f)
            {
                isArmed = true;
                break;
            }
        }
        
        // Update UI
        if (armedLabel != null)
        {
            armedLabel.text = isArmed ? "ARMED" : "DISARMED";
            armedLabel.RemoveFromClassList("armed");
            armedLabel.RemoveFromClassList("disarmed");
            armedLabel.AddToClassList(isArmed ? "armed" : "disarmed");
        }
    }
    
    void UpdateBattery()
    {
        // Drain battery when armed
        if (isArmed && batteryPercent > 0)
            batteryPercent -= drainRatePerSecond * Time.deltaTime;
            
        // Clamp battery
        batteryPercent = Mathf.Clamp(batteryPercent, 0f, 100f);
        
        // Update UI
        if (batteryLabel != null)
            batteryLabel.text = $"BAT: {batteryPercent:F0}%";
    }
    
    void UpdateFlightData()
    {
        // Altitude
        float alt = drone != null ? drone.transform.position.y : 0;
        if (altitudeLabel != null)
            altitudeLabel.text = $"ALT: {alt:F1}m";
            
        // Speed
        float spd = droneRb != null ? droneRb.linearVelocity.magnitude : 0;
        if (speedLabel != null)
            speedLabel.text = $"SPD: {spd:F1} m/s";
            
        // Camera Mode
        string mode = cameraManager != null ? cameraManager.CurrentMode.ToString() : "N/A";
        if (cameraModeLabel != null)
            cameraModeLabel.text = $"MODE: {mode}";
    }
    
    void UpdateConnectionStatus()
    {
        // SITL Connection
        bool connected = rcBridge != null && rcBridge.IsConnected;
        if (sitlStatusLabel != null)
        {
            sitlStatusLabel.text = connected ? "SITL: CONNECTED" : "SITL: DISCONNECTED";
            sitlStatusLabel.RemoveFromClassList("connected");
            sitlStatusLabel.RemoveFromClassList("disconnected");
            sitlStatusLabel.AddToClassList(connected ? "connected" : "disconnected");
        }
    }
}