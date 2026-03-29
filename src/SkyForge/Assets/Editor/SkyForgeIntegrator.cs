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
        // 1. Create config assets first
        DroneConfig droneConfig = CreateOrFindAsset<DroneConfig>("Assets/Configs/DroneConfig.asset");
        BridgeConfig bridgeConfig = CreateOrFindAsset<BridgeConfig>("Assets/Configs/BridgeConfig.asset");
        ControllerConfig controllerConfig = CreateOrFindAsset<ControllerConfig>("Assets/Configs/ControllerConfig.asset");

        // 2. Apply default values to configs (ScriptableObjects have public fields)
        ApplyDefaultDroneConfig(droneConfig);
        ApplyDefaultBridgeConfig(bridgeConfig);
        ApplyDefaultControllerConfig(controllerConfig);

        // 3. Create drone GameObject
        GameObject drone = new GameObject("Drone");
        drone.transform.position = new Vector3(0, 2, 0);

        // Add DroneController (which adds Rigidbody via RequireComponent)
        DroneController droneController = drone.AddComponent<DroneController>();

        // Set DroneController.config via SerializedObject (private [SerializeField])
        SetSerializedField(droneController, "config", droneConfig);

        // Run DroneSetup to add BoxCollider, motors, FPV camera, placeholder mesh
        DroneSetup.SetupDronePrefab(drone);

        // 4. Create FlightDynamicsBridge
        GameObject bridgeObj = new GameObject("FlightDynamicsBridge");
        FlightDynamicsBridge bridge = bridgeObj.AddComponent<FlightDynamicsBridge>();

        // Set private fields via SerializedObject
        SetSerializedField(bridge, "config", bridgeConfig);
        SetSerializedField(bridge, "droneController", droneController);

        // 5. Create RCInputBridge
        GameObject rcObj = new GameObject("RCInputBridge");
        RCInputBridge rcInput = rcObj.AddComponent<RCInputBridge>();

        // Set private field via SerializedObject
        SetSerializedField(rcInput, "config", controllerConfig);

        // 6. Mark everything dirty and save
        EditorUtility.SetDirty(drone);
        EditorUtility.SetDirty(bridgeObj);
        EditorUtility.SetDirty(rcObj);
        EditorUtility.SetDirty(droneConfig);
        EditorUtility.SetDirty(bridgeConfig);
        EditorUtility.SetDirty(controllerConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the drone in hierarchy
        Selection.activeGameObject = drone;

        Debug.Log("[SkyForge] Setup complete! Drone at (0, 2, 0) with Bridge + RC Input configured.");
        Debug.Log("[SkyForge] Next: Start SITL with tools/start_sitl.sh, then press Play.");
    }

    /// <summary>
    /// Sets a private [SerializeField] on a component using SerializedObject
    /// </summary>
    static void SetSerializedField(Component component, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[SkyForge] Field '{fieldName}' not found on {component.GetType().Name}");
        }
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
            AssetDatabase.SaveAssets();
        }
        return asset;
    }

    static void ApplyDefaultDroneConfig(DroneConfig config)
    {
        config.mass = 0.5f;
        config.maxThrustPerMotor = 4.0f;
        config.armLength = 0.12f;
        config.dragCoefficient = 0.1f;
        config.angularDragCoefficient = 0.5f;
        EditorUtility.SetDirty(config);
    }

    static void ApplyDefaultBridgeConfig(BridgeConfig config)
    {
        config.bfSITLIPAddress = "127.0.0.1";
        config.fdmSendPort = 9002;
        config.pwmReceivePort = 9003;
        config.updateFrequency = 400;
        EditorUtility.SetDirty(config);
    }

    static void ApplyDefaultControllerConfig(ControllerConfig config)
    {
        config.bfSITLIPAddress = "127.0.0.1";
        config.rcPort = 9004;
        config.sendRateHz = 100;

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

        if (config.auxMappings != null && config.auxMappings.Length >= 4)
        {
            config.auxMappings[0].rcChannel = 4;
            config.auxMappings[0].buttonName = "Button South";
            config.auxMappings[1].rcChannel = 5;
            config.auxMappings[1].buttonName = "Button East";
            config.auxMappings[2].rcChannel = 6;
            config.auxMappings[2].buttonName = "Button West";
            config.auxMappings[3].rcChannel = 7;
            config.auxMappings[3].buttonName = "Button North";
        }

        EditorUtility.SetDirty(config);
    }
}
#endif
