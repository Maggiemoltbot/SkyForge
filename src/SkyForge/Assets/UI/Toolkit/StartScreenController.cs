using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

public class StartScreenController : MonoBehaviour
{
    [Header("UI References")]
    public VisualTreeAsset uiAsset;
    public UIDocument uiDocument;
    public ControllerSetupController controllerSetupController;
    public UIManager uiManager;
    
    [Header("Maps Settings")]
    public List<string> mapNames = new List<string>()
    {
        "Bicycle", "Garden", "Stump", "Treehill", "Flowers",
        "DrJohnson", "Truck", "Playroom", "Kitchen", "Room",
        "Counter", "Bonsai", "Train"
    };
    
    [Header("Scene Settings")]
    public string flySceneName = "MainScene";
    
    private Button flyButton;
    private Button controllerSetupButton;
    private Button settingsButton;
    private Button quitButton;
    private VisualElement root;
    private string selectedMap = "";
    
    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
            
        root = uiDocument.rootVisualElement;
        
        // Initialize controllers
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
            
        if (controllerSetupController == null)
            controllerSetupController = FindObjectOfType<ControllerSetupController>();
    }
    
    void OnEnable()
    {
        // Ensure UI Manager exists
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
            
        if (controllerSetupController == null)
            controllerSetupController = FindObjectOfType<ControllerSetupController>();
            
        SetupUI();
        BindEvents();
    }
    
    void SetupUI()
    {
        // Get references to UI elements
        flyButton = root.Q<Button>("fly-button");
        controllerSetupButton = root.Q<Button>("controller-setup-button");
        settingsButton = root.Q<Button>("settings-button");
        quitButton = root.Q<Button>("quit-button");
        
        // Ensure all buttons are found
        if (flyButton == null) Debug.LogError("Fly button not found");
        if (controllerSetupButton == null) Debug.LogError("Controller Setup button not found");
        if (settingsButton == null) Debug.LogError("Settings button not found");
        if (quitButton == null) Debug.LogError("Quit button not found");
    }
    
    void BindEvents()
    {
        if (flyButton != null)
            flyButton.clicked += OnFlyClicked;
            
        if (controllerSetupButton != null)
            controllerSetupButton.clicked += OnControllerSetupClicked;
            
        if (settingsButton != null)
            settingsButton.clicked += OnSettingsClicked;
            
        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
            
        // Set up map buttons
        SetupMapButtons();
    }
    
    void SetupMapButtons()
    {
        var mapsGrid = root.Q<VisualElement>("maps-grid");
        if (mapsGrid == null)
        {
            Debug.LogError("Maps grid not found");
            return;
        }
        
        // Remove existing buttons
        mapsGrid.Clear();
        
        // Create buttons for each map
        foreach (string mapName in mapNames)
        {
            Button mapButton = new Button();
            mapButton.text = mapName;
            mapButton.AddToClassList("map-button");
            mapButton.clicked += () => OnMapSelected(mapName);
            mapsGrid.Add(mapButton);
        }
    }
    
    void OnFlyClicked()
    {
        Debug.Log($"Flying to scene: {flySceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(flySceneName);
    }
    
    void OnControllerSetupClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowScreen("ControllerSetup");
        }
        else if (controllerSetupController != null)
        {
            controllerSetupController.Show();
        }
        else
        {
            Debug.LogError("No controller setup handler available");
        }
    }
    
    void OnSettingsClicked()
    {
        // Placeholder for settings
        Debug.Log("Settings not implemented yet");
    }
    
    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    void OnMapSelected(string mapName)
    {
        selectedMap = mapName;
        Debug.Log($"Map selected: {mapName}");
    }
    
    void OnDisable()
    {
        // Unbind events
        if (flyButton != null)
            flyButton.clicked -= OnFlyClicked;
            
        if (controllerSetupButton != null)
            controllerSetupButton.clicked -= OnControllerSetupClicked;
            
        if (settingsButton != null)
            settingsButton.clicked -= OnSettingsClicked;
            
        if (quitButton != null)
            quitButton.clicked -= OnQuitClicked;
    }
}