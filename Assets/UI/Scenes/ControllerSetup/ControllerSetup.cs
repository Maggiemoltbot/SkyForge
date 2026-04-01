using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ControllerSetup : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset controllerSetupUxml;
    
    private VisualElement root;
    
    // UI References
    private Label titleLabel;
    private VisualElement stepIndicator;
    private VisualElement wizardContent;
    private VisualElement[] stepContents;
    private Button backButton;
    private Button nextButton;
    private Button saveButton;
    private Button loadButton;
    private Button detectButton;
    
    // Wizard State
    private int currentStep = 0;
    private const int totalSteps = 4;
    
    // Stick Preview Elements
    private VisualElement leftStickThumb;
    private VisualElement rightStickThumb;
    
    void OnEnable()
    {
        InitializeUI();
        RegisterEventHandlers();
        UpdateStepVisibility();
        UpdateStepIndicator();
    }
    
    private void InitializeUI()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        if (controllerSetupUxml != null)
        {
            root.Clear();
            TemplateContainer template = controllerSetupUxml.Instantiate();
            root.Add(template);
            
            // Get references to UI elements
            titleLabel = root.Q<Label>("title-label");
            stepIndicator = root.Q<VisualElement>("step-indicator");
            wizardContent = root.Q<VisualElement>("wizard-content");
            
            // Get step content elements
            stepContents = new VisualElement[totalSteps];
            stepContents[0] = root.Q<VisualElement>("step1-content");
            stepContents[1] = root.Q<VisualElement>("step2-content");
            stepContents[2] = root.Q<VisualElement>("step3-content");
            stepContents[3] = root.Q<VisualElement>("step4-content");
            
            // Navigation buttons
            backButton = root.Q<Button>("back-button");
            nextButton = root.Q<Button>("next-button");
            
            // Save/Load buttons
            saveButton = root.Q<Button>("save-button");
            loadButton = root.Q<Button>("load-button");
            
            // Detect button
            detectButton = root.Q<Button>("detect-button");
            
            // Stick preview elements
            leftStickThumb = root.Q<VisualElement>("left-stick-thumb");
            rightStickThumb = root.Q<VisualElement>("right-stick-thumb");
        }
    }
    
    private void RegisterEventHandlers()
    {
        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;
            
        if (nextButton != null)
            nextButton.clicked += OnNextButtonClicked;
            
        if (saveButton != null)
            saveButton.clicked += OnSaveButtonClicked;
            
        if (loadButton != null)
            loadButton.clicked += OnLoadButtonClicked;
            
        if (detectButton != null)
            detectButton.clicked += OnDetectButtonClicked;
    }
    
    private void OnBackButtonClicked()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateStepVisibility();
            UpdateStepIndicator();
        }
    }
    
    private void OnNextButtonClicked()
    {
        if (currentStep < totalSteps - 1)
        {
            currentStep++;
            UpdateStepVisibility();
            UpdateStepIndicator();
        }
    }
    
    private void OnSaveButtonClicked()
    {
        Debug.Log("Save profile clicked");
        // Implement save profile logic
    }
    
    private void OnLoadButtonClicked()
    {
        Debug.Log("Load profile clicked");
        // Implement load profile logic
    }
    
    private void OnDetectButtonClicked()
    {
        Debug.Log("Detect controller clicked");
        // Implement controller detection logic
        Label controllerStatus = root.Q<Label>("controller-status");
        if (controllerStatus != null)
        {
            controllerStatus.text = "Controller detected: Generic USB Joystick";
        }
    }
    
    private void UpdateStepVisibility()
    {
        for (int i = 0; i < stepContents.Length; i++)
        {
            if (stepContents[i] != null)
            {
                stepContents[i].style.display = (i == currentStep) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        // Update button states
        if (backButton != null)
            backButton.SetEnabled(currentStep > 0);
            
        if (nextButton != null)
        {
            if (currentStep == totalSteps - 1)
            {
                nextButton.text = "FINISH";
            }
            else
            {
                nextButton.text = "NEXT";
            }
        }
    }
    
    private void UpdateStepIndicator()
    {
        for (int i = 1; i <= totalSteps; i++)
        {
            Label stepLabel = root.Q<Label>($"step{i}");
            if (stepLabel != null)
            {
                if (i == currentStep + 1)
                {
                    stepLabel.style.color = new StyleColor(Color.white);
                    stepLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    stepLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                    stepLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            }
        }
    }
    
    void OnDisable()
    {
        UnregisterEventHandlers();
    }
    
    private void UnregisterEventHandlers()
    {
        if (backButton != null)
            backButton.clicked -= OnBackButtonClicked;
            
        if (nextButton != null)
            nextButton.clicked -= OnNextButtonClicked;
            
        if (saveButton != null)
            saveButton.clicked -= OnSaveButtonClicked;
            
        if (loadButton != null)
            loadButton.clicked -= OnLoadButtonClicked;
            
        if (detectButton != null)
            detectButton.clicked -= OnDetectButtonClicked;
    }
    
    // Method to update stick previews
    public void UpdateStickPreviews(Vector2 leftStick, Vector2 rightStick)
    {
        if (leftStickThumb != null)
        {
            float leftX = Mathf.Lerp(20, 80, (leftStick.x + 1f) / 2f);
            float leftY = Mathf.Lerp(20, 80, (leftStick.y + 1f) / 2f);
            leftStickThumb.style.left = leftX;
            leftStickThumb.style.top = leftY;
        }
        
        if (rightStickThumb != null)
        {
            float rightX = Mathf.Lerp(20, 80, (rightStick.x + 1f) / 2f);
            float rightY = Mathf.Lerp(20, 80, (rightStick.y + 1f) / 2f);
            rightStickThumb.style.left = rightX;
            rightStickThumb.style.top = rightY;
        }
    }
}