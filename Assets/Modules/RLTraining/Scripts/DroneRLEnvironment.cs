using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Unity-seitiges RL-Environment für das SkyForge Drohnen-Training.
/// 
/// Kommuniziert mit dem Python-Training-Skript via TCP-Socket (Port 9020).
/// Protokoll: JSON-Nachrichten, newline-terminiert.
/// 
/// Observation Space (18-dim):
///   [0-2]  Position relativ zum Ziel (x,y,z) in Metern
///   [3-5]  Velocity (vx,vy,vz) in m/s
///   [6-8]  Rotation (roll, pitch, yaw) in Radiant
///   [9-11] Angular Velocity (ωx,ωy,ωz) in rad/s
///   [12-15] Motor-PWM (4x) normalisiert [0,1]
///   [16]   Höhe über Boden in Metern
///   [17]   Distanz zum Ziel in Metern
///
/// Action Space (4-dim continuous):
///   [0-3]  Motor-PWM-Deltas (FL, FR, BL, BR), Bereich [-1, 1]
///          Wird auf absolute PWM-Werte [0,1] geclampt.
/// </summary>
public class DroneRLEnvironment : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector-Felder
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Drone References")]
    [Tooltip("Der DroneController der zu trainierenden Drohne.")]
    [SerializeField] private DroneController droneController;

    [Header("RL Environment Settings")]
    [Tooltip("Zielposition für den Hover-Task.")]
    [SerializeField] private Vector3 targetPosition = new Vector3(0f, 5f, 0f);

    [Tooltip("Start-Position der Drohne beim Reset.")]
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 1f, 0f);

    [Tooltip("Maximale Episode-Länge in Sekunden.")]
    [SerializeField] private float maxEpisodeDuration = 30f;

    [Tooltip("Radius in Metern, ab dem die Drohne als 'out-of-bounds' gilt.")]
    [SerializeField] private float outOfBoundsRadius = 50f;

    [Tooltip("Crash-Höhe: Unter diesem Y-Wert gilt die Drohne als gecrasht.")]
    [SerializeField] private float crashHeight = -0.1f;

    [Header("Reward Weights")]
    [SerializeField] private float rewardAlpha = 1.0f;    // Position error weight
    [SerializeField] private float rewardBeta  = 0.5f;    // Orientation error weight
    [SerializeField] private float rewardGamma = 0.1f;    // Velocity penalty weight
    [SerializeField] private float rewardDelta = 0.01f;   // Energy penalty weight
    [SerializeField] private float rewardEpsilon = 0.1f;  // Alive bonus
    [SerializeField] private float crashPenalty = 100f;

    [Header("Socket Settings")]
    [Tooltip("TCP-Port für Python ↔ Unity RL-Kommunikation.")]
    [SerializeField] private int rlSocketPort = 9020;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool showGizmos = true;

    // ─────────────────────────────────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float episodeStartTime;
    private float[] currentMotorPWM = new float[4]; // Absolute PWM-Werte [0,1]
    private int episodeCount = 0;
    private float episodeCumulativeReward = 0f;

    // Socket
    private TcpListener tcpListener;
    private TcpClient connectedClient;
    private NetworkStream networkStream;
    private readonly ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> outgoingMessages = new ConcurrentQueue<string>();
    private bool socketRunning = false;
    private System.Threading.Thread socketThread;

    // Step-Synchronisation
    private bool waitingForAction = false;
    private float[] pendingAction = null;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (droneController == null)
            droneController = GetComponentInChildren<DroneController>();

        if (droneController == null)
            Debug.LogError("[DroneRLEnvironment] Kein DroneController gefunden! Bitte im Inspector setzen.");
    }

    void Start()
    {
        StartSocketServer();
        ResetEnvironment();
    }

    void FixedUpdate()
    {
        ProcessIncomingMessages();

        if (pendingAction != null)
        {
            ApplyAction(pendingAction);
            pendingAction = null;

            float reward = CalculateReward();
            bool done = CheckDone();
            float[] obs = GetObservation();

            episodeCumulativeReward += reward;

            SendStepResult(obs, reward, done);

            if (done)
            {
                if (debugLog)
                    Debug.Log($"[RLEnv] Episode {episodeCount} beendet. Reward: {episodeCumulativeReward:F2}");

                episodeCount++;
                episodeCumulativeReward = 0f;
                ResetEnvironment();
            }
        }
    }

    void OnDestroy()
    {
        StopSocketServer();
    }

    void OnApplicationQuit()
    {
        StopSocketServer();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Environment API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Setzt die Drohne auf die Startposition zurück.
    /// Wird zu Beginn jeder Episode aufgerufen.
    /// </summary>
    public void ResetEnvironment()
    {
        if (droneController == null) return;

        // Drohne auf Startposition
        droneController.transform.position = startPosition;
        droneController.transform.rotation = Quaternion.identity;

        // Physik zurücksetzen via DroneController.ResetDrone()
        droneController.ResetDrone();

        // Motoren auf 0
        for (int i = 0; i < 4; i++)
            currentMotorPWM[i] = 0f;

        // Episode-Timer
        episodeStartTime = Time.time;
        waitingForAction = false;
        pendingAction = null;

        if (debugLog)
            Debug.Log($"[RLEnv] Reset. Ziel: {targetPosition}, Start: {startPosition}");

        // Initiale Observation senden
        float[] obs = GetObservation();
        SendResetResult(obs);
    }

    /// <summary>
    /// Wendet eine Action (4 Motor-Deltas) auf die Drohne an.
    /// </summary>
    /// <param name="action">float[4] mit Werten in [-1, 1]</param>
    public void ApplyAction(float[] action)
    {
        if (droneController == null || action == null || action.Length < 4) return;

        // Action als Delta auf aktuelle PWM-Werte
        for (int i = 0; i < 4; i++)
        {
            currentMotorPWM[i] = Mathf.Clamp01(currentMotorPWM[i] + action[i] * 0.1f);
            droneController.motorPWM[i] = currentMotorPWM[i];
        }
    }

    /// <summary>
    /// Sammelt den aktuellen Observation-Vektor (18-dimensional).
    /// </summary>
    public float[] GetObservation()
    {
        if (droneController == null)
            return new float[18];

        Vector3 pos = droneController.GetPosition();
        Vector3 vel = droneController.GetVelocity();
        Vector3 angVel = droneController.GetAngularVelocity();
        Quaternion rot = droneController.GetRotation();

        // Euler-Winkel in Radiant
        Vector3 euler = rot.eulerAngles;
        float roll  = Mathf.Deg2Rad * WrapAngle(euler.z);
        float pitch = Mathf.Deg2Rad * WrapAngle(euler.x);
        float yaw   = Mathf.Deg2Rad * WrapAngle(euler.y);

        // Relative Position zum Ziel
        Vector3 relPos = pos - targetPosition;

        float distToTarget = relPos.magnitude;

        float[] obs = new float[18];
        obs[0]  = Mathf.Clamp(relPos.x, -50f, 50f);
        obs[1]  = Mathf.Clamp(relPos.y, -50f, 50f);
        obs[2]  = Mathf.Clamp(relPos.z, -50f, 50f);
        obs[3]  = Mathf.Clamp(vel.x, -20f, 20f);
        obs[4]  = Mathf.Clamp(vel.y, -20f, 20f);
        obs[5]  = Mathf.Clamp(vel.z, -20f, 20f);
        obs[6]  = roll;
        obs[7]  = pitch;
        obs[8]  = yaw;
        obs[9]  = Mathf.Clamp(angVel.x, -10f, 10f);
        obs[10] = Mathf.Clamp(angVel.y, -10f, 10f);
        obs[11] = Mathf.Clamp(angVel.z, -10f, 10f);
        obs[12] = currentMotorPWM[0];
        obs[13] = currentMotorPWM[1];
        obs[14] = currentMotorPWM[2];
        obs[15] = currentMotorPWM[3];
        obs[16] = Mathf.Clamp(pos.y, 0f, 100f);
        obs[17] = Mathf.Clamp(distToTarget, 0f, 50f);

        return obs;
    }

    /// <summary>
    /// Berechnet den Reward für den aktuellen Zustand.
    /// </summary>
    public float CalculateReward()
    {
        if (droneController == null) return 0f;

        Vector3 pos = droneController.GetPosition();
        Vector3 vel = droneController.GetVelocity();
        Quaternion rot = droneController.GetRotation();

        // Position Error
        float posError = Vector3.Distance(pos, targetPosition);

        // Orientation Error (Abweichung von Level-Flug)
        Vector3 euler = rot.eulerAngles;
        float rollRad  = Mathf.Deg2Rad * WrapAngle(euler.z);
        float pitchRad = Mathf.Deg2Rad * WrapAngle(euler.x);
        float orientError = Mathf.Sqrt(rollRad * rollRad + pitchRad * pitchRad);

        // Velocity Penalty
        float velPenalty = vel.magnitude;

        // Energy Penalty (Summe der PWM-Werte)
        float energyPenalty = 0f;
        for (int i = 0; i < 4; i++)
            energyPenalty += currentMotorPWM[i];

        // Crash Check
        if (pos.y < crashHeight)
            return -crashPenalty;

        float reward =
            -rewardAlpha   * posError
            -rewardBeta    * orientError
            -rewardGamma   * velPenalty
            -rewardDelta   * energyPenalty
            + rewardEpsilon;

        return reward;
    }

    /// <summary>
    /// Prüft ob die Episode beendet ist (Crash, Out-of-Bounds, Timeout).
    /// </summary>
    public bool CheckDone()
    {
        if (droneController == null) return true;

        Vector3 pos = droneController.GetPosition();

        // Crash
        if (pos.y < crashHeight)
        {
            if (debugLog) Debug.Log("[RLEnv] Done: Crash");
            return true;
        }

        // Out of Bounds
        float dist = Vector3.Distance(pos, startPosition);
        if (dist > outOfBoundsRadius)
        {
            if (debugLog) Debug.Log("[RLEnv] Done: Out of Bounds");
            return true;
        }

        // Timeout
        if (Time.time - episodeStartTime > maxEpisodeDuration)
        {
            if (debugLog) Debug.Log("[RLEnv] Done: Timeout");
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Socket-Kommunikation (TCP JSON-Protokoll)
    // ─────────────────────────────────────────────────────────────────────────

    private void StartSocketServer()
    {
        socketRunning = true;
        socketThread = new System.Threading.Thread(SocketServerLoop)
        {
            IsBackground = true,
            Name = "RLSocketServer"
        };
        socketThread.Start();
        Debug.Log($"[RLEnv] TCP-Server gestartet auf Port {rlSocketPort}");
    }

    private void StopSocketServer()
    {
        socketRunning = false;
        try { connectedClient?.Close(); } catch { }
        try { tcpListener?.Stop(); } catch { }
        socketThread?.Join(500);
        Debug.Log("[RLEnv] TCP-Server gestoppt.");
    }

    private void SocketServerLoop()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Loopback, rlSocketPort);
            tcpListener.Start();

            while (socketRunning)
            {
                // Warte auf Verbindung
                if (tcpListener.Pending())
                {
                    connectedClient?.Close();
                    connectedClient = tcpListener.AcceptTcpClient();
                    networkStream = connectedClient.GetStream();
                    Debug.Log("[RLEnv] Python-Client verbunden.");
                }

                if (connectedClient != null && connectedClient.Connected && networkStream != null)
                {
                    // Lesen
                    if (networkStream.DataAvailable)
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            foreach (var line in msg.Split('\n'))
                            {
                                string trimmed = line.Trim();
                                if (trimmed.Length > 0)
                                    incomingMessages.Enqueue(trimmed);
                            }
                        }
                    }

                    // Schreiben
                    while (outgoingMessages.TryDequeue(out string outMsg))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(outMsg + "\n");
                        try { networkStream.Write(data, 0, data.Length); }
                        catch { break; }
                    }
                }

                System.Threading.Thread.Sleep(1);
            }
        }
        catch (Exception e)
        {
            if (socketRunning)
                Debug.LogError($"[RLEnv] Socket-Fehler: {e.Message}");
        }
    }

    private void ProcessIncomingMessages()
    {
        while (incomingMessages.TryDequeue(out string msg))
        {
            HandleMessage(msg);
        }
    }

    private void HandleMessage(string json)
    {
        // Minimaler JSON-Parser ohne externe Deps
        // Erwartet: {"cmd":"step","action":[a0,a1,a2,a3]} oder {"cmd":"reset"}
        try
        {
            if (json.Contains("\"cmd\":\"reset\""))
            {
                ResetEnvironment();
                return;
            }

            if (json.Contains("\"cmd\":\"step\""))
            {
                float[] action = ParseActionFromJson(json);
                if (action != null)
                    pendingAction = action;
                return;
            }

            if (json.Contains("\"cmd\":\"ping\""))
            {
                outgoingMessages.Enqueue("{\"type\":\"pong\"}");
                return;
            }

            Debug.LogWarning($"[RLEnv] Unbekannte Nachricht: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RLEnv] Fehler beim Verarbeiten der Nachricht: {e.Message}");
        }
    }

    private void SendStepResult(float[] obs, float reward, bool done)
    {
        string obsStr = FloatArrayToJson(obs);
        string msg = $"{{\"type\":\"step\",\"obs\":{obsStr},\"reward\":{reward:F6},\"done\":{(done ? "true" : "false")}}}";
        outgoingMessages.Enqueue(msg);
    }

    private void SendResetResult(float[] obs)
    {
        string obsStr = FloatArrayToJson(obs);
        string msg = $"{{\"type\":\"reset\",\"obs\":{obsStr}}}";
        outgoingMessages.Enqueue(msg);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hilfsmethoden
    // ─────────────────────────────────────────────────────────────────────────

    private float WrapAngle(float angle)
    {
        // Konvertiert Unity-Euler (0..360) zu (-180..180)
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private string FloatArrayToJson(float[] arr)
    {
        StringBuilder sb = new StringBuilder("[");
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(arr[i].ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append("]");
        return sb.ToString();
    }

    private float[] ParseActionFromJson(string json)
    {
        // Einfaches Parsing: "action":[a0,a1,a2,a3]
        int start = json.IndexOf("\"action\":[");
        if (start < 0) return null;

        start = json.IndexOf('[', start);
        int end = json.IndexOf(']', start);
        if (start < 0 || end < 0) return null;

        string inner = json.Substring(start + 1, end - start - 1);
        string[] parts = inner.Split(',');

        if (parts.Length < 4) return null;

        float[] action = new float[4];
        for (int i = 0; i < 4; i++)
        {
            if (!float.TryParse(parts[i].Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out action[i]))
                return null;
        }

        return action;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Zielposition
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        Gizmos.DrawLine(targetPosition - Vector3.up * 0.5f, targetPosition + Vector3.up * 0.5f);

        // Out-of-Bounds Radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(startPosition, outOfBoundsRadius);
    }
}
