using System;
using UnityEngine;
using UnityEngine.UIElements;

public class StartScreenController : MonoBehaviour
{
    private UIDocument m_Document;
    private VisualElement m_Root;
    private ScrollView m_MapGrid;
    private Button m_FlyButton;
    private Button m_ControllerSetupButton;
    private Button m_SettingsButton;
    private Button m_QuitButton;

    public UIManager uiManager;

    // Map-Namen aus dem PROJECT_CONTEXT – die 13 Karten
    private readonly string[] mapNames = {
        "GS-Map-01", "GS-Map-02", "GS-Map-03", "GS-Map-04", "GS-Map-05",
        "GS-Map-06", "GS-Map-07", "GS-Map-08", "GS-Map-09", "GS-Map-10",
        "GS-Map-11", "GS-Map-12", "GS-Map-13"
    };

    private int selectedMapIndex = -1;
    private bool uiInitialized;

    void Awake()
    {
        m_Document = GetComponent<UIDocument>();
        if (m_Document == null)
        {
            Debug.LogError("StartScreenController requires a UIDocument component.");
            enabled = false;
            return;
        }

        m_Root = m_Document.rootVisualElement;
        if (m_Root == null)
        {
            Debug.LogError("StartScreenController could not access rootVisualElement on UIDocument.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (!InitializeUI())
        {
            return;
        }

        SetupEventListeners();
    }

    void OnDisable()
    {
        if (!uiInitialized)
        {
            return;
        }

        CleanupEventListeners();
    }

    private bool InitializeUI()
    {
        if (m_Root == null)
        {
            return false;
        }

        // Referenzen zu UI-Elementen
        m_MapGrid = m_Root.Q<ScrollView>("map-grid");
        m_FlyButton = m_Root.Q<Button>("fly-button");
        m_ControllerSetupButton = m_Root.Q<Button>("controller-setup-button");
        m_SettingsButton = m_Root.Q<Button>("settings-button");
        m_QuitButton = m_Root.Q<Button>("quit-button");

        if (m_MapGrid == null || m_FlyButton == null || m_ControllerSetupButton == null || m_QuitButton == null)
        {
            Debug.LogError("StartScreenController could not locate required UI elements. Check UXML ids.");
            uiInitialized = false;
            return false;
        }

        // Map-Buttons generieren
        PopulateMapGrid();

        // Fly-Button deaktivieren, bis eine Map ausgewählt wurde
        m_FlyButton.SetEnabled(false);
        uiInitialized = true;
        return true;
    }

    private void PopulateMapGrid()
    {
        if (m_MapGrid == null)
        {
            return;
        }

        m_MapGrid.Clear();

        foreach (string mapName in mapNames)
        {
            Button mapButton = new Button(() => OnMapSelected(mapName))
            {
                text = mapName
            };
            m_MapGrid.Add(mapButton);
        }
    }

    private void SetupEventListeners()
    {
        if (!uiInitialized)
        {
            return;
        }

        if (m_FlyButton != null)
            m_FlyButton.clicked += OnFlyButtonClicked;

        if (m_ControllerSetupButton != null)
            m_ControllerSetupButton.clicked += OnControllerSetupButtonClicked;

        if (m_SettingsButton != null)
            m_SettingsButton.clicked += OnSettingsButtonClicked;

        if (m_QuitButton != null)
            m_QuitButton.clicked += OnQuitButtonClicked;
    }

    private void CleanupEventListeners()
    {
        if (m_FlyButton != null)
            m_FlyButton.clicked -= OnFlyButtonClicked;

        if (m_ControllerSetupButton != null)
            m_ControllerSetupButton.clicked -= OnControllerSetupButtonClicked;

        if (m_SettingsButton != null)
            m_SettingsButton.clicked -= OnSettingsButtonClicked;

        if (m_QuitButton != null)
            m_QuitButton.clicked -= OnQuitButtonClicked;
    }

    private void OnMapSelected(string mapName)
    {
        if (m_MapGrid == null || m_FlyButton == null)
        {
            return;
        }

        // Markiere alle Buttons als nicht ausgewählt
        foreach (VisualElement child in m_MapGrid.Children())
        {
            if (child is Button button)
            {
                button.RemoveFromClassList("selected");
            }
        }

        // Finde den ausgewählten Button und markiere ihn
        foreach (VisualElement child in m_MapGrid.Children())
        {
            if (child is Button button && button.text == mapName)
            {
                button.AddToClassList("selected");
                selectedMapIndex = Array.IndexOf(mapNames, mapName);
                break;
            }
        }

        // Aktiviere den Fly-Button
        m_FlyButton.SetEnabled(true);
    }

    private void OnFlyButtonClicked()
    {
        if (selectedMapIndex < 0)
        {
            return;
        }

        if (uiManager == null)
        {
            Debug.LogWarning("UIManager reference missing on StartScreenController. Cannot load scene.");
            return;
        }

        string sceneToLoad = "FlightScene"; // Standard, könnte von mapName oder Index abhängen
        uiManager.LoadScene(sceneToLoad);
    }

    private void OnControllerSetupButtonClicked()
    {
        if (uiManager == null)
        {
            Debug.LogWarning("UIManager reference missing on StartScreenController. Cannot open controller setup.");
            return;
        }

        uiManager.ShowControllerSetup();
    }

    private void OnSettingsButtonClicked()
    {
        // Platzhalter für zukünftige Einstellungen
        Debug.Log("Settings not implemented yet");
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
