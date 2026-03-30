using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraMode { FreeCam, FPV, ThirdPerson }
    
    public Camera mainCamera;      // Main Camera mit FlyCamera
    public Transform droneTransform; // Drone GameObject
    
    [Header("Third Person")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0, 2, -5);
    [SerializeField] private float followSpeed = 5f;
    
    private CameraMode currentMode = CameraMode.FreeCam;
    public CameraMode CurrentMode => currentMode;
    private FlyCamera flyCameraComponent;
    private Vector3 fpvPosition = new Vector3(0, 0.02f, 0.05f);
    private float freeCamFOV = 60f;
    private float fpvFOV = 120f;
    private float thirdPersonFOV = 60f;
    
    void Start()
    {
        // Get the FlyCamera component
        if (mainCamera != null)
        {
            flyCameraComponent = mainCamera.GetComponent<FlyCamera>();
        }
        
        // Set initial camera mode
        SwitchToMode(CameraMode.FreeCam);
    }
    
    void LateUpdate()
    {
        HandleInput();
        UpdateCameraPosition();
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
    
    void UpdateCameraPosition()
    {
        if (currentMode == CameraMode.ThirdPerson && mainCamera != null && droneTransform != null)
        {
            // Calculate desired position with offset
            Vector3 desiredPosition = droneTransform.TransformPoint(chaseOffset);
            
            // Smoothly move towards the desired position
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
            
            // Always look at the drone
            mainCamera.transform.LookAt(droneTransform.position);
        }
    }
    
    void SwitchToMode(CameraMode mode)
    {
        if (currentMode == mode) return;
        
        // Reset settings that might have been changed
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = freeCamFOV;
        }
        
        // Enable the selected mode
        switch (mode)
        {
            case CameraMode.FreeCam:
                if (mainCamera != null)
                {
                    // Ensure camera is detached from the drone hierarchy when entering FreeCam
                    mainCamera.transform.SetParent(null);

                    // Enable FlyCamera component
                    if (flyCameraComponent != null)
                    {
                        flyCameraComponent.enabled = true;
                    }
                    
                    // Set FOV
                    mainCamera.fieldOfView = freeCamFOV;
                }
                break;
                
            case CameraMode.FPV:
                if (mainCamera != null && droneTransform != null)
                {
                    // Disable FlyCamera component
                    if (flyCameraComponent != null)
                    {
                        flyCameraComponent.enabled = false;
                    }
                    
                    // Set FPV position and rotation relative to drone
                    mainCamera.transform.SetParent(droneTransform);
                    mainCamera.transform.localPosition = fpvPosition;
                    mainCamera.transform.localRotation = Quaternion.identity;
                    
                    // Set FOV
                    mainCamera.fieldOfView = fpvFOV;
                }
                break;
                
            case CameraMode.ThirdPerson:
                if (mainCamera != null && droneTransform != null)
                {
                    // Disable FlyCamera component
                    if (flyCameraComponent != null)
                    {
                        flyCameraComponent.enabled = false;
                    }
                    
                    // Detach from drone if it was child
                    mainCamera.transform.SetParent(null);
                    
                    // Set FOV
                    mainCamera.fieldOfView = thirdPersonFOV;
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
                modeText += "FreeCam";
                break;
            case CameraMode.FPV:
                modeText += "FPV";
                break;
            case CameraMode.ThirdPerson:
                modeText += "ThirdPerson";
                break;
        }
        
        modeText += " | F1/F2/F3/Tab";
        
        GUI.Label(new Rect(10, Screen.height - 30, 500, 30), modeText, style);
    }
}