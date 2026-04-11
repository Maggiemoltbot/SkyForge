using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ControllerDiagnostic : MonoBehaviour
{
    void Update()
    {
        if (Time.frameCount % 120 != 0) return;
        
        var gp = Gamepad.current;
        if (gp != null)
        {
            Debug.Log($"[DIAG-GP] L=({gp.leftStick.x.ReadValue():F3},{gp.leftStick.y.ReadValue():F3}) " +
                      $"R=({gp.rightStick.x.ReadValue():F3},{gp.rightStick.y.ReadValue():F3}) " +
                      $"Name={gp.displayName}");
        }
        
        var js = Joystick.current;
        if (js != null)
        {
            string axes = "";
            string buttons = "";
            foreach (var ctrl in js.allControls)
            {
                if (ctrl is AxisControl ac && !(ctrl is ButtonControl))
                {
                    float v = ac.ReadValue();
                    axes += $" {ctrl.name}={v:F3}";
                }
                else if (ctrl is ButtonControl bc)
                {
                    if (bc.isPressed) buttons += $" {ctrl.name}";
                }
            }
            Debug.Log($"[DIAG-JS] {js.displayName} AXES:{axes}");
            if (buttons.Length > 0) Debug.Log($"[DIAG-JS] PRESSED:{buttons}");
        }
    }
}
