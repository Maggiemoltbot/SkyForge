using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance;
    
    // UI Document references
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private UIDocument controllerSetupDocument;
    [SerializeField] private UIDocument mapSelectionDocument;
    [SerializeField] private UIDocument aboutDocument;
    
    // Component references
    private MainMenu mainMenu;
    private ControllerSetup controllerSetup;
    private MapSelection mapSelection;
    private About about;
    
    // Screen management
    private string currentScreen = "";
    private Stack<string> screenHistory = new Stack<string>();
    
    // Transition effects
    [SerializeField] private float transitionDuration = 0.3f;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeScreens();
        ShowScreen("MainMenu");
    }
    
    private void InitializeScreens()
    {
        // Initialize Main Menu
        if (mainMenuDocument != null)
        {
            mainMenu = mainMenuDocument.GetComponent<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.onFly += OnFlyClicked;
                mainMenu.onControllerSetup += OnControllerSetupClicked;
                mainMenu.onMapSelect += OnMapSelectClicked;
                mainMenu.onSettings += OnSettingsClicked;
                mainMenu.onAbout += OnAboutClicked;
                mainMenu.onQuit += OnQuitClicked;
            }
        }
        
        // Initialize Controller Setup
        if (controllerSetupDocument != null)
        {
            controllerSetup = controllerSetupDocument.GetComponent<ControllerSetup>();
            // Controller setup events would be handled here
        }
        
        // Initialize Map Selection
        if (mapSelectionDocument != null)
        {
            mapSelection = mapSelectionDocument.GetComponent<MapSelection>();
            if (mapSelection != null)
            {
                mapSelection.onMapSelected += OnMapSelected;
                mapSelection.onBackToMainMenu += OnBackToMainMenuFromMapSelection;
            }
        }
        
        // Initialize About
        if (aboutDocument != null)
        {
            about = aboutDocument.GetComponent<About>();
            if (about != null)
            {
                about.onBack += OnBackToMainMenuFromAbout;
                about.onWebsiteClicked += OnWebsiteClicked;
                about.onGitHubClicked += OnGitHubClicked;
            }
        }
    }
    
    public void ShowScreen(string screenName)
    {
        // Hide current screen
        HideCurrentScreen();
        
        // Push current screen to history if valid
        if (!string.IsNullOrEmpty(currentScreen))
        {
            screenHistory.Push(currentScreen);
        }
        
        // Show new screen
        switch (screenName)
        {
            case "MainMenu":
                ShowMainMenu();
                break;
            case "ControllerSetup":
                ShowControllerSetup();
                break;
            case "MapSelection":
                ShowMapSelection();
                break;
            case "About":
                ShowAbout();
                break;
        }
        
        currentScreen = screenName;
    }
    
    private void HideCurrentScreen()
    {
        switch (currentScreen)
        {
            case "MainMenu":
                if (mainMenuDocument != null)
                    mainMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
                break;
            case "ControllerSetup":
                if (controllerSetupDocument != null)
                    controllerSetupDocument.rootVisualElement.style.display = DisplayStyle.None;
                break;
            case "MapSelection":
                if (mapSelectionDocument != null)
                    mapSelectionDocument.rootVisualElement.style.display = DisplayStyle.None;
                break;
            case "About":
                if (aboutDocument != null)
                    aboutDocument.rootVisualElement.style.display = DisplayStyle.None;
                break;
        }
    }
    
    private void ShowMainMenu()
    {
        if (mainMenuDocument != null)
        {
            mainMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // Apply fade in effect
            StartCoroutine(FadeIn(mainMenuDocument.rootVisualElement));
        }
    }
    
    private void ShowControllerSetup()
    {
        if (controllerSetupDocument != null)
        {
            controllerSetupDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // Apply fade in effect
            StartCoroutine(FadeIn(controllerSetupDocument.rootVisualElement));
        }
    }
    
    private void ShowMapSelection()
    {
        if (mapSelectionDocument != null)
        {
            mapSelectionDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // Apply fade in effect
            StartCoroutine(FadeIn(mapSelectionDocument.rootVisualElement));
        }
    }
    
    private void ShowAbout()
    {
        if (aboutDocument != null)
        {
            aboutDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // Apply fade in effect
            StartCoroutine(FadeIn(aboutDocument.rootVisualElement));
        }
    }
    
    public void GoBack()
    {
        if (screenHistory.Count > 0)
        {
            string previousScreen = screenHistory.Pop();
            ShowScreen(previousScreen);
        }
        else
        {
            // If no history, go to main menu
            ShowScreen("MainMenu");
        }
    }
    
    // Event Handlers
    private void OnFlyClicked()
    {
        // Start the game
        Debug.Log("Starting game...");
        // In a real implementation, this would load the game scene
        // SceneManager.LoadScene("GameScene");
    }
    
    private void OnControllerSetupClicked()
    {
        ShowScreen("ControllerSetup");
    }
    
    private void OnMapSelectClicked()
    {
        ShowScreen("MapSelection");
    }
    
    private void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
        // Implement settings screen
    }
    
    private void OnAboutClicked()
    {
        ShowScreen("About");
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void OnMapSelected(MapData map)
    {
        Debug.Log($"Map selected: {map.name}");
        // In a real implementation, this would load the selected map/scene
        // SceneManager.LoadScene(map.scenePath);
    }
    
    private void OnBackToMainMenuFromMapSelection()
    {
        ShowScreen("MainMenu");
    }
    
    private void OnBackToMainMenuFromAbout()
    {
        ShowScreen("MainMenu");
    }
    
    private void OnWebsiteClicked()
    {
        Debug.Log("Opening website...");
        // In a real implementation, this would open the website
        // Application.OpenURL("https://xflight-aircrafts.com");
    }
    
    private void OnGitHubClicked()
    {
        Debug.Log("Opening GitHub...");
        // In a real implementation, this would open the GitHub repository
        // Application.OpenURL("https://github.com/xflight");
    }
    
    // Coroutine for fade in effect
    private IEnumerator FadeIn(VisualElement element)
    {
        element.style.opacity = 0;
        
        float elapsedTime = 0;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float opacity = Mathf.Clamp01(elapsedTime / transitionDuration);
            element.style.opacity = opacity;
            yield return null;
        }
        
        element.style.opacity = 1;
    }
    
    // Handle escape key for navigation
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't go back from main menu or when in flight
            if (currentScreen != "MainMenu" && currentScreen != "")
            {
                GoBack();
            }
            else if (currentScreen == "MainMenu")
            {
                // Confirm quit on main menu
                OnQuitClicked();
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (mainMenu != null)
        {
            mainMenu.onFly -= OnFlyClicked;
            mainMenu.onControllerSetup -= OnControllerSetupClicked;
            mainMenu.onMapSelect -= OnMapSelectClicked;
            mainMenu.onSettings -= OnSettingsClicked;
            mainMenu.onAbout -= OnAboutClicked;
            mainMenu.onQuit -= OnQuitClicked;
        }
        
        if (mapSelection != null)
        {
            mapSelection.onMapSelected -= OnMapSelected;
            mapSelection.onBackToMainMenu -= OnBackToMainMenuFromMapSelection;
        }
        
        if (about != null)
        {
            about.onBack -= OnBackToMainMenuFromAbout;
            about.onWebsiteClicked -= OnWebsiteClicked;
            about.onGitHubClicked -= OnGitHubClicked;
        }
    }
}