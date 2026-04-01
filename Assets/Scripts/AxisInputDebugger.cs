using UnityEngine;

/// <summary>
/// Simple runtime diagnostic overlay that visualises the legacy Input Manager axes
/// used by the drone controller setup (Throttle, Yaw, Pitch, Roll).
/// Attach to an empty GameObject inside a diagnostic scene to verify joystick input.
/// </summary>
public class AxisInputDebugger : MonoBehaviour
{
    [System.Serializable]
    public class AxisEntry
    {
        public string axisName;
        [HideInInspector] public float rawValue;
        [HideInInspector] public float normalizedValue;
        [HideInInspector] public float smoothedValue;
    }

    [Header("Axes")]
    public AxisEntry[] axes =
    {
        new AxisEntry { axisName = "Throttle" },
        new AxisEntry { axisName = "Yaw" },
        new AxisEntry { axisName = "Pitch" },
        new AxisEntry { axisName = "Roll" }
    };

    [Header("Visuals")]
    [Tooltip("Smoothing time in seconds for the slider visualisation (0 = no smoothing).")]
    public float smoothingTime = 0.1f;

    [Tooltip("Range of the slider in the overlay. Defaults to 0-1.")]
    public Vector2 sliderRange = new Vector2(0f, 1f);

    [Tooltip("If enabled, raw axis values are clamped to the -1..1 range before normalisation.")]
    public bool clampRawInput = true;

    private const float RawMin = -1f;
    private const float RawMax = 1f;

    private void Reset()
    {
        // Reinitialise default axis names if the component is added via inspector.
        axes = new[]
        {
            new AxisEntry { axisName = "Throttle" },
            new AxisEntry { axisName = "Yaw" },
            new AxisEntry { axisName = "Pitch" },
            new AxisEntry { axisName = "Roll" }
        };
    }

    private void Update()
    {
        float lerpFactor;
        if (smoothingTime <= 0f)
        {
            lerpFactor = 1f;
        }
        else
        {
            // Exponential smoothing factor based on configured time constant.
            lerpFactor = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothingTime));
        }

        foreach (var axis in axes)
        {
            if (axis == null || string.IsNullOrWhiteSpace(axis.axisName))
            {
                continue;
            }

            float raw = Input.GetAxis(axis.axisName);
            if (clampRawInput)
            {
                raw = Mathf.Clamp(raw, RawMin, RawMax);
            }

            float normalized = Mathf.InverseLerp(RawMin, RawMax, raw);

            axis.rawValue = raw;
            axis.normalizedValue = normalized;
            axis.smoothedValue = Mathf.Lerp(axis.smoothedValue, normalized, lerpFactor);
        }
    }

    private void OnGUI()
    {
        const float boxWidth = 340f;
        float boxHeight = 50f + axes.Length * 55f;

        GUILayout.BeginArea(new Rect(20f, 20f, boxWidth, boxHeight), GUI.skin.box);
        GUILayout.Label("Flight Axis Debugger", GUI.skin.label);
        GUILayout.Space(10f);

        foreach (var axis in axes)
        {
            if (axis == null || string.IsNullOrWhiteSpace(axis.axisName))
            {
                continue;
            }

            GUILayout.Label($"{axis.axisName} — raw {axis.rawValue:F2}");
            float sliderValue = Mathf.Lerp(sliderRange.x, sliderRange.y, axis.smoothedValue);
            GUI.enabled = false;
            GUILayout.HorizontalSlider(sliderValue, sliderRange.x, sliderRange.y);
            GUI.enabled = true;
            GUILayout.Space(5f);
        }

        GUILayout.EndArea();
    }
}
