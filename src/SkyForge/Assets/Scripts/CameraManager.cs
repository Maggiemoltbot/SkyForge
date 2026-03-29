using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraMode { FreeCam, FPV, ThirdPerson }
    
    [SerializeField] private Camera mainCamera;      // Main Camera mit FlyCamera
    [SerializeField] private Camera fpvCamera;        // FPV auf der Drohne
    [SerializeField] private Transform droneTransform; // Drone GameObject
    
    [Header("Third Person")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0, 2, -5);
    [SerializeField] private float followSpeed = 5f;
    
    private CameraMode currentMode = CameraMode.FreeCam;
    private Camera chaseCamera;
    private RenderTexture fpvRenderTexture; // Backup der original RT
    
    void Start()
    {
        // Store the original FPV render texture
        if (fpvCamera != null)
        {
            fpvRenderTexture = fpvCamera.targetTexture;
        }
        
        // Create the chase camera as a child of this GameObject
        GameObject chaseCamObj = new GameObject("ChaseCamera");
        chaseCamObj.transform.SetParent(transform, false);
        chaseCamera = chaseCamObj.AddComponent<Camera>();
        chaseCamera.enabled = false;
        
        // Set initial camera mode
        SwitchToMode(CameraMode.FreeCam);
    }
    
    void Update()
    {
        HandleInput();
        UpdateChaseCamera();
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SwitchToMode(CameraMode.FreeCam);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            SwitchToMode(CameraMode.FPV);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            SwitchToMode(CameraMode.ThirdPerson);
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Cycle through modes
            CameraMode nextMode = (CameraMode)(((int)currentMode + 1) % 3);
            SwitchToMode(nextMode);
        }
    }
    
    void UpdateChaseCamera()
    {
        if (currentMode == CameraMode.ThirdPerson && chaseCamera != null && droneTransform != null)
        {
            // Calculate desired position with offset
            Vector3 desiredPosition = droneTransform.TransformPoint(chaseOffset);
            
            // Smoothly move towards the desired position
            chaseCamera.transform.position = Vector3.Lerp(chaseCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
            
            // Always look at the drone
            chaseCamera.transform.LookAt(droneTransform.position);
        }
    }
    
    void SwitchToMode(CameraMode mode)
    {
        if (currentMode == mode) return;
        
        // Disable all cameras first
        if (mainCamera != null && mainCamera.GetComponent<FlyCamera>() != null)
        {
            mainCamera.GetComponent<FlyCamera>().enabled = false;
        }
        
        if (fpvCamera != null)
        {
            fpvCamera.enabled = false;
            fpvCamera.targetTexture = fpvRenderTexture; // Restore render texture
        }
        
        if (chaseCamera != null)
        {
            chaseCamera.enabled = false;
        }
        
        // Enable the selected mode
        switch (mode)
        {
            case CameraMode.FreeCam:
                if (mainCamera != null && mainCamera.GetComponent<FlyCamera>() != null)
                {
                    mainCamera.GetComponent<FlyCamera>().enabled = true;
                }
                break;
                
            case CameraMode.FPV:
                if (fpvCamera != null)
                {
                    fpvCamera.enabled = true;
                    fpvCamera.targetTexture = null; // Render directly to screen
                }
                break;
                
            case CameraMode.ThirdPerson:
                if (chaseCamera != null)
                {
                    chaseCamera.enabled = true;
                }
                break;
        }
        
        currentMode = mode;
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        string modeText = "Camera: ";
        switch (currentMode)
        {
            case CameraMode.FreeCam:
                modeText += "Free Cam";
                break;
            case CameraMode.FPV:
                modeText += "FPV";
                break;
            case CameraMode.ThirdPerson:
                modeText += "Third Person";
                break;
        }
        
        GUI.Label(new Rect(10, 10, 300, 30), modeText, style);
    }
}