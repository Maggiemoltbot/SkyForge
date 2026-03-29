using UnityEngine;

/// <summary>
/// Ein einfaches Skript, das die Position, Drehung und Geschwindigkeit eines GameObjects (eine Drohne) steuert.
/// In einer realen Implementation würde dies die Physik-Engine beeinflussen.
/// Hier dient es als Platzhalter für eine echte Drohnensteuerung.
/// </summary>
public class DroneController : MonoBehaviour
{
    [Header("Dynamics")]
    public Vector3 position = Vector3.zero; // Position der Drohne (wird von FlightDynamicsBridge gesetzt)
    public Vector3 velocity = Vector3.zero; // Geschwindigkeit der Drohne (wird von FlightDynamicsBridge gesetzt)
    public Quaternion orientation = Quaternion.identity; // Orientierung der Drohne (wird von FlightDynamicsBridge gesetzt)

    [Header("Settings")]
    public float lerpSpeed = 10f; // Geschwindigkeit für die Interpolation von Position und Rotation

    private Rigidbody rb; // Optional, falls eine echte Physik-Integration erfolgt

    void Awake()
    {
        // Speichere die Rigidbody-Komponente, falls vorhanden
        rb = GetComponent<Rigidbody>();

        // Stelle sicher, dass das GameObject zentral ist
        transform.localPosition = Vector3.zero;
    }

    void Start()
    {
        // Initialisiere die Position, falls gewünscht
        transform.position = position;
        transform.rotation = orientation;
    }

    void Update()
    {
        // Interpoliere die Darstellung zwischen der letzten bekannten und der neuen Position
        // Dies macht die Bewegung flüssiger, auch wenn die SITL-Aktualisierungsrate variiert
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, orientation, Time.deltaTime * lerpSpeed);
    }

    void FixedUpdate()
    {
        // Hier könnte die echte Physik-Integration erfolgen
        // z.B. Anwendung von Kräften oder Drehmomenten auf den Rigidbody basierend auf den Eingaben

        // Beispiel (auskommentiert):
        // if (rb != null)
        // {
        //     rb.velocity = velocity; // Setzt die Geschwindigkeit
        //     // Oder: rb.AddForce(throttle); // Fügt eine Kraft in Aufwärtsrichtung hinzu
        // }
    }
}