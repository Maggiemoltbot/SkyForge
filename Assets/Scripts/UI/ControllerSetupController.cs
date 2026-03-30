using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class ControllerSetupController : MonoBehaviour
{
    private UIDocument m_Document;
    private VisualElement m_Root;
    
    private enum SetupStep
    {
        Throttle,
        Yaw,
        Pitch,
        Roll
    }
    
    private SetupStep currentStep = SetupStep.Throttle;
    
    // UI Referenzen
    private VisualElement[] stepElements;
    private Label instructionText;
    private VisualElement progressFill;
    private Label pwmValueLabel;
    private Toggle invertToggle;
    private Slider deadzoneSlider;
    private Label deadzoneLabel;
    private Button testButton;
    private Button previousButton;
    private Button nextButton;
    
    // Mock Referenz für die Config (In der Realität wäre dies ein Asset)
    // private ControllerConfig config; // ZB: ScriptableObject
    
    void Awake()
    {
        m_Document = GetComponent<UIDocument>();
        m_Root = m_Document.rootVisualElement;
    }

    void OnEnable()
    {
        InitializeUI();
        SetupEventListeners();
    }

    void Update()
    {
        UpdateLivePreview();
    }

    void InitializeUI()
    {
        // Step-Referenzen
        var steps = m_Root.Query(".step").Children<VisualElement>(".step");
        stepElements = steps.Take(4).ToArray();
        stepElements[0].AddToClassList("active");
        // steps.ElementAt(0).AddToClassList("active");

        // Andere Referenzen
        instructionText = m_Root.Q<Label>("instruction-text");
        progressFill = m_Root.Q<VisualElement>("progress-fill");
        pwmValueLabel = m_Root.Q<Label>("pwm-value");
        invertToggle = m_Root.Q<Toggle>("invert-toggle");
        deadzoneSlider = m_Root.Q<Slider>("deadzone-slider");
        deadzoneLabel = m_Root.Q<Label>("deadzone-label");
        testButton = m_Root.Q<Button>("test-button");
        previousButton = m_Root.Q<Button>("previous-button");
        nextButton = m_Root.Q<Button>("next-button");

        // Aktualisiere Starttext
        UpdateInstructionText();
        UpdateDeadzoneLabel();
    }
    
    void SetupEventListeners()
    {
        deadzoneSlider?.RegisterValueChangedCallback(evt =>
        {
            // Update Label
            UpdateDeadzoneLabel();
            
            // Update Config
            // config.deadzone = evt.newValue;
            // EditorUtility.SetDirty(config); // Wenn ScriptableObject im Editor
            Debug.Log($"Deadzone changed to: {evt.newValue}");
        });
        
        invertToggle?.RegisterValueChangedCallback(evt =>
        {
            // Update Config
            // config.SetInvert(currentStep.ToString(), evt.newValue);
            // EditorUtility.SetDirty(config);
            string axis = currentStep.ToString();
            Debug.Log($"Invert {axis} set to: {evt.newValue}");
        });
        
        previousButton?.clicked += () =>
        {
            if (currentStep > SetupStep.Throttle)
            {
                currentStep--;
                UpdateStepUI();
            }
        };
        
        nextButton?.clicked += () =>
        {
            if (currentStep < SetupStep.Roll)
            {
                currentStep++;
                UpdateStepUI();
            }
            else
            {
                // Finish Wizard
                Debug.Log("Controller Setup Complete!");
                // UIManager.Instance.HideControllerSetup(); // Beispiel
            }
        };
        
        testButton?.clicked += () =>
        {
            Debug.Log("Test button clicked");
            // Implementiere Testlogik
        };
    }
    
    void UpdateStepUI()
    {
        // Aktualisiere Step-Visualisierung
        for (int i = 0; i < stepElements.Length; i++)
        {
            stepElements[i].RemoveFromClassList("active");
            stepElements[i].RemoveFromClassList("completed");
        }
        
        stepElements[(int)currentStep].AddToClassList("active");
        
        // Markiere vorherige Schritte als "completed"
        for (int i = 0; i < (int)currentStep; i++)
        {
            stepElements[i].AddToClassList("completed");
        }
        
        // Update Anweisungstext
        UpdateInstructionText();
        
        // Reset Live Preview
        if (progressFill != null) progressFill.style.width = new StyleLength(Length.Percent(0));
        if (pwmValueLabel != null) pwmValueLabel.text = "0%";
    }
    
    void UpdateInstructionText()
    {
        string[] instructions = {
            "Move your Throttle stick up and down.",
            "Move your Yaw stick left and right.",
            "Move your Pitch stick forward and backward.",
            "Move your Roll stick left and right."
        };
        
        if (instructionText != null)
        {
            instructionText.text = instructions[(int)currentStep];
        }
    }
    
    void UpdateDeadzoneLabel()
    {
        if (deadzoneLabel != null && deadzoneSlider != null)
        {
            deadzoneLabel.text = deadzoneSlider.value.ToString("F2");
        }
    }
    
    void UpdateLivePreview()
    {
        // Simuliere PWM-Wert. In Wirklichkeit liest man dies vom Input-System.
        // float fakePWM = Mathf.PingPong(Time.time * 0.5f, 1.0f); 
        float fakePWM = GetMockAxisValue();
        
        float percent = Mathf.Clamp01(fakePWM) * 100;
        
        if (progressFill != null)
        {
            progressFill.style.width = new StyleLength(Length.Percent(percent));
        }
        
        if (pwmValueLabel != null)
        {
            pwmValueLabel.text = $"{percent:F0}%";
        }
    }
    
    float GetMockAxisValue()
    {
        // Mock für differente Achsen
        switch (currentStep)
        {
            case SetupStep.Throttle:
                // Throttle: 0% (unten) bis 100% (oben)
                return Mathf.InverseLerp(-1, 1, Input.GetAxis("Throttle")); // Annahme eines virtuellen Achsen
            case SetupStep.Yaw:
                // Yaw: -100% (links) bis +100% (rechts)
                return Mathf.Abs(Input.GetAxis("Yaw"));
            case SetupStep.Pitch:
                // Pitch: -100% (vorne) bis +100% (hinten)
                return Mathf.Abs(Input.GetAxis("Pitch"));
            case SetupStep.Roll:
                // Roll: -100% (links) bis +100% (rechts)
                return Mathf.Abs(Input.GetAxis("Roll"));
            default:
                return 0;
        }
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
