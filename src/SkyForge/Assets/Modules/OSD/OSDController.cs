using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class OSDController : MonoBehaviour
{
    [Header("References")]
    public Camera fpvCamera;
    public UIDocument uiDocument;
    public GameObject targetGameObject; // Reference to the GameObject with the camera

    [Header("Settings")]
    public bool showOSD = true;
    public KeyCode toggleKey = KeyCode.F5;
    public KeyCode fpvToggleKey = KeyCode.F2;
    public string fpvCameraTag = "FPVCamera";

    private VisualElement root;
    private Label batteryLabel;
    private Label timerLabel;
    private Label modeLabel;
    private Label altitudeLabel;
    private Label rssiLabel;
    private Label armedLabel;

    private Camera currentCamera;
    private bool isFPVMode = false;

    void Start()
    {
        // Find FPV camera if not assigned
        if (fpvCamera == null && !string.IsNullOrEmpty(fpvCameraTag))
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.CompareTag(fpvCameraTag))
                {
                    fpvCamera = cam;
                    break;
                }
            }
        }

        // Find the camera component on the target GameObject if it exists
        if (targetGameObject != null)
        {
            currentCamera = targetGameObject.GetComponent<Camera>();
            if (currentCamera == null)
            {
                // Try to get camera from CameraManager or other sources
                CameraManager cm = FindFirstObjectByType<CameraManager>();
                if (cm != null)
                {
                    currentCamera = cm.GetComponent<Camera>();
                }
            }
        }

        // Initialize UI
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;
            InitializeLabels();
            UpdateVisibility();
        }
        else
        {
            Debug.LogError("OSDController: UIDocument is not assigned!");
        }
    }

    void Update()
    {
        // Toggle OSD visibility
        if (Input.GetKeyDown(toggleKey))
        {
            showOSD = !showOSD;
            UpdateVisibility();
        }

        // Check if we're in FPV mode
        if (Input.GetKeyDown(fpvToggleKey))
        {
            StartCoroutine(DetectFPVModeDelayed());
        }
        else
        {
            // Try to detect FPV mode by checking active camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                isFPVMode = mainCam == fpvCamera;
                UpdateVisibility();
            }
        }

        // Update OSD data if visible and in FPV mode
        if (showOSD && isFPVMode && root != null)
        {
            UpdateOSDData();
        }
    }

    private IEnumerator DetectFPVModeDelayed()
    {
        // Wait one frame to allow CameraManager to update
        yield return null;
        
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            isFPVMode = mainCam == fpvCamera;
            UpdateVisibility();
        }
    }

    private void InitializeLabels()
    {
        batteryLabel = root.Q<Label>("batteryLabel");
        timerLabel = root.Q<Label>("timerLabel");
        modeLabel = root.Q<Label>("modeLabel");
        altitudeLabel = root.Q<Label>("altitudeLabel");
        rssiLabel = root.Q<Label>("rssiLabel");
        armedLabel = root.Q<Label>("armedLabel");

        if (batteryLabel == null || timerLabel == null || modeLabel == null || 
            altitudeLabel == null || rssiLabel == null || armedLabel == null)
        {
            Debug.LogError("OSDController: One or more labels not found in UIDocument!");
            return;
        }
    }

    private void UpdateVisibility()
    {
        VisualElement root = uiDocument?.rootVisualElement;
        if (root == null) return;

        // Show OSD only in FPV mode and when enabled
        bool visible = showOSD && isFPVMode;
        root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    private void UpdateOSDData()
    {
        if (OSDData.Instance == null) return;

        // Update battery
        string batteryText = $"V:{OSDData.Instance.batteryVoltage:F1}V";
        batteryLabel.text = batteryText;
        batteryLabel.RemoveFromClassList("warning");
        batteryLabel.RemoveFromClassList("critical");
        if (OSDData.Instance.batteryVoltage < 14.8f)
        {
            batteryLabel.AddToClassList("warning");
        }
        if (OSDData.Instance.batteryVoltage < 13.2f)
        {
            batteryLabel.AddToClassList("critical");
        }

        // Update timer
        timerLabel.text = OSDData.Instance.GetFormattedFlightTime();

        // Update flight mode
        modeLabel.text = OSDData.Instance.flightMode;

        // Update altitude
        string altitudeText = $"ALT:{OSDData.Instance.altitude:F1}m {OSDData.Instance.verticalSpeed:F1}m/s";
        altitudeLabel.text = altitudeText;

        // Update RSSI
        string rssiText = $"RSSI:{OSDData.Instance.rssi}%";
        rssiLabel.text = rssiText;

        // Update armed status
        string armedText = OSDData.Instance.isArmed ? "ARMED" : "DISARMED";
        armedLabel.text = armedText;
        armedLabel.RemoveFromClassList("warning");
        armedLabel.RemoveFromClassList("critical");
        if (OSDData.Instance.isArmed)
        {
            armedLabel.AddToClassList("warning");
        }

        // Increment flight time
        OSDData.Instance.IncrementFlightTime(Time.deltaTime);
    }

    void OnDestroy()
    {
        if (uiDocument != null && root != null)
        {
            root.style.visibility = Visibility.Hidden;
        }
    }
}