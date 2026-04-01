using UnityEngine;
using UnityEngine.UIElements;

public static class UIPanelSettingsUtility
{
    private const string PanelSettingsResourcePath = "UI/PanelSettings/SkyForgePanelSettings";
    private const string ThemeResourcePath = "Styles/SkyForgeTheme";

    private static PanelSettings cachedFallback;
    private static ThemeStyleSheet cachedTheme;

    /// <summary>
    /// Ensures that the supplied <see cref="UIDocument"/> has a valid <see cref="PanelSettings"/>.
    /// When none is assigned (serialized fileID == 0) a shared fallback is applied and a warning is logged.
    /// </summary>
    /// <param name="document">UIDocument instance that should render the UI.</param>
    /// <param name="context">Context object used for logging (typically the MonoBehaviour calling this method).</param>
    /// <param name="documentName">Optional friendly name for logging; falls back to the GameObject name.</param>
    /// <returns>The resolved <see cref="PanelSettings"/> instance.</returns>
    public static PanelSettings Ensure(UIDocument document, Object context, string documentName)
    {
        if (document == null)
        {
            Debug.LogError("[UIPanelSettingsUtility] UIDocument reference is missing; cannot ensure PanelSettings.", context);
            return null;
        }

        if (document.panelSettings != null)
        {
            return document.panelSettings;
        }

        var fallback = GetOrCreateFallback();
        document.panelSettings = fallback;

        string ownerName = string.IsNullOrEmpty(documentName) ? document.gameObject.name : documentName;
        Debug.LogWarning($"[UIPanelSettingsUtility] UIDocument '{ownerName}' had no PanelSettings assigned (fileID 0). Applied fallback '{fallback?.name ?? "<runtime>"}'.", context);

        return fallback;
    }

    private static PanelSettings GetOrCreateFallback()
    {
        if (cachedFallback != null)
        {
            return cachedFallback;
        }

        var resource = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
        if (resource != null)
        {
            cachedFallback = resource;
            ApplyThemeIfAvailable(cachedFallback);
            return cachedFallback;
        }

        cachedFallback = ScriptableObject.CreateInstance<PanelSettings>();
        cachedFallback.hideFlags = HideFlags.DontSave;
        cachedFallback.name = "SkyForgeRuntimePanelSettings";
        cachedFallback.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        cachedFallback.referenceResolution = new Vector2Int(1920, 1080);
        cachedFallback.match = 0.5f;
        cachedFallback.referenceDpi = 96f;
        cachedFallback.targetDisplay = 0;
        cachedFallback.clearDepthStencil = false;
        cachedFallback.clearColor = false;

        ApplyThemeIfAvailable(cachedFallback);

        return cachedFallback;
    }

    private static void ApplyThemeIfAvailable(PanelSettings settings)
    {
        if (settings == null || settings.themeStyleSheet != null)
        {
            return;
        }

        if (cachedTheme == null)
        {
            cachedTheme = Resources.Load<ThemeStyleSheet>(ThemeResourcePath);
            if (cachedTheme == null)
            {
                cachedTheme = Resources.Load<ThemeStyleSheet>("SkyForgeTheme");
            }

            if (cachedTheme == null)
            {
                var legacySheet = Resources.Load<StyleSheet>(ThemeResourcePath) ?? Resources.Load<StyleSheet>("SkyForgeTheme");
                if (legacySheet is ThemeStyleSheet themeSheet)
                {
                    cachedTheme = themeSheet;
                }
#if UNITY_EDITOR
                else if (legacySheet != null)
                {
                    Debug.LogWarning($"[UIPanelSettingsUtility] Resources asset '{ThemeResourcePath}' is a StyleSheet. Consider converting it to a ThemeStyleSheet for PanelSettings compatibility.");
                }
#endif
            }
        }

        if (cachedTheme != null)
        {
            settings.themeStyleSheet = cachedTheme;
        }
    }
}
