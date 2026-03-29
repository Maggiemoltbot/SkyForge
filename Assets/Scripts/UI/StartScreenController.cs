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

    // Map-Namen aus dem PROJECT_CONTEXT
die 13 Karten
    private string[] mapNames = {
        "GS-Map-01", "GS-Map-02", "GS-Map-03", "GS-Map-04", "GS-Map-05",
        "GS-Map-06", "GS-Map-07", "GS-Map-08", "GS-Map-09", "GS-Map-10",
        "GS-Map-11", "GS-Map-12", "GS-Map-13"
    };

    private int selectedMapIndex = -1;

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

    void OnDisable()
    {
        CleanupEventListeners();
    }

    void InitializeUI()
    {
        // Referenzen zu UI-Elementen
        m_MapGrid = m_Root.Q<ScrollView>("map-grid");
        m_FlyButton = m_Root.Q<Button>("fly-button");
        m_ControllerSetupButton = m_Root.Q<Button>("controller-setup-button");
        m_SettingsButton = m_Root.Q<Button>("settings-button");
        m_QuitButton = m_Root.Q<Button>("quit-button");

        // Map-Buttons generieren
        PopulateMapGrid();

        // Fly-Button deaktivieren, bis eine Map ausgewählt wurde
        m_FlyButton.SetEnabled(false);
    }

    void PopulateMapGrid()
    {
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

    void SetupEventListeners()
    {
        m_FlyButton.clicked += OnFlyButtonClicked;
        m_ControllerSetupButton.clicked += OnControllerSetupButtonClicked;
        // m_SettingsButton.clicked += OnSettingsButtonClicked; // Platzhalter
        m_QuitButton.clicked += OnQuitButtonClicked;
    }

    void CleanupEventListeners()
    {
        m_FlyButton.clicked -= OnFlyButtonClicked;
        m_ControllerSetupButton.clicked -= OnControllerSetupButtonClicked;
        // m_SettingsButton.clicked -= OnSettingsButtonClicked;
        m_QuitButton.clicked -= OnQuitButtonClicked;
    }

    void OnMapSelected(string mapName)
    {
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
                selectedMapIndex = mapNames.IndexOf(mapName);
                break;
            }
        }

        // Aktiviere den Fly-Button
        m_FlyButton.SetEnabled(true);
    }

    void OnFlyButtonClicked()
    {
        if (selectedMapIndex >= 0)
        {
            string sceneToLoad = "FlightScene"; // Standard, könnte von mapName oder Index abhängen
            uiManager.LoadScene(sceneToLoad);
        }
    }

    void OnControllerSetupButtonClicked()
    {
        uiManager.ShowControllerSetup();
    }

    void OnSettingsButtonClicked()
    {
        // Platzhalter für zukünftige Einstellungen
        Debug.Log("Settings not implemented yet");
    }

    void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}