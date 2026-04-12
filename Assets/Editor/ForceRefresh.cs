using UnityEditor;

public static class ForceRefreshFromCLI
{
    [MenuItem("Tools/Force Refresh")]
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
