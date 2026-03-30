using UnityEngine;
using UnityEngine.UIElements;
using System;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset mainMenuUxml;
    
    private VisualElement root;
    
    // UI References
    private Label titleLabel;
    private Label subtitleLabel;
    private Button flyButton;
    private Button controllerButton;
    private Button mapSelectButton;
    private Button settingsButton;
    private Button aboutButton;
    private Button quitButton;
    
    // Events
    public event Action onFly;
    public event Action onControllerSetup;
    public event Action onMapSelect;
    public event Action onSettings;
    public event Action onAbout;
    public event Action onQuit;
    
    void OnEnable()
    {
        InitializeUI();
        RegisterEventHandlers();
    }
    
    private void InitializeUI()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        if (mainMenuUxml != null)
        {
            root.Clear();
            TemplateContainer template = mainMenuUxml.Instantiate();
            root.Add(template);
            
            // Get references to UI elements
            titleLabel = root.Q<Label>("title-label");
            subtitleLabel = root.Q<Label>("subtitle-label");
            flyButton = root.Q<Button>("fly-button");
            controllerButton = root.Q<Button>("controller-button");
            mapSelectButton = root.Q<Button>("map-select-button");
            settingsButton = root.Q<Button>("settings-button");
            aboutButton = root.Q<Button>("about-button");
            quitButton = root.Q<Button>("quit-button");
        }
    }
    
    private void RegisterEventHandlers()
    {
        if (flyButton != null)
            flyButton.clicked += HandleFlyClicked;
            
        if (controllerButton != null)
            controllerButton.clicked += HandleControllerSetupClicked;
            
        if (mapSelectButton != null)
            mapSelectButton.clicked += HandleMapSelectClicked;
            
        if (settingsButton != null)
            settingsButton.clicked += HandleSettingsClicked;
            
        if (aboutButton != null)
            aboutButton.clicked += HandleAboutClicked;
            
        if (quitButton != null)
            quitButton.clicked += HandleQuitClicked;
    }
    
    void OnDisable()
    {
        UnregisterEventHandlers();
    }
    
    private void UnregisterEventHandlers()
    {
        if (flyButton != null)
            flyButton.clicked -= HandleFlyClicked;
            
        if (controllerButton != null)
            controllerButton.clicked -= HandleControllerSetupClicked;
            
        if (mapSelectButton != null)
            mapSelectButton.clicked -= HandleMapSelectClicked;
            
        if (settingsButton != null)
            settingsButton.clicked -= HandleSettingsClicked;
            
        if (aboutButton != null)
            aboutButton.clicked -= HandleAboutClicked;
            
        if (quitButton != null)
            quitButton.clicked -= HandleQuitClicked;
    }

    private void HandleFlyClicked() => onFly?.Invoke();
    private void HandleControllerSetupClicked() => onControllerSetup?.Invoke();
    private void HandleMapSelectClicked() => onMapSelect?.Invoke();
    private void HandleSettingsClicked() => onSettings?.Invoke();
    private void HandleAboutClicked() => onAbout?.Invoke();
    private void HandleQuitClicked() => onQuit?.Invoke();
}
