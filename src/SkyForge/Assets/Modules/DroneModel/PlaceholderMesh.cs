using UnityEngine;

[ExecuteAlways]
public class PlaceholderMesh : MonoBehaviour
{
    [Header("Drone Dimensions")]
    [SerializeField] private float armLength = 0.6f;
    [SerializeField] private float armWidth = 0.05f;
    [SerializeField] private float armHeight = 0.025f;
    [SerializeField] private float centerSize = 0.15f;
    [SerializeField] private float motorSphereSize = 0.05f;
    private bool isBuilt = false;
    
    void Start()
    {
        if (!isBuilt) CreateQuadFrame();
    }

    void OnEnable()
    {
        // Also create in Editor mode
        if (transform.childCount == 0)
        {
            CreateQuadFrame();
        }
    }
    
    void CreateQuadFrame()
    {
        // Prevent duplicate creation
        if (transform.childCount > 0) return;
        isBuilt = true;
        
        // Ensure this GameObject is on the Drone layer
        SetDroneLayer(gameObject);
        
        // Create center cube
        CreateCenterCube();
        
        // Create arms
        CreateArm(Vector3.forward, "FrontArm");      // Front
        CreateArm(Vector3.back, "BackArm");          // Back
        CreateArm(Vector3.right, "RightArm");        // Right
        CreateArm(Vector3.left, "LeftArm");          // Left
        
        // Create motor markers
        CreateMotorMarker(new Vector3(armLength, 0, armLength), Color.red, "FL_Motor", 0, true);      // FL, CW
        CreateMotorMarker(new Vector3(armLength, 0, -armLength), Color.green, "FR_Motor", 1, false);   // FR, CCW
        CreateMotorMarker(new Vector3(-armLength, 0, armLength), Color.blue, "BL_Motor", 2, false);    // BL, CCW
        CreateMotorMarker(new Vector3(-armLength, 0, -armLength), Color.yellow, "BR_Motor", 3, true);  // BR, CW
    }
    
    /// <summary>
    /// Uses a custom overlay shader that renders AFTER the Gaussian Splat composite pass.
    /// The GS composite uses ZTest Always + alpha blend at BeforeRenderingTransparents,
    /// so it overwrites anything rendered before. Our DroneOverlay shader also uses
    /// ZTest Always but in the Overlay queue (4000), ensuring the drone is visible.
    /// </summary>
    private void ForceOpaqueRendering(Renderer renderer, Color color)
    {
        // Use our custom overlay shader that renders AFTER Gaussian Splats
        Shader overlayShader = Shader.Find("SkyForge/DroneOverlay");
        if (overlayShader != null)
        {
            Material mat = new Material(overlayShader);
            mat.SetColor("_Color", color);
            renderer.material = mat;
        }
        else
        {
            // Fallback: just set color on default material
            Material mat = renderer.material;
            mat.color = color;
            mat.renderQueue = 4000; // Overlay queue
            Debug.LogWarning("[SkyForge] DroneOverlay shader not found! Falling back to default material.");
        }
    }

    void CreateCenterCube()
    {
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cube);
        center.name = "Center";
        center.transform.SetParent(transform);
        center.transform.localPosition = Vector3.zero;
        center.transform.localScale = new Vector3(centerSize, centerSize, centerSize);
        
        // Set to drone layer
        SetDroneLayer(center);
        
        // Force opaque rendering so drone is visible with Gaussian Splats
        ForceOpaqueRendering(center.GetComponent<Renderer>(), Color.gray);
        
        // Remove collider to prevent interference
        Destroy(center.GetComponent<Collider>());
    }
    
    void CreateArm(Vector3 direction, string name)
    {
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = name;
        arm.transform.SetParent(transform);
        
        // Set to drone layer
        SetDroneLayer(arm);
        
        // Position arm at the center of its respective side
        Vector3 position = direction * armLength * 0.5f;
        arm.transform.localPosition = position;
        
        // Orient arm to point in the correct direction
        if (direction == Vector3.forward || direction == Vector3.back)
        {
            arm.transform.localScale = new Vector3(armWidth, armHeight, armLength);
        }
        else
        {
            arm.transform.localScale = new Vector3(armLength, armHeight, armWidth);
        }
        
        // Force opaque rendering so arm is visible with Gaussian Splats
        ForceOpaqueRendering(arm.GetComponent<Renderer>(), Color.white);
        
        // Remove collider to prevent interference
        Destroy(arm.GetComponent<Collider>());
    }
    
    void CreateMotorMarker(Vector3 localPosition, Color color, string name, int motorIndex, bool clockwise)
    {
        GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        motor.name = name;
        motor.transform.SetParent(transform);
        motor.transform.localPosition = localPosition;
        motor.transform.localScale = new Vector3(motorSphereSize, motorSphereSize, motorSphereSize);
        
        // Set to drone layer
        SetDroneLayer(motor);
        
        // Force opaque rendering so motor is visible with Gaussian Splats
        ForceOpaqueRendering(motor.GetComponent<Renderer>(), color);
        
        // Remove collider to prevent interference
        Destroy(motor.GetComponent<Collider>());
        
        // Add LED thrust indicator
        LEDThrustIndicator led = motor.AddComponent<LEDThrustIndicator>();
        led.motorIndex = motorIndex;

        // Create propeller disc
        GameObject propeller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        propeller.name = name.Replace("Motor", "Propeller");
        propeller.transform.SetParent(motor.transform);
        propeller.transform.localPosition = new Vector3(0, 0.01f, 0); // Leicht über Motor
        propeller.transform.localScale = new Vector3(0.1f, 0.002f, 0.1f); // Flache Disc

        // Set to drone layer
        SetDroneLayer(propeller);

        // Force opaque so propeller is visible with Gaussian Splats
        ForceOpaqueRendering(propeller.GetComponent<Renderer>(), new Color(0.5f, 0.5f, 0.5f, 1f));

        Destroy(propeller.GetComponent<Collider>()); // Kein Collider

        PropellerRotation propRotation = propeller.AddComponent<PropellerRotation>();
        propRotation.motorIndex = motorIndex;
        propRotation.clockwise = clockwise;
    }
    
    // Overloaded method to maintain compatibility
    void CreateMotorMarker(Vector3 localPosition, Color color, string name)
    {
        // Default values for index and rotation direction
        int motorIndex = 0;
        bool clockwise = true;
        
        // Determine motor index based on name
        switch (name)
        {
            case "FL_Motor":
                motorIndex = 0;
                clockwise = true;  // CW
                break;
            case "FR_Motor":
                motorIndex = 1;
                clockwise = false; // CCW
                break;
            case "BL_Motor":
                motorIndex = 2;
                clockwise = false; // CCW
                break;
            case "BR_Motor":
                motorIndex = 3;
                clockwise = true;  // CW
                break;
        }
        
        CreateMotorMarker(localPosition, color, name, motorIndex, clockwise);
    }
    
    /// <summary>
    /// Sets the GameObject and all its children to the "Drone" layer
    /// </summary>
    /// <param name="obj">GameObject to set layer for</param>
    private void SetDroneLayer(GameObject obj)
    {
        // Try to find the "Drone" layer, if it doesn't exist use layer 3 as default
        int droneLayer = LayerMask.NameToLayer("Drone");
        if (droneLayer == -1)
        {
            droneLayer = 3; // Default to layer 3
        }
        
        obj.layer = droneLayer;
        
        // Recursively set layer for all children
        foreach (Transform child in obj.transform)
        {
            SetDroneLayer(child.gameObject);
        }
    }
}