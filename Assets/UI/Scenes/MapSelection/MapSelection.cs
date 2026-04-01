using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class MapData
{
    public string name;
    public string scenePath;
    public string thumbnailPath;
    public int splatCount;
    public string type;
}

public class MapSelection : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset mapSelectionUxml;
    
    private VisualElement root;
    
    // UI References
    private Label titleLabel;
    private TextField searchField;
    private ScrollView mapScrollView;
    private VisualElement mapGrid;
    private Label statusLabel;
    private Button backButton;
    
    // Map data
    private List<MapData> allMaps = new List<MapData>();
    private List<MapData> filteredMaps = new List<MapData>();
    private MapData selectedMap;
    
    // Events
    public System.Action<MapData> onMapSelected;
    public System.Action onBackToMainMenu;
    
    void OnEnable()
    {
        InitializeUI();
        RegisterEventHandlers();
        LoadMaps();
    }
    
    private void InitializeUI()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        if (mapSelectionUxml != null)
        {
            root.Clear();
            TemplateContainer template = mapSelectionUxml.Instantiate();
            root.Add(template);
            
            // Get references to UI elements
            titleLabel = root.Q<Label>("title-label");
            searchField = root.Q<TextField>("search-field");
            mapScrollView = root.Q<ScrollView>("map-scroll-view");
            mapGrid = root.Q<VisualElement>("map-grid");
            statusLabel = root.Q<Label>("status-label");
            backButton = root.Q<Button>("back-button");
        }
    }
    
    private void RegisterEventHandlers()
    {
        if (searchField != null)
            searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            
        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;
    }
    
    private void OnSearchValueChanged(ChangeEvent<string> evt)
    {
        FilterMaps(evt.newValue);
        DisplayMaps();
    }
    
    private void OnBackButtonClicked()
    {
        onBackToMainMenu?.Invoke();
    }
    
    private void LoadMaps()
    {
        // In a real implementation, this would scan the GaussianAssets folder
        // For now, we'll create some sample data
        CreateSampleMapData();
        
        // Display the maps
        DisplayMaps();
    }
    
    private void CreateSampleMapData()
    {
        allMaps.Clear();
        
        // Sample map data
        allMaps.Add(new MapData
        {
            name = "Forest Clearing",
            scenePath = "Assets/Scenes/ForestClearing.unity",
            thumbnailPath = "",
            splatCount = 120000,
            type = "Outdoor"
        });
        
        allMaps.Add(new MapData
        {
            name = "Urban Canyon",
            scenePath = "Assets/Scenes/UrbanCanyon.unity",
            thumbnailPath = "",
            splatCount = 95000,
            type = "Outdoor"
        });
        
        allMaps.Add(new MapData
        {
            name = "Warehouse",
            scenePath = "Assets/Scenes/Warehouse.unity",
            thumbnailPath = "",
            splatCount = 75000,
            type = "Indoor"
        });
        
        allMaps.Add(new MapData
        {
            name = "Mountain Pass",
            scenePath = "Assets/Scenes/MountainPass.unity",
            thumbnailPath = "",
            splatCount = 210000,
            type = "Outdoor"
        });
        
        allMaps.Add(new MapData
        {
            name = "Tech Lab",
            scenePath = "Assets/Scenes/TechLab.unity",
            thumbnailPath = "",
            splatCount = 65000,
            type = "Indoor"
        });
        
        allMaps.Add(new MapData
        {
            name = "Desert Ruins",
            scenePath = "Assets/Scenes/DesertRuins.unity",
            thumbnailPath = "",
            splatCount = 150000,
            type = "Outdoor"
        });
        
        filteredMaps = new List<MapData>(allMaps);
    }
    
    private void FilterMaps(string searchText)
    {
        filteredMaps.Clear();
        
        if (string.IsNullOrEmpty(searchText))
        {
            filteredMaps.AddRange(allMaps);
        }
        else
        {
            foreach (var map in allMaps)
            {
                if (map.name.ToLower().Contains(searchText.ToLower()))
                {
                    filteredMaps.Add(map);
                }
            }
        }
    }
    
    private void DisplayMaps()
    {
        if (mapGrid == null) return;
        
        mapGrid.Clear();
        
        if (filteredMaps.Count == 0)
        {
            if (statusLabel != null)
                statusLabel.text = "No maps found";
            return;
        }
        
        foreach (var map in filteredMaps)
        {
            VisualElement mapCard = CreateMapCard(map);
            mapGrid.Add(mapCard);
        }
        
        if (statusLabel != null)
            statusLabel.text = $"{filteredMaps.Count} maps available";
    }
    
    private VisualElement CreateMapCard(MapData map)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("grid-item");
        card.name = $"map-card-{map.name.Replace(" ", "-").ToLower()}";
        
        // Card content
        VisualElement cardContent = new VisualElement();
        cardContent.style.flexDirection = FlexDirection.Column;
        cardContent.style.alignItems = Align.Center;
        
        // Map name
        Label nameLabel = new Label(map.name);
        nameLabel.AddToClassList("label-primary");
        nameLabel.style.fontSize = new StyleLength(16);
        nameLabel.style.marginBottom = 10;
        
        // Map thumbnail placeholder
        VisualElement thumbnail = new VisualElement();
        thumbnail.style.width = 200;
        thumbnail.style.height = 120;
        thumbnail.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.15f));
        thumbnail.style.borderTopLeftRadius = 5;
        thumbnail.style.borderTopRightRadius = 5;
        thumbnail.style.borderBottomLeftRadius = 5;
        thumbnail.style.borderBottomRightRadius = 5;
        thumbnail.style.marginBottom = 10;
        
        // Map info
        Label infoLabel = new Label($"{map.type}, {map.splatCount:N0} splats");
        infoLabel.AddToClassList("label-secondary");
        infoLabel.style.marginBottom = 10;
        
        // Select button
        Button selectButton = new Button(() => OnMapSelected(map));
        selectButton.text = "SELECT";
        selectButton.AddToClassList("button");
        selectButton.style.width = 150;
        
        // Add elements to card
        cardContent.Add(nameLabel);
        cardContent.Add(thumbnail);
        cardContent.Add(infoLabel);
        cardContent.Add(selectButton);
        
        card.Add(cardContent);
        
        return card;
    }
    
    private void OnMapSelected(MapData map)
    {
        selectedMap = map;
        onMapSelected?.Invoke(map);
    }
    
    void OnDisable()
    {
        UnregisterEventHandlers();
    }
    
    private void UnregisterEventHandlers()
    {
        if (searchField != null)
            searchField.UnregisterValueChangedCallback(OnSearchValueChanged);
            
        if (backButton != null)
            backButton.clicked -= OnBackButtonClicked;
    }
}