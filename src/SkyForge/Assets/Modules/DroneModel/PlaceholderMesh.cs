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
        // Create center cube
        CreateCenterCube();
        
        // Create arms
        CreateArm(Vector3.forward, "FrontArm");      // Front
        CreateArm(Vector3.back, "BackArm");          // Back
        CreateArm(Vector3.right, "RightArm");        // Right
        CreateArm(Vector3.left, "LeftArm");          // Left
        
        // Create motor markers
        CreateMotorMarker(new Vector3(armLength, 0, armLength), Color.red, "FL_Motor");     // Front Left
        CreateMotorMarker(new Vector3(armLength, 0, -armLength), Color.green, "FR_Motor");  // Front Right
        CreateMotorMarker(new Vector3(-armLength, 0, armLength), Color.blue, "BL_Motor");   // Back Left
        CreateMotorMarker(new Vector3(-armLength, 0, -armLength), Color.yellow, "BR_Motor"); // Back Right
    }
    
    void CreateCenterCube()
    {
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cube);
        center.name = "Center";
        center.transform.SetParent(transform);
        center.transform.localPosition = Vector3.zero;
        center.transform.localScale = new Vector3(centerSize, centerSize, centerSize);
        
        // Set color
        Renderer renderer = center.GetComponent<Renderer>();
        renderer.material.color = Color.gray;
        
        // Remove collider to prevent interference
        Destroy(center.GetComponent<Collider>());
    }
    
    void CreateArm(Vector3 direction, string name)
    {
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = name;
        arm.transform.SetParent(transform);
        
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
        
        // Set color
        Renderer renderer = arm.GetComponent<Renderer>();
        renderer.material.color = Color.white;
        
        // Remove collider to prevent interference
        Destroy(arm.GetComponent<Collider>());
    }
    
    void CreateMotorMarker(Vector3 localPosition, Color color, string name)
    {
        GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        motor.name = name;
        motor.transform.SetParent(transform);
        motor.transform.localPosition = localPosition;
        motor.transform.localScale = new Vector3(motorSphereSize, motorSphereSize, motorSphereSize);
        
        // Set color
        Renderer renderer = motor.GetComponent<Renderer>();
        renderer.material.color = color;
        
        // Remove collider to prevent interference
        Destroy(motor.GetComponent<Collider>());
    }
}