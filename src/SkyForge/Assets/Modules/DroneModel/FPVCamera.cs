using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FPVCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float tiltAngle = 10f; // degrees downward
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private float nearClipPlane = 0.1f;
    [SerializeField] private float farClipPlane = 500f;
    
    [Header("Lens Distortion")]
    [SerializeField] private bool enableDistortion = true;
    [SerializeField] private float distortionAmount = 0.1f;
    
    [Header("Render Settings")]
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    
    private Camera cam;
    private RenderTexture renderTexture;
    
    void Awake()
    {
        SetupCamera();
    }
    
    void SetupCamera()
    {
        cam = GetComponent<Camera>();
        
        // Apply settings
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = nearClipPlane;
        cam.farClipPlane = farClipPlane;
        
        // Set tilt angle
        transform.localRotation = Quaternion.Euler(tiltAngle, 0, 0);
        
        // Create render texture
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(width, height, 24);
            cam.targetTexture = renderTexture;
        }
    }
    
    public RenderTexture GetRenderTexture()
    {
        return renderTexture;
    }
    
    public void SetTiltAngle(float angle)
    {
        tiltAngle = angle;
        transform.localRotation = Quaternion.Euler(tiltAngle, 0, 0);
    }
    
    public void SetDistortion(bool enabled, float amount)
    {
        enableDistortion = enabled;
        distortionAmount = amount;
    }
    
    void OnValidate()
    {
        if (cam != null)
        {
            SetupCamera();
        }
    }
}