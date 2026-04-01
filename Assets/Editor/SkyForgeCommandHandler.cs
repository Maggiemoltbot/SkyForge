using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SkyForge.Editor
{
    public static class SkyForgeCommandHandler
    {
        // Oeffnet eine Szene per Pfad
        [MenuItem("SkyForge/Open Welle1Test Scene")]
        public static void OpenWelle1TestScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Welle1Test.unity");
            Debug.Log("[SkyForge] Welle1Test Scene geoeffnet");
        }

        // Listet alle Scenes im Build
        public static void ListBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                Debug.Log($"[SkyForge] Build Scene: {s.path} (enabled: {s.enabled})");
        }

        // Fuegt eine Scene zu den Build Settings hinzu
        public static void AddSceneToBuild()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var newScene = new EditorBuildSettingsScene("Assets/Scenes/Welle1Test.unity", true);
            if (!scenes.Exists(s => s.path == newScene.path))
            {
                scenes.Add(newScene);
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[SkyForge] Welle1Test zu Build Settings hinzugefuegt");
            }
        }

        // Zeigt URP Renderer Settings
        public static void ShowRendererSettings()
        {
            var rendererData = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rendererData != null)
                Debug.Log($"[SkyForge] Current Render Pipeline: {rendererData.name}");
            else
                Debug.Log("[SkyForge] Kein Render Pipeline konfiguriert");
        }

        // Erstellt ein leeres Prefab
        public static void CreateEmptyPrefab()
        {
            var go = new GameObject("SkyForge_TestPrefab");
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/SkyForge_TestPrefab.prefab");
            Object.DestroyImmediate(go);
            Debug.Log("[SkyForge] Test Prefab erstellt: Assets/Prefabs/SkyForge_TestPrefab.prefab");
        }

        // Health Check - testet ob SkyForge Commands funktionieren
        public static void HealthCheck()
        {
            Debug.Log("[SkyForge] === Health Check ===");
            Debug.Log($"[SkyForge] Unity Version: {Application.unityVersion}");
            Debug.Log($"[SkyForge] Platform: {Application.platform}");
            Debug.Log($"[SkyForge] Project: {Application.dataPath}");
            ListBuildScenes();
            ShowRendererSettings();
            Debug.Log("[SkyForge] === Health Check Complete ===");
        }
    }
}
