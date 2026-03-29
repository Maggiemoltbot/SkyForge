using UnityEngine;

[CreateAssetMenu(fileName = "OSDData", menuName = "SkyForge/OSD Data")]
public class OSDData : ScriptableObject
{
    private static OSDData instance;
    public static OSDData Instance => instance ??= CreateInstance<OSDData>();

    [Header("Battery")]
    public float batteryVoltage = 16.8f;
    public float batteryCurrent = 0f;
    public int batteryConsumption = 0;

    [Header("Flight")]
    public string flightMode = "ACRO";
    public bool isArmed = false;
    public float altitude = 0f;
    public float verticalSpeed = 0f;
    public int rssi = 0;

    [Header("Timer")]
    public float flightTime = 0f;

    private void OnEnable()
    {
        instance = this;
    }

    public void SetBatteryData(float voltage, float current, int consumption)
    {
        batteryVoltage = voltage;
        batteryCurrent = current;
        batteryConsumption = consumption;
    }

    public void SetFlightMode(string mode)
    {
        flightMode = mode;
    }

    public void SetArmedStatus(bool armed)
    {
        isArmed = armed;
        if (!armed) flightTime = 0f; // Reset timer when disarmed
    }

    public void SetAltitudeData(float alt, float verticalSpeed)
    {
        altitude = alt;
        this.verticalSpeed = verticalSpeed;
    }

    public void SetRssi(int signal)
    {
        rssi = signal;
    }

    public void IncrementFlightTime(float deltaTime)
    {
        if (isArmed)
        {
            flightTime += deltaTime;
        }
    }

    public string GetFormattedFlightTime()
    {
        int minutes = (int)(flightTime / 60);
        int seconds = (int)(flightTime % 60);
        return string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }
}