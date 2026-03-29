#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to set up a complete drone scene with all required components,
/// configurations, and connections for SkyForge integration with Betaflight SITL.
/// </summary>
public class SkyForgeIntegrator
{
    [MenuItem("SkyForge/Setup Drone in Scene")]
    public static void SetupDroneInScene()
    {
        // 1. Create drone prefab
        GameObject dronePrefab = CreateDronePrefab();
        if (dronePrefab == null) return;

        // 2. Create config assets
        DroneConfig droneConfig = CreateOrFindAsset<DroneConfig>("Assets/Configs/DroneConfig.asset");
        BridgeConfig bridgeConfig = CreateOrFindAsset<BridgeConfig>("Assets/Configs/BridgeConfig.asset");
        ControllerConfig controllerConfig = CreateOrFindAsset<ControllerConfig>("Assets/Configs/ControllerConfig.asset");

        // 3. Apply default values
        ApplyDefaultDroneConfig(droneConfig);
        ApplyDefaultBridgeConfig(bridgeConfig);
        ApplyDefaultControllerConfig(controllerConfig);

        // 4. Connect components
        DroneController droneController = dronePrefab.GetComponent<DroneController>();
        droneController.config = droneConfig;

        FlightDynamicsBridge bridge = Object.FindObjectOfType<FlightDynamicsBridge>();
        if (bridge == null)
        {
            GameObject bridgeObj = new GameObject("FlightDynamicsBridge");
            bridge = bridgeObj.AddComponent<FlightDynamicsBridge>();
        }
        bridge.config = bridgeConfig;
        bridge.droneController = droneController;

        // 5. Ensure RC input bridge exists
        RCInputBridge rcInput = Object.FindObjectOfType<RCInputBridge>();
        if (rcInput == null)
        {
            GameObject rcObj = new GameObject("RCInputBridge");
            rcInput = rcObj.AddComponent<RCInputBridge>();
        }
        rcInput.config = controllerConfig;

        // 6. Set spawn position
        dronePrefab.transform.position = new Vector3(0, 2, 0);

        // 7. Save assets
        EditorUtility.SetDirty(dronePrefab);
        EditorUtility.SetDirty(droneConfig);
        EditorUtility.SetDirty(bridgeConfig);
        EditorUtility.SetDirty(controllerConfig);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(rcInput);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SkyForgeIntegrator] Drone setup complete! Created drone prefab with all configurations and connections.");
    }

    /// <summary>
    /// Creates the drone prefab with all required components and children
    /// </summary>
    static GameObject CreateDronePrefab()
    {
        GameObject drone = new GameObject("Drone");
        
        // Add required components
        if (drone.GetComponent<DroneController>() == null)
        {
            drone.AddComponent<DroneController>();
        }
        
        // Use DroneSetup to configure the prefab
        DroneSetup.SetupDronePrefab(drone);
        
        return drone;
    }

    /// <summary>
    /// Creates a ScriptableObject asset at the specified path, or loads it if it already exists
    /// </summary>
    static T CreateOrFindAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    /// <summary>
    /// Applies default values to the drone configuration based on the architecture plan
    /// </summary>
    static void ApplyDefaultDroneConfig(DroneConfig config)
    {
        config.mass = 0.5f;
        config.maxThrustPerMotor = 4.0f;
        config.armLength = 0.12f;
        config.dragCoefficient = 0.1f;
        config.angularDragCoefficient = 0.5f;
        EditorUtility.SetDirty(config);
    }

    /// <summary>
    /// Applies default values to the bridge configuration based on the architecture plan
    /// </summary>
    static void ApplyDefaultBridgeConfig(BridgeConfig config)
    {
        config.bfSITLIPAddress = "127.0.0.1";
        config.fdmSendPort = 9002;
        config.pwmReceivePort = 9003;
        config.updateFrequency = 400;
        EditorUtility.SetDirty(config);
    }

    /// <summary>
    /// Applies default values to the controller configuration based on the architecture plan
    /// </summary>
    static void ApplyDefaultControllerConfig(ControllerConfig config)
    {
        config.bfSITLIPAddress = "127.0.0.1";
        config.rcPort = 9004;
        config.sendRateHz = 100;
        
        // Configure axis mappings
        config.roll.rcChannel = 0;
        config.roll.invert = false;
        config.roll.deadzone = 0.05f;
        config.roll.expo = 0.0f;
        
        config.pitch.rcChannel = 1;
        config.pitch.invert = false;
        config.pitch.deadzone = 0.05f;
        config.pitch.expo = 0.0f;
        
        config.throttle.rcChannel = 2;
        config.throttle.invert = false;
        config.throttle.deadzone = 0.02f;
        config.throttle.expo = 0.0f;
        
        config.yaw.rcChannel = 3;
        config.yaw.invert = false;
        config.yaw.deadzone = 0.05f;
        config.yaw.expo = 0.0f;
        
        // Configure AUX mappings if they exist
        if (config.auxMappings != null && config.auxMappings.Length >= 4)
        {
            config.auxMappings[0].rcChannel = 4;
            config.auxMappings[0].buttonName = "Button South (A/Cross)";
            
            config.auxMappings[1].rcChannel = 5;
            config.auxMappings[1].buttonName = "Button East (B/Circle)";
            
            config.auxMappings[2].rcChannel = 6;
            config.auxMappings[2].buttonName = "Button West (X/Square)";
            
            config.auxMappings[3].rcChannel = 7;
            config.auxMappings[3].buttonName = "Button North (Y/Triangle)";
        }
        
        EditorUtility.SetDirty(config);
    }
}
#endif