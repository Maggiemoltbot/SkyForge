using UnityEngine;
using UnityEngine.UIElements;
using System;

public class About : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset aboutUxml;
    
    private VisualElement root;
    
    // UI References
    private Label titleLabel;
    private Label versionLabel;
    private Label buildLabel;
    private Button websiteButton;
    private Button githubButton;
    private Button backButton;
    
    // Events
    public event Action onBack;
    public event Action onWebsiteClicked;
    public event Action onGitHubClicked;
    
    void OnEnable()
    {
        InitializeUI();
        RegisterEventHandlers();
        UpdateVersionInfo();
    }
    
    private void InitializeUI()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        if (aboutUxml != null)
        {
            root.Clear();
            TemplateContainer template = aboutUxml.Instantiate();
            root.Add(template);
            
            // Get references to UI elements
            titleLabel = root.Q<Label>("title-label");
            versionLabel = root.Q<Label>("version-label");
            buildLabel = root.Q<Label>("build-label");
            websiteButton = root.Q<Button>("website-button");
            githubButton = root.Q<Button>("github-button");
            backButton = root.Q<Button>("back-button");
        }
    }
    
    private void RegisterEventHandlers()
    {
        if (websiteButton != null)
            websiteButton.clicked += () => onWebsiteClicked?.Invoke();
            
        if (githubButton != null)
            githubButton.clicked += () => onGitHubClicked?.Invoke();
            
        if (backButton != null)
            backButton.clicked += () => onBack?.Invoke();
    }
    
    private void UpdateVersionInfo()
    {
        // In a real implementation, this would get the actual version from the project settings
        if (versionLabel != null)
            versionLabel.text = "Version 1.0.0";
            
        // Build date could be set during build process
        if (buildLabel != null)
            buildLabel.text = $"Build {DateTime.Now:yyyy.M.d}";
    }
    
    void OnDisable()
    {
        UnregisterEventHandlers();
    }
    
    private void UnregisterEventHandlers()
    {
        if (websiteButton != null)
            websiteButton.clicked -= () => onWebsiteClicked?.Invoke();
            
        if (githubButton != null)
            githubButton.clicked -= () => onGitHubClicked?.Invoke();
            
        if (backButton != null)
            backButton.clicked -= () => onBack?.Invoke();
    }
}