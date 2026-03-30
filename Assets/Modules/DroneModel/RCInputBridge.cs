using UnityEngine;
using UnityEngine.InputSystem;

public class RCInputBridge : MonoBehaviour
{
    [Header("References")]
    public FlightDynamicsBridge flightDynamicsBridge; // Verweis auf die Bridge für das Senden von RC-Daten an SITL

    [Header("Input Settings")]
    public string yawInput = "Yaw"; // Input-Achse für Gier (z.B. horizontaler Joystick)
    public string pitchInput = "Pitch"; // Input-Achse für Nick (z.B. vertikaler Joystick)
    public string rollInput = "Roll"; // Input-Achse für Wank (z.B. horizontaler Joystick)
    public string throttleInput = "Throttle"; // Input-Achse für Gas (z.B. Trigger)
    public string auxSwitchInput = "Jump"; // Input-Achse für einen Schalter (z.B. Taste)

    public float deadzone = 0.05f; // Unempfindlichkeitsbereich für Analog-Achsen

    private int[] rcChannels = new int[16]; // Speicher für die 16 RC-Kanäle
    private bool lastAuxState = false; // Zustand der AUX-Zuweisung, um flankengesteuerte Events zu erkennen

    // UI- und Status-Abfragen
    public bool IsConnected => flightDynamicsBridge != null;
    public string ActiveController
    {
        get
        {
            var gamepad = Gamepad.current;
            if (gamepad != null) return gamepad.displayName;

            var joystick = Joystick.current;
            if (joystick != null) return joystick.displayName;

            return "None";
        }
    }

    /// <summary>
    /// Gibt den aktuellen Wert eines RC-Kanals zurück
    /// </summary>
    public ushort GetChannelValue(int channelIndex)
    {
        if (channelIndex < 0 || channelIndex >= 16) return 1500;
        return (ushort)Mathf.Clamp(rcChannels[channelIndex], 1000, 2000);
    }

    // Beispiele für Joystick-IDs für XBox360 (kann je nach Controller variieren)
    public const string JOY_1 = "joystick button 0"; // Kann für Schalter/D-Pad verwendet werden
    public const string JOY_2 = "joystick button 1";
    public const string JOY_3 = "joystick button 2";
    public const string JOY_4 = "joystick button 3";
    public const string JOY_5 = "joystick button 4"; // Typischerweise LB
    public const string JOY_6 = "joystick button 5"; // Typischerweise RB

    void Start()
    {
        if (flightDynamicsBridge == null)
        {
            Debug.LogError("[RCInputBridge] Keine FlightDynamicsBridge zugewiesen. RC-Eingaben werden nicht gesendet.");
            return;
        }

        Debug.Log("[RCInputBridge] Initialisiert. Warte auf Eingaben von "+ yawInput + ", " + pitchInput + ", etc.");
    }

    void Update()
    {
        // Prüfe, ob die Flugdynamik-Bridge verfügbar ist
        if (flightDynamicsBridge == null) return;

        // Lese Analog-Achsen (Typ: -1.0f to 1.0f)
        float yaw = ApplyDeadzone(SafeGetAxis(yawInput)); // Gier (Ruder)
        float pitch = ApplyDeadzone(SafeGetAxis(pitchInput)); // Nick
        float roll = ApplyDeadzone(SafeGetAxis(rollInput)); // Wank
        float throttle = ApplyDeadzone(SafeGetAxis(throttleInput)); // Gas (oft 0.0f to 1.0f, also invertieren)

        // Konvertiere -1..1 zu 0..1 für Konsistenz

        // Lese Schaltkanal (Typ: Taste oder Joystick-Button)
        bool auxPressed = Input.GetButton(auxSwitchInput); // z.B. Space oder JOY_X
        bool auxSwitchOn = false;

        // Flankenerkennung für einen einfachen Schalter-Modus (Ein/Aus bei Tastendruck)
        if (auxPressed && !lastAuxState)
        {
            // Toggle-Logik für die nächste Aktualisierung
            auxSwitchOn = !PlayerPrefs.GetInt("RC_AUX_State", 0).Equals(0); // Lese den aktuellen Togglespeicher
            PlayerPrefs.SetInt("RC_AUX_State", auxSwitchOn ? 1 : 0); // Speichere den neuen Zustand
        }
        lastAuxState = auxPressed; // Merke den aktuellen Zustand

        // Mappe die gelesenen Eingaben auf die RC-Kanäle, gemäß typischer RC-Konvention
        // Ail(1), Ele(2), Thr(3), Rud(4) + AUX-Schalter auf Kanal 5
        rcChannels[0] = MapFloatToInt(roll, 1000, 2000); // Roll -> Aileron (Kanal 0)
        rcChannels[1] = MapFloatToInt(pitch, 1000, 2000); // Pitch -> Elevator (Kanal 1)
        rcChannels[2] = MapFloatToInt(throttle, 1000, 2000); // Throttle -> (Kanal 2)
        rcChannels[3] = MapFloatToInt(yaw, 1000, 2000); // Yaw -> Rudder (Kanal 3)
        rcChannels[4] = auxSwitchOn ? 1900 : 1100; // AUX-Schalter auf Kanal 4 (1100=OFF, 1900=ON, typische Endwerte)

        // Die restlichen Kanäle (bis 15) können weiter gemappt werden, z.B. für Modusumschaltung

        // Senden Sie die Kanalwerte an die FlightDynamicsBridge
        flightDynamicsBridge.SendRCChannels(rcChannels);
    }

    // Hilfsmethode zum Lesen einer Achse mit Fehlerbehandlung
    float SafeGetAxis(string axisName)
    {
        try
        {
            return Input.GetAxis(axisName);
        }
        catch
        {
            // Debug.LogWarning("[RCInputBridge] Achse '" + axisName + "' konnte nicht gelesen werden.");
            return 0.0f;
        }
    }

    // Hilfsmethode: Wende ein kreuzförmiges Deadzone an
    float ApplyDeadzone(float value)
    {
        if (Mathf.Abs(value) < deadzone)
            return 0.0f;
        return value;
    }

    // Hilfsmethode: Karte einen float-Wert aus einem Bereich auf einen int aus einem anderen Bereich
    // z.B. float [-1..1] to int [1000..2000] für RC-Signale
    int MapFloatToInt(float value, int min, int max)
    {
        // Normalisiere den float-Wert zu [0..1]
        float normalized = (value + 1.0f) / 2.0f; 
        // Berechne den int-Wert
        return (int)(min + normalized * (max - min));
    }
}
