using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class ControllerSetupController : MonoBehaviour
{
    [Header("UI References")]
    public VisualTreeAsset uiAsset;
    public UIDocument uiDocument;
    
    [Header("Configuration")]
    public float calibrationThreshold = 0.3f; // Minimum movement to detect axis
    public float detectionTimeout = 5f; // Seconds to wait for user input
    
    private VisualElement root;
    private Label wizardSubtitle;
    private Label instructionText;
    private ControllerConfig config;
    private RCInputBridge rcBridge;
    
    // Step tracking
    private enum SetupStep { Throttle, Yaw, Pitch, Roll }
    private SetupStep currentStep = SetupStep.Throttle;
    private bool waitingForInput = false;
    private float timeoutTimer = 0f;
    
    // UI Elements for each step
    private readonly string[] stepTitles = { "Throttle", "Yaw", "Pitch", "Roll" };
    private readonly string[] stepInstructions = {
        "Move your Throttle stick up and down to calibrate",
        "Move your Yaw stick left and right to calibrate",
        "Move your Pitch stick forward and backward to calibrate",
        "Move your Roll stick left and right to calibrate"
    };
    
    // Visual elements for live preview
    private VisualElement throttleBar;
    private VisualElement yawBar;
    private VisualElement pitchBar;
    private VisualElement rollBar;
    private VisualElement throttleFill;
    private VisualElement yawFill;
    private VisualElement pitchFill;
    private VisualElement rollFill;
    private Label throttleValue;
    private Label yawValue;
    private Label pitchValue;
    private Label rollValue;
    
    // Axis settings UI elements
    private VisualElement throttleSettings;
    private VisualElement yawSettings;
    private VisualElement pitchSettings;
    private VisualElement rollSettings;
    private Toggle throttleInvert;
    private Toggle yawInvert;
    private Toggle pitchInvert;
    private Toggle rollInvert;
    private Slider throttleDeadzone;
    private Slider yawDeadzone;
    private Slider pitchDeadzone;
    private Slider rollDeadzone;
    private Label throttleDeadzoneValue;
    private Label yawDeadzoneValue;
    private Label pitchDeadzoneValue;
    private Label rollDeadzoneValue;
    
    // Navigation buttons
    private Button prevButton;
    private Button testButton;
    private Button nextButton;
    
    // Progressive step completion tracking
    private bool[] stepCompleted = new bool[4];
    
    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
            
        root = uiDocument.rootVisualElement;
        
        // Find controller components
        config = FindObjectOfType<ControllerConfig>();
        rcBridge = FindObjectOfType<RCInputBridge>();
        
        if (config == null)
            Debug.LogError("ControllerConfig not found in scene");
            
        if (rcBridge == null)
            Debug.LogError("RCInputBridge not found in scene");
    }
    
    void OnEnable()
    {
        SetupUI();
        BindEvents();
        UpdateStep();
    }
    
    void SetupUI()
    {
        // Initialize wizard labels
        wizardSubtitle = root.Q<Label>("wizard-subtitle");
        instructionText = root.Q<Label>("instruction-text");
        
        // Live preview elements
        throttleBar = root.Q<VisualElement>("throttle-bar");
        yawBar = root.Q<VisualElement>("yaw-bar");
        pitchBar = root.Q<VisualElement>("pitch-bar");
        rollBar = root.Q<VisualElement>("roll-bar");
        throttleFill = root.Q<VisualElement>("throttle-fill");
        yawFill = root.Q<VisualElement>("yaw-fill");
        pitchFill = root.Q<VisualElement>("pitch-fill");
        rollFill = root.Q<VisualElement>("roll-fill");
        throttleValue = root.Q<Label>("throttle-value");
        yawValue = root.Q<Label>("yaw-value");
        pitchValue = root.Q<Label>("pitch-value");
        rollValue = root.Q<Label>("roll-value");
        
        // Axis settings panels
        throttleSettings = root.Q<VisualElement>("throttle-settings");
        yawSettings = root.Q<VisualElement>("yaw-settings");
        pitchSettings = root.Q<VisualElement>("pitch-settings");
        rollSettings = root.Q<VisualElement>("roll-settings");
        
        // Axis setting controls
        throttleInvert = root.Q<Toggle>("throttle-invert");
        yawInvert = root.Q<Toggle>("yaw-invert");
        pitchInvert = root.Q<Toggle>("pitch-invert");
        rollInvert = root.Q<Toggle>("roll-invert");
        
        throttleDeadzone = root.Q<Slider>("throttle-deadzone");
        yawDeadzone = root.Q<Slider>("yaw-deadzone");
        pitchDeadzone = root.Q<Slider>("pitch-deadzone");
        rollDeadzone = root.Q<Slider>("roll-deadzone");
        
        throttleDeadzoneValue = root.Q<Label>("throttle-deadzone-value");
        yawDeadzoneValue = root.Q<Label>("yaw-deadzone-value");
        pitchDeadzoneValue = root.Q<Label>("pitch-deadzone-value");
        rollDeadzoneValue = root.Q<Label>("roll-deadzone-value");
        
        // Navigation buttons
        prevButton = root.Q<Button>("prev-button");
        testButton = root.Q<Button>("test-button");
        nextButton = root.Q<Button>("next-button");
        
        // Initialize settings values
        if (config != null)
        {
            // Set initial toggle states
            if (throttleInvert != null) throttleInvert.value = config.throttle.invert;
            if (yawInvert != null) yawInvert.value = config.yaw.invert;
            if (pitchInvert != null) pitchInvert.value = config.pitch.invert;
            if (rollInvert != null) rollInvert.value = config.roll.invert;
            
            // Set initial slider values
            if (throttleDeadzone != null) throttleDeadzone.value = config.throttle.deadzone;
            if (yawDeadzone != null) yawDeadzone.value = config.yaw.deadzone;
            if (pitchDeadzone != null) pitchDeadzone.value = config.pitch.deadzone;
            if (rollDeadzone != null) rollDeadzone.value = config.roll.deadzone;
        }
        
        // Hide all panels initially
        HideAllPanels();
        
        // Set correct initial state
        UpdateStep();
    }
    
    void BindEvents()
    {
        // Slider value changed events
        if (throttleDeadzone != null)
            throttleDeadzone.RegisterValueChangedCallback(evt => {
                if (config != null) config.throttle.deadzone = evt.newValue;
                if (throttleDeadzoneValue != null) throttleDeadzoneValue.text = evt.newValue.ToString("F2");
            });
            
        if (yawDeadzone != null)
            yawDeadzone.RegisterValueChangedCallback(evt => {
                if (config != null) config.yaw.deadzone = evt.newValue;
                if (yawDeadzoneValue != null) yawDeadzoneValue.text = evt.newValue.ToString("F2");
            });
            
        if (pitchDeadzone != null)
            pitchDeadzone.RegisterValueChangedCallback(evt => {
                if (config != null) config.pitch.deadzone = evt.newValue;
                if (pitchDeadzoneValue != null) pitchDeadzoneValue.text = evt.newValue.ToString("F2");
            });
            
        if (rollDeadzone != null)
            rollDeadzone.RegisterValueChangedCallback(evt => {
                if (config != null) config.roll.deadzone = evt.newValue;
                if (rollDeadzoneValue != null) rollDeadzoneValue.text = evt.newValue.ToString("F2");
            });
            
        // Toggle change events
        if (throttleInvert != null)
            throttleInvert.RegisterValueChangedCallback(evt => {
                if (config != null) config.throttle.invert = evt.newValue;
            });
            
        if (yawInvert != null)
            yawInvert.RegisterValueChangedCallback(evt => {
                if (config != null) config.yaw.invert = evt.newValue;
            });
            
        if (pitchInvert != null)
            pitchInvert.RegisterValueChangedCallback(evt => {
                if (config != null) config.pitch.invert = evt.newValue;
            });
            
        if (rollInvert != null)
            rollInvert.RegisterValueChangedCallback(evt => {
                if (config != null) config.roll.invert = evt.newValue;
            });
            
        // Navigation buttons
        if (prevButton != null)
            prevButton.clicked += OnPreviousClicked;
            
        if (testButton != null)
            testButton.clicked += OnTestClicked;
            
        if (nextButton != null)
            nextButton.clicked += OnNextClicked;
    }
    
    void Update()
    {
        UpdateLivePreview();
        
        // Handle detection timeout
        if (waitingForInput)
        {
            timeoutTimer += Time.deltaTime;
            if (timeoutTimer >= detectionTimeout)
            {
                Debug.LogWarning($"Timeout waiting for {currentStep} input");
                waitingForInput = false;
            }
        }
    }
    
    void UpdateLivePreview()
    {
        if (rcBridge == null || !rcBridge.IsConnected) return;
        
        // Get channel values (0.0-1.0 range)
        float throttle = GetAxisValue(rcBridge, config?.throttle.rcChannel ?? 0);
        float yaw = GetAxisValue(rcBridge, config?.yaw.rcChannel ?? 1);
        float pitch = GetAxisValue(rcBridge, config?.pitch.rcChannel ?? 2);
        float roll = GetAxisValue(rcBridge, config?.roll.rcChannel ?? 3);
        
        // Update visual bars
        UpdateAxisBar(throttleBar, throttleFill, throttleValue, throttle);
        UpdateAxisBar(yawBar, yawFill, yawValue, yaw);
        UpdateAxisBar(pitchBar, pitchFill, pitchValue, pitch);
        UpdateAxisBar(rollBar, rollFill, rollValue, roll);
    }
    
    float GetAxisValue(RCInputBridge bridge, int channel)
    {
        ushort rawValue = bridge.GetChannelValue(channel);
        // Convert 1000-2000 range to 0.0-1.0
        return Mathf.InverseLerp(1000, 2000, rawValue);
    }
    
    void UpdateAxisBar(VisualElement bar, VisualElement fill, Label valueLabel, float value)
    {
        if (bar == null || fill == null || valueLabel == null) return;
        float displayValue = value;
        
        // Apply deadzone
        if (config != null)
        {
            float deadzone = GetDeadzoneForStep(currentStep);
            float sign = displayValue >= 0.5f ? 1f : -1f;
            float centered = (displayValue - 0.5f) * 2f; // -1 to 1
            float absCentered = Mathf.Abs(centered);
            
            if (absCentered < deadzone)
            {
                displayValue = 0.5f;
            }
            else
            {
                // Apply deadzone scaling
                float scaled = (absCentered - deadzone) / (1f - deadzone);
                displayValue = 0.5f + (sign * scaled * 0.5f);
            }
        }
        
        // Update fill width (convert 0-1 to 0-100%)
        float fillPercent = displayValue * 100f;
        fill.style.width = new StyleLength(new Length(fillPercent, LengthUnit.Percent));
        
        // Update value text
        valueLabel.text = $"{(displayValue * 100):F0}%";
    }
    
    float GetDeadzoneForStep(SetupStep step)
    {
        if (config == null) return 0.1f;
        
        switch (step)
        {
            case SetupStep.Throttle: return config.throttle.deadzone;
            case SetupStep.Yaw: return config.yaw.deadzone;
            case SetupStep.Pitch: return config.pitch.deadzone;
            case SetupStep.Roll: return config.roll.deadzone;
            default: return 0.1f;
        }
    }
    
    void OnNextClicked()
    {
        // If waiting for input, don't allow advancing
        if (waitingForInput)
        {
            Debug.Log("Still waiting for input detection");
            return;
        }
        
        // Mark current step as completed
        stepCompleted[(int)currentStep] = true;
        
        // Advance to next step
        currentStep = (SetupStep)(((int)currentStep + 1) % 4);
        UpdateStep();
    }
    
    void OnPreviousClicked()
    {
        if (waitingForInput)
        {
            Debug.Log("Still waiting for input detection");
            return;
        }
        
        // Go to previous step
        currentStep = (SetupStep)(((int)currentStep - 1 + 4) % 4);
        UpdateStep();
    }
    
    void OnTestClicked()
    {
        Debug.Log("Test mode: Toggle controller visualization");
    }
    
    void UpdateStep()
    {
        // Update subtitle and instructions
        if (wizardSubtitle != null)
            wizardSubtitle.text = $"Step {(int)currentStep + 1} of 4: {stepTitles[(int)currentStep]}";
            
        if (instructionText != null)
            instructionText.text = stepInstructions[(int)currentStep];
            
        // Update progress indicators
        UpdateProgressIndicators();
        
        // Show/hide appropriate panels
        HideAllPanels();
        
        switch (currentStep)
        {
            case SetupStep.Throttle:
                throttleBar?.RemoveFromClassList("hidden");
                throttleSettings?.RemoveFromClassList("hidden");
                break;
                
            case SetupStep.Yaw:
                yawBar?.RemoveFromClassList("hidden");
                yawSettings?.RemoveFromClassList("hidden");
                break;
                
            case SetupStep.Pitch:
                pitchBar?.RemoveFromClassList("hidden");
                pitchSettings?.RemoveFromClassList("hidden");
                break;
                
            case SetupStep.Roll:
                rollBar?.RemoveFromClassList("hidden");
                rollSettings?.RemoveFromClassList("hidden");
                break;
        }
        
        // Update navigation button states
        UpdateNavigationButtons();
        
        // Start waiting for input if in Throttle step
        if (currentStep == SetupStep.Throttle)
        {
            StartCoroutine(DetectAxisMovement());
        }
    }
    
    void UpdateProgressIndicators()
    {
        // Update step numbers and labels
        for (int i = 0; i < 4; i++)
        {
            Label numberLabel = root?.Q<Label>($"step-number{i + 1}");
            Label label = root?.Q<Label>($"step-label{i + 1}");
            
            if (i == (int)currentStep)
            {
                numberLabel?.AddToClassList("active");
                label?.AddToClassList("active");
            }
            else
            {
                numberLabel?.RemoveFromClassList("active");
                label?.RemoveFromClassList("active");
            }
        }
    }
    
    void UpdateNavigationButtons()
    {
        // Update button visibility and text
        if (prevButton != null)
        {
            prevButton.text = "Previous";
            prevButton.SetEnabled(currentStep > SetupStep.Throttle); // Disable on first step
        }
        
        if (nextButton != null)
        {
            nextButton.text = ((int)currentStep == 3) ? "Finish" : "Next";
        }
        
        // Enable test button only after throttle calibration
        if (testButton != null)
        {
            testButton.SetEnabled(stepCompleted[0]); // Only after Throttle step
        }
    }
    
    void HideAllPanels()
    {
        // Hide all axis bars
        throttleBar?.AddToClassList("hidden");
        yawBar?.AddToClassList("hidden");
        pitchBar?.AddToClassList("hidden");
        rollBar?.AddToClassList("hidden");
        
        // Hide all settings panels
        throttleSettings?.AddToClassList("hidden");
        yawSettings?.AddToClassList("hidden");
        pitchSettings?.AddToClassList("hidden");
        rollSettings?.AddToClassList("hidden");
    }
    
    IEnumerator DetectAxisMovement()
    {
        waitingForInput = true;
        timeoutTimer = 0f;
        
        Debug.Log($"Detecting {currentStep} movement. Move your stick now.");
        
        float lastValue = 0f;
        float movementDetected = 0f;
        
        while (waitingForInput && timeoutTimer < detectionTimeout)
        {
            float currentValue = GetAxisValue(rcBridge, (int)currentStep);
            float delta = Mathf.Abs(currentValue - lastValue);
            
            if (delta > calibrationThreshold)
            {
                movementDetected += delta * Time.deltaTime;
                
                // Require sustained movement to avoid false positives
                if (movementDetected > 0.3f)
                {
                    Debug.Log($"{currentStep} axis detected!");
                    waitingForInput = false;
                    
                    // Automatically advance if throttle is detected
                    if (currentStep == SetupStep.Throttle)
                    {
                        OnNextClicked();
                    }
                    
                    yield break;
                }
            }
            
            lastValue = currentValue;
            yield return null;
        }
        
        if (waitingForInput)
        {
            Debug.LogWarning($"Failed to detect {currentStep} movement within timeout");
            instructionText.text = $"No movement detected. {stepInstructions[(int)currentStep]}";
        }
        
        waitingForInput = false;
    }
    
    // Public method to show the controller setup
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    // Public method to hide the controller setup
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    void OnDisable()
    {
        // Unbind events
        if (throttleDeadzone != null)
            throttleDeadzone.UnregisterValueChangedCallback(_ => {});
            
        if (yawDeadzone != null)
            yawDeadzone.UnregisterValueChangedCallback(_ => {});
            
        if (pitchDeadzone != null)
            pitchDeadzone.UnregisterValueChangedCallback(_ => {});
            
        if (rollDeadzone != null)
            rollDeadzone.UnregisterValueChangedCallback(_ => {});
            
        if (throttleInvert != null)
            throttleInvert.UnregisterValueChangedCallback(_ => {});
            
        if (yawInvert != null)
            yawInvert.UnregisterValueChangedCallback(_ => {});
            
        if (pitchInvert != null)
            pitchInvert.UnregisterValueChangedCallback(_ => {});
            
        if (rollInvert != null)
            rollInvert.UnregisterValueChangedCallback(_ => {});
            
        if (prevButton != null)
            prevButton.clicked -= OnPreviousClicked;
            
        if (testButton != null)
            testButton.clicked -= OnTestClicked;
            
        if (nextButton != null)
            nextButton.clicked -= OnNextClicked;
    }
}