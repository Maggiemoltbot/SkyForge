using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private UIDocument m_Document;
    private VisualElement m_Root;

    // HUD Labels
    private Label altitudeValue;
    private Label speedValue;
    private Label modeValue;
    private Label armedValue;
    private Label batteryValue;
    private Label sitlValue;

    // Referenz zur Drone für Testdaten (in der Realität würde man das von Systemen beziehen)
    // private DroneController drone; // Annahme einer Drohnenkomponente

    void Awake()
    {
        m_Document = GetComponent<UIDocument>();
        if (m_Document == null)
        {
            Debug.LogError("HUDController requires a UIDocument component.");
            enabled = false;
            return;
        }

        UIPanelSettingsUtility.Ensure(m_Document, this, nameof(HUDController));
        m_Root = m_Document.rootVisualElement;
        if (m_Root == null)
        {
            Debug.LogError("HUDController could not access rootVisualElement on UIDocument.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        // Referenzen laden
        LoadHUDReferences();
    }

    void Update()
    {
        UpdateHUD();
    }

    void LoadHUDReferences()
    {
        altitudeValue = m_Root.Q<Label>("altitude-value");
        speedValue = m_Root.Q<Label>("speed-value");
        modeValue = m_Root.Q<Label>("mode-value");
        armedValue = m_Root.Q<Label>("armed-value");
        batteryValue = m_Root.Q<Label>("battery-value");
        sitlValue = m_Root.Q<Label>("sitl-value");
    }

    void UpdateHUD()
    {
        // Guard: skip if labels not found in UXML
        if (altitudeValue == null) return;

        // --- Simulierte Daten für DEMO ---
        Vector3 dronePosition = Vector3.zero;
        Vector3 droneVelocity = Vector3.zero;
        string currentMode = "FPV";
        bool isArmed = false;  // Default: DISARMED until Betaflight arms
        float battery = 75.0f;
        bool sitlConnected = false;
        // --- ENDE Simulation ---

        float altitude = dronePosition.y;
        altitudeValue.text = altitude.ToString("F1");

        float speed = droneVelocity.magnitude;
        if (speedValue != null) speedValue.text = speed.ToString("F1");

        if (modeValue != null) modeValue.text = currentMode;

        if (armedValue != null)
        {
            armedValue.text = isArmed ? "ARMED" : "DISARMED";
            armedValue.RemoveFromClassList("armed");
            armedValue.RemoveFromClassList("disarmed");
            armedValue.AddToClassList(isArmed ? "armed" : "disarmed");
        }

        if (batteryValue != null)
        {
            batteryValue.text = $"{battery:F0}%";
            batteryValue.RemoveFromClassList("low");
            if (battery < 20) batteryValue.AddToClassList("low");
        }

        if (sitlValue != null)
        {
            sitlValue.text = sitlConnected ? "CONNECTED" : "DISCONNECTED";
            sitlValue.RemoveFromClassList("connected");
            sitlValue.RemoveFromClassList("disconnected");
            sitlValue.AddToClassList(sitlConnected ? "connected" : "disconnected");
        }
    }
}