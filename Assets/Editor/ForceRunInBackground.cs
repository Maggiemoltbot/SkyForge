using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ForceRunInBackground
{
    static ForceRunInBackground()
    {
        Application.runInBackground = true;
        PlayerSettings.runInBackground = true;
        Debug.Log("[SkyForge] runInBackground forced to TRUE");
    }
}
