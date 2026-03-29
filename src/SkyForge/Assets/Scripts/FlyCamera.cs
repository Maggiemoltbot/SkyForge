using UnityEngine;

/// <summary>
/// Simple fly camera for SkyForge performance testing.
/// WASD = move, QE = up/down, Shift = fast, Mouse (right-click held) = look.
/// Attach to Main Camera.
/// </summary>
public class FlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 3f;
    public float scrollSpeed = 50f;

    [Header("Look")]
    public float lookSensitivity = 2f;

    private float _yaw;
    private float _pitch;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
    }

    void Update()
    {
        // --- Look (right mouse button held) ---
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -90f, 90f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // --- Movement ---
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        // Scroll wheel = speed boost forward/back
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            move += transform.forward * scroll * scrollSpeed;

        transform.position += move * speed * Time.deltaTime;
    }
}
