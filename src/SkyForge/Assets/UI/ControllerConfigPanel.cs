using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ControllerConfigPanel : MonoBehaviour
{
    [Header("References")]
    public ControllerConfig config;
    public RCInputBridge rcBridge;

    [Header("UI Toolkit")]
    [SerializeField] private VisualTreeAsset overlayAsset;
    [SerializeField] private string overlayResourcePath = "UI/ControllerConfigOverlay";

    private UIDocument uiDocument;
    private VisualElement root;
    private Label controllerLabel;
    private Label connectionLabel;
    private Button closeButton;
    private Button saveButton;
    private Button loadButton;
    private Button testButton;

    private bool isVisible;
    private bool callbacksRegistered;

    private readonly List<string> channelChoices = new List<string>(16);

    private AxisUI rollUI;
    private AxisUI pitchUI;
    private AxisUI throttleUI;
    private AxisUI yawUI;

    private struct AxisUI
    {
        public string Name;
        public AxisMapping Mapping;
        public Label LiveValue;
        public ProgressBar Progress;
        public Toggle InvertToggle;
        public Slider DeadzoneSlider;
        public Slider ExpoSlider;
        public DropdownField ChannelDropdown;
    }
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        if (overlayAsset == null && !string.IsNullOrEmpty(overlayResourcePath))
        {
            overlayAsset = Resources.Load<VisualTreeAsset>(overlayResourcePath);
        }

        if (overlayAsset == null)
        {
            Debug.LogError("[ControllerConfigPanel] Missing controller config overlay asset.");
            return;
        }

        uiDocument.visualTreeAsset = overlayAsset;

        if (uiDocument.panelSettings == null)
        {
            PanelSettings runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            runtimePanelSettings.referenceDpi = 96f;
            uiDocument.panelSettings = runtimePanelSettings;
        }

        for (int i = 0; i < 16; i++)
        {
            channelChoices.Add(i.ToString());
        }
    }

    private void OnEnable()
    {
        BuildUI();
        HideOverlay();
        SyncUIFromConfig();
        UpdateStatusLabels();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ToggleOverlay();
        }

        if (isVisible)
        {
            UpdateStatusLabels();
            UpdateLiveValues();
        }
    }

    private void BuildUI()
    {
        if (root != null)
        {
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[ControllerConfigPanel] UI Document did not create root visual element.");
            return;
        }

        controllerLabel = root.Q<Label>("controller-label");
        connectionLabel = root.Q<Label>("connection-label");
        closeButton = root.Q<Button>("close-button");
        saveButton = root.Q<Button>("save-button");
        loadButton = root.Q<Button>("load-button");
        testButton = root.Q<Button>("test-button");

        rollUI = BuildAxisUI("roll", config != null ? config.roll : null);
        pitchUI = BuildAxisUI("pitch", config != null ? config.pitch : null);
        throttleUI = BuildAxisUI("throttle", config != null ? config.throttle : null);
        yawUI = BuildAxisUI("yaw", config != null ? config.yaw : null);

        if (!callbacksRegistered)
        {
            RegisterCallbacks();
            callbacksRegistered = true;
        }
    }
    private AxisUI BuildAxisUI(string prefix, AxisMapping mapping)
    {
        AxisUI ui = new AxisUI
        {
            Name = prefix,
            Mapping = mapping,
            LiveValue = root.Q<Label>($"{prefix}-live"),
            Progress = root.Q<ProgressBar>($"{prefix}-progress"),
            InvertToggle = root.Q<Toggle>($"{prefix}-invert"),
            DeadzoneSlider = root.Q<Slider>($"{prefix}-deadzone"),
            ExpoSlider = root.Q<Slider>($"{prefix}-expo"),
            ChannelDropdown = root.Q<DropdownField>($"{prefix}-channel")
        };

        if (ui.Progress != null)
        {
            ui.Progress.lowValue = 1000f;
            ui.Progress.highValue = 2000f;
            ui.Progress.value = 1500f;
        }

        if (ui.ChannelDropdown != null)
        {
            ui.ChannelDropdown.choices = channelChoices;
        }

        return ui;
    }

    private void RegisterCallbacks()
    {
        if (closeButton != null)
        {
            closeButton.clicked += HideOverlay;
        }

        if (saveButton != null)
        {
            saveButton.clicked += SaveConfiguration;
        }

        if (loadButton != null)
        {
            loadButton.clicked += LoadConfiguration;
        }

        if (testButton != null)
        {
            testButton.clicked += ToggleTestMode;
        }

        RegisterAxisCallbacks(rollUI);
        RegisterAxisCallbacks(pitchUI);
        RegisterAxisCallbacks(throttleUI);
        RegisterAxisCallbacks(yawUI);
    }

    private void RegisterAxisCallbacks(AxisUI ui)
    {
        if (ui.Mapping == null)
        {
            return;
        }

        if (ui.InvertToggle != null)
        {
            ui.InvertToggle.RegisterValueChangedCallback(evt =>
            {
                ui.Mapping.invert = evt.newValue;
            });
        }

        if (ui.DeadzoneSlider != null)
        {
            ui.DeadzoneSlider.RegisterValueChangedCallback(evt =>
            {
                ui.Mapping.deadzone = evt.newValue;
            });
        }

        if (ui.ExpoSlider != null)
        {
            ui.ExpoSlider.RegisterValueChangedCallback(evt =>
            {
                ui.Mapping.expo = evt.newValue;
            });
        }

        if (ui.ChannelDropdown != null)
        {
            ui.ChannelDropdown.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int channel))
                {
                    ui.Mapping.rcChannel = channel;
                }
            });
        }
    }
    private void ToggleOverlay()
    {
        if (root == null)
        {
            return;
        }

        if (isVisible)
        {
            HideOverlay();
        }
        else
        {
            ShowOverlay();
        }
    }

    private void ShowOverlay()
    {
        if (root == null)
        {
            return;
        }

        root.style.display = DisplayStyle.Flex;
        isVisible = true;

        SyncUIFromConfig();
        UpdateStatusLabels();
        UpdateLiveValues();
    }

    private void HideOverlay()
    {
        if (root == null)
        {
            return;
        }

        root.style.display = DisplayStyle.None;
        isVisible = false;
    }

    private void SyncUIFromConfig()
    {
        SyncAxisUI(rollUI);
        SyncAxisUI(pitchUI);
        SyncAxisUI(throttleUI);
        SyncAxisUI(yawUI);
    }

    private void SyncAxisUI(AxisUI ui)
    {
        if (ui.Mapping == null)
        {
            return;
        }

        if (ui.InvertToggle != null)
        {
            ui.InvertToggle.SetValueWithoutNotify(ui.Mapping.invert);
        }

        if (ui.DeadzoneSlider != null)
        {
            ui.DeadzoneSlider.SetValueWithoutNotify(ui.Mapping.deadzone);
        }

        if (ui.ExpoSlider != null)
        {
            ui.ExpoSlider.SetValueWithoutNotify(ui.Mapping.expo);
        }

        if (ui.ChannelDropdown != null)
        {
            string value = Mathf.Clamp(ui.Mapping.rcChannel, 0, 15).ToString();
            ui.ChannelDropdown.SetValueWithoutNotify(value);
        }
    }
    private void UpdateStatusLabels()
    {
        if (controllerLabel != null)
        {
            string controllerName = rcBridge != null ? rcBridge.ActiveController : "None";
            controllerLabel.text = $"Controller: {controllerName}";
        }

        if (connectionLabel != null)
        {
            bool connected = rcBridge != null && rcBridge.IsConnected;
            connectionLabel.text = $"Connected: {(connected ? "Yes" : "No")}";
        }
    }

    private void UpdateLiveValues()
    {
        UpdateAxisLiveUI(rollUI);
        UpdateAxisLiveUI(pitchUI);
        UpdateAxisLiveUI(throttleUI);
        UpdateAxisLiveUI(yawUI);
    }

    private void UpdateAxisLiveUI(AxisUI ui)
    {
        if (ui.Mapping == null)
        {
            return;
        }

        ushort value = rcBridge != null ? rcBridge.GetChannelValue(ui.Mapping.rcChannel) : (ushort)1500;

        if (ui.LiveValue != null)
        {
            ui.LiveValue.text = $"Live: {value} (CH {ui.Mapping.rcChannel})";
        }

        if (ui.Progress != null)
        {
            float clamped = Mathf.Clamp(value, ui.Progress.lowValue, ui.Progress.highValue);
            ui.Progress.value = clamped;
        }
    }
    private void SaveConfiguration()
    {
        if (config == null)
        {
            Debug.LogWarning("[ControllerConfigPanel] Cannot save configuration - ControllerConfig missing.");
            return;
        }

        string filePath = Path.Combine(Application.persistentDataPath, "ControllerMapping.json");
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"Controller configuration saved to: {filePath}");
    }

    private void LoadConfiguration()
    {
        if (config == null)
        {
            Debug.LogWarning("[ControllerConfigPanel] Cannot load configuration - ControllerConfig missing.");
            return;
        }

        string filePath = Path.Combine(Application.persistentDataPath, "ControllerMapping.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Configuration file not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        JsonUtility.FromJsonOverwrite(json, config);
        SyncUIFromConfig();
        Debug.Log($"Controller configuration loaded from: {filePath}");
    }

    private void ToggleTestMode()
    {
        if (rcBridge != null)
        {
            Debug.Log("Test mode toggled");
        }
    }
}
