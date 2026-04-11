using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Screens")]
    public GameObject startScreen;
    public GameObject controllerSetup;
    public GameObject hud;

    private VisualElement startScreenRoot;
    private VisualElement controllerSetupRoot;
    private VisualElement hudRoot;

    private UIDocument startScreenDoc;
    private UIDocument controllerSetupDoc;
    private UIDocument hudDoc;

    public Button flyButton;
    public Button controllerSetupButton;
    public Button settingsButton;
    public Button quitButton;
    public Button testButton;
    public Button previousButton;
    public Button nextButton;

    // Singleton und Persistenz
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeComponents();
        SetupEventListeners();
    }

    private void InitializeComponents()
    {
        startScreenDoc = startScreen.GetComponent<UIDocument>();
        controllerSetupDoc = controllerSetup.GetComponent<UIDocument>();
        hudDoc = hud.GetComponent<UIDocument>();

        EnsurePanelSettings("StartScreen", startScreenDoc);
        EnsurePanelSettings("ControllerSetup", controllerSetupDoc);
        EnsurePanelSettings("HUD", hudDoc);

        startScreenRoot = startScreenDoc != null ? startScreenDoc.rootVisualElement : null;
        controllerSetupRoot = controllerSetupDoc != null ? controllerSetupDoc.rootVisualElement : null;
        hudRoot = hudDoc != null ? hudDoc.rootVisualElement : null;
    }

    private void EnsurePanelSettings(string ownerLabel, UIDocument document)
    {
        if (document == null)
        {
            Debug.LogError($"UIManager: UIDocument missing on {ownerLabel} object.", this);
            return;
        }

        UIPanelSettingsUtility.Ensure(document, this, ownerLabel);
    }

    private void SetupEventListeners()
    {
        // StartScreen Buttons
        if (startScreenRoot != null)
        {
            flyButton = startScreenRoot.Q<Button>("fly-button");
            controllerSetupButton = startScreenRoot.Q<Button>("controller-setup-button");
            settingsButton = startScreenRoot.Q<Button>("settings-button");
            quitButton = startScreenRoot.Q<Button>("quit-button");

            if (flyButton != null)
            {
                flyButton.clicked += HideStartScreenAndStartFlight;
            }

            if (controllerSetupButton != null)
            {
                controllerSetupButton.clicked += ShowControllerSetup;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked += () => Debug.Log("Settings clicked");
            }

            if (quitButton != null)
            {
                quitButton.clicked += () => Application.Quit();
            }
        }

        // ControllerSetup Buttons
        if (controllerSetupRoot != null)
        {
            testButton = controllerSetupRoot.Q<Button>("test-button");
            previousButton = controllerSetupRoot.Q<Button>("previous-button");
            nextButton = controllerSetupRoot.Q<Button>("next-button");

            if (testButton != null)
            {
                testButton.clicked += () => Debug.Log("Test mode activated");
            }

            if (previousButton != null)
            {
                previousButton.clicked += () => Debug.Log("Previous step");
            }

            if (nextButton != null)
            {
                nextButton.clicked += () => Debug.Log("Next step");
            }
        }
    }

    private void Update()
    {
        // Globaler F4-Toggle für Controller Setup
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ToggleControllerSetup();
        }
    }

    public void ShowStartScreen()
    {
        Time.timeScale = 0;
        startScreen.SetActive(true);
    }

    public void HideStartScreen()
    {
        Time.timeScale = 1;
        startScreen.SetActive(false);
    }

    public void ShowControllerSetup()
    {
        controllerSetup.SetActive(true);
    }

    public void HideControllerSetup()
    {
        controllerSetup.SetActive(false);
    }

    public void ToggleControllerSetup()
    {
        controllerSetup.SetActive(!controllerSetup.activeSelf);
    }

    public void ShowHUD()
    {
        hud.SetActive(true);
    }

    public void HideHUD()
    {
        hud.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        // Hier die Logik zum Laden der Szene einfügen
        Debug.Log($"Loading scene: {sceneName}");
        // SceneManager.LoadScene(sceneName);
        SaveControllerConfiguration();
    }

    private void SaveControllerConfiguration()
    {
        // Temporärer Platzhalter. Hier muss die Logik zum Speichern der Controller-Konfiguration rein.
        Debug.Log("Saving Controller Configuration...");
    }
}