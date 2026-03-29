#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class DroneSetup
{
    static DroneSetup()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    static void OnHierarchyChanged()
    {
        // Auto-setup drone prefab when detected in hierarchy
        DroneController[] controllers = Object.FindObjectsOfType<DroneController>();
        foreach (DroneController controller in controllers)
        {
            if (controller.GetComponent<BoxCollider>() == null)
            {
                SetupDronePrefab(controller.gameObject);
            }
        }
    }

    [MenuItem("SkyForge/Setup Drone Prefab")]
    public static void SetupDronePrefabMenuItem()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null && selected.GetComponent<DroneController>() != null)
        {
            SetupDronePrefab(selected);
        }
        else
        {
            Debug.LogError("Please select a GameObject with DroneController component");
        }
    }

    public static void SetupDronePrefab(GameObject drone)
    {
        // Add BoxCollider (0.3m x 0.05m x 0.3m — Quad-Rahmen)
        BoxCollider boxCollider = drone.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = drone.AddComponent<BoxCollider>();
        }
        boxCollider.size = new Vector3(0.3f, 0.05f, 0.3f);

        // Create motor points
        CreateMotorPoints(drone);

        // Create FPV Camera
        CreateFPVCamera(drone);

        // Create placeholder mesh
        CreatePlaceholderMesh(drone);
    }

    static void CreateMotorPoints(GameObject drone)
    {
        string[] motorNames = { "FrontLeft", "FrontRight", "BackLeft", "BackRight" };
        Vector3[] positions = {
            new Vector3(0.12f, 0, 0.12f),
            new Vector3(0.12f, 0, -0.12f),
            new Vector3(-0.12f, 0, 0.12f),
            new Vector3(-0.12f, 0, -0.12f)
        };

        for (int i = 0; i < 4; i++)
        {
            Transform motor = drone.transform.Find(motorNames[i]);
            if (motor == null)
            {
                GameObject motorObj = new GameObject(motorNames[i]);
                motorObj.transform.SetParent(drone.transform);
                motorObj.transform.localPosition = positions[i];
                motorObj.transform.localRotation = Quaternion.identity;
            }
        }
    }

    static void CreateFPVCamera(GameObject drone)
    {
        Transform fpvCamera = drone.transform.Find("FPVCamera");
        if (fpvCamera == null)
        {
            GameObject cameraObj = new GameObject("FPVCamera");
            cameraObj.transform.SetParent(drone.transform);
            
            // Position 0/0.02/0.05, Rotation 10° nach unten
            cameraObj.transform.localPosition = new Vector3(0, 0.02f, 0.05f);
            cameraObj.transform.localRotation = Quaternion.Euler(10, 0, 0);
            
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.fieldOfView = 120f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            
            // RenderTexture 640x480
            RenderTexture rt = new RenderTexture(640, 480, 24);
            cam.targetTexture = rt;
        }
    }

    static void CreatePlaceholderMesh(GameObject drone)
    {
        // This will be handled by the PlaceholderMesh script
        if (drone.GetComponent<PlaceholderMesh>() == null)
        {
            drone.AddComponent<PlaceholderMesh>();
        }
    }
}
#endif