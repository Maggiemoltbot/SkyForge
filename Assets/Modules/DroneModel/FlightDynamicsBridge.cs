using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;

public class FlightDynamicsBridge : MonoBehaviour
{
    [Header("Network Settings")]
    public string sitlIpAddress = "127.0.0.1"; // Adresse des SITL
    public int fdmPort = 9003; // Port für FDM-Daten (von SITL)
    public int rcInPort = 9004; // Port für RC-Eingabe (an SITL)

    [Header("Debug Options")]
    public bool disableUDP = false; // Für Tests ohne SITL

    [Header("References")]
    public DroneController droneController; // Referenz zum zu steuernden Drohnenobjekt

    private UdpClient fdmClient; // Client zum Empfangen von FDM-Daten
    private Thread receiveThread; // Thread für nicht-blockierenden Empfang
    private bool threadRunning = false; // Steuert den Thread

    private UdpClient rcOutClient; // Client zum Senden von RC-Daten

    // Datenstrukturen für die empfangenen Daten
    [System.Serializable]
    struct FdmPacket
    {
        public double timestamp;
        public float[] imu_angular_velocity_rpy; // rad/s (body frame)
        public float[] imu_linear_acceleration_xyz; // m/s² (NED, body frame)
        public float[] imu_orientation_quat; // w, x, y, z
        public float[] velocity_xyz; // m/s (ENU! für GPS-Modus, siehe Betaflight-Doku)
        public float[] position_xyz; // Lat, Lon, Alt (ENU Convention)
        public float pressure; // Pa (Barometer)
    }

    void Start()
    {
        if (disableUDP)
        {
            Debug.Log("[FlightDynamicsBridge] UDP deaktiviert. Keine Verbindung zu SITL.");
            return;
        }

        if (droneController == null)
        {
            Debug.LogError("[FlightDynamicsBridge] Kein gültiges DroneController-Objekt zugewiesen!");
            return;
        }

        Debug.Log("[FlightDynamicsBridge] Initialisiere UDP-Clienten für SITL... (FDMPort: " + fdmPort + ", RCInPort: " + rcInPort + ")");
        try
        {
            // UDP Client für FDM-Pakete (SITL -> Unity)
            fdmClient = new UdpClient(fdmPort, AddressFamily.InterNetwork);
            Debug.Log("[FlightDynamicsBridge] FDM-Empfangs-Client auf Port " + fdmPort + " gestartet.");

            // UDP Client zum Senden von RC-Daten (Unity -> SITL)
            rcOutClient = new UdpClient(AddressFamily.InterNetwork);
            Debug.Log("[FlightDynamicsBridge] RC-Absende-Client initialisiert.");

            // Thread starten, um FDM-Daten asynchron zu empfangen
            threadRunning = true;
            receiveThread = new Thread(new ThreadStart(ReceiveFdmData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("[FlightDynamicsBridge] Receive-Thread gestartet.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FlightDynamicsBridge] Fehler beim Starten der UDP-Clients: " + e.Message);
        }
    }

    void Update()
    {
        // In Update() können die verarbeiteten FDM-Daten auf die Drohne angewendet werden
        // (Die tatsächliche Anwendung geschieht in ProcessFdmPacket(), welches aus dem Thread gesendet wird)
        // Hier kann z.B. der Status der Verbindung überwacht werden
    }

    void OnDestroy()
    {
        // Räume die UDP-Ressourcen sauber auf
        threadRunning = false;
        if (receiveThread != null)
        {
            receiveThread.Join();
        }

        if (fdmClient != null)
        {
            fdmClient.Close();
            fdmClient = null;
        }

        if (rcOutClient != null)
        {
            rcOutClient.Close();
            rcOutClient = null;
        }

        Debug.Log("[FlightDynamicsBridge] Ressourcen wurden freigegeben.");
    }

    // Hintergrund-Thread für den UDP-Empfang
    void ReceiveFdmData()
    {
        while (threadRunning)
        {
            try
            {
                IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = fdmClient.Receive(ref remoteIp); // Blockiert, solange keine Daten empfangen werden

                ProcessFdmPacket(data);
            }
            catch (System.Threading.ThreadAbortException)
            {
                // Thread wurde gezielt beendet, normales Verhalten
                break;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FlightDynamicsBridge] Fehler beim Empfangen von FDM-Daten: " + e.Message);
            }
        }
    }

    // Verarbeiten des empfangenen FDM-Pakets
    void ProcessFdmPacket(byte[] data)
    {
        // Hier sollte eine robuste Serialisierung stattfinden. Die folgende Implementierung ist für Demonstrationszwecke stark vereinfacht.
        // Sie geht von einer festen Struktur und Little-Endian Byte-Reihenfolge aus.

        if (data.Length < (8 + 3*4 + 4*4 + 4*4 + 3*4 + 3*4 + 4))
        {
            Debug.LogWarning("[FlightDynamicsBridge] Empfangenes FDM-Paket hat unerwartete Länge (" + data.Length + "), Erwartet mindestens 64 Bytes.");
            return;
        }
        
        FdmPacket packet = new FdmPacket();
        int offset = 0;

        packet.timestamp = System.BitConverter.ToDouble(data, offset); offset += 8;
        packet.imu_angular_velocity_rpy = new float[3]; 
        packet.imu_linear_acceleration_xyz = new float[3]; 
        packet.imu_orientation_quat = new float[4]; 
        packet.velocity_xyz = new float[3]; 
        packet.position_xyz = new float[3];

        for (int i = 0; i < 3; i++) packet.imu_angular_velocity_rpy[i] = System.BitConverter.ToSingle(data, offset + i*4); offset += 12;
        for (int i = 0; i < 3; i++) packet.imu_linear_acceleration_xyz[i] = System.BitConverter.ToSingle(data, offset + i*4); offset += 12;
        for (int i = 0; i < 4; i++) packet.imu_orientation_quat[i] = System.BitConverter.ToSingle(data, offset + i*4); offset += 16;
        for (int i = 0; i < 3; i++) packet.velocity_xyz[i] = System.BitConverter.ToSingle(data, offset + i*4); offset += 12;
        for (int i = 0; i < 3; i++) packet.position_xyz[i] = System.BitConverter.ToSingle(data, offset + i*4); offset += 12;
        packet.pressure = System.BitConverter.ToSingle(data, offset); // offset += 4;

        // Debug.Log("[FlightDynamicsBridge] FDM-Paket empfangen: Timestamp=" + packet.timestamp + " Pos: " + packet.position_xyz[0] + "," + packet.position_xyz[1] + "," + packet.position_xyz[2]);

        // Wichtig: Die Position (position_xyz) in Betaflight SITL ist in ENU (East, North, Up) und repräsentiert Lat, Lon, Alt.
        // Für eine Simulation in geografischen Koordinaten wäre ein WGS84 zu UTM/Lokal-Koordinaten-Transform nötig.
        // Für eine rein lokale Simulation interpretieren wir die Werte als lokale m-Koordinaten.


        // Wichtig: Die Geschwindigkeit (velocity_xyz) ist in den Einheiten Ve, Vn, Vup (East, North, Up).
        // Diese müssen ggf. in das lokale Koordinatensystem der Drohne transformiert werden.


        // Hier werden die Daten auf das DroneController-Objekt angewendet
        // In einem realen Setup würde man hier z.B. die Rigidbody-Komponente beeinflussen.
        // Für diese Demo-Implementierung wird ein einfaches Teleport-Skript verwendet.

        if (droneController != null)
        {
            droneController.position = new Vector3(packet.position_xyz[0], packet.position_xyz[2], packet.position_xyz[1]); // ENU (x,y,z) = (East, Up, North)
            droneController.velocity = new Vector3(packet.velocity_xyz[0], packet.velocity_xyz[2], packet.velocity_xyz[1]); // Ve, Vn, Vup als (x, z, y)

            // Quaternion: [w, x, y, z] (Quaternion w in Unity an erster Stelle)
            // Im Betaflight-Packet ist die Reihenfolge [w,x,y,z], also direkt kompatibel
            droneController.orientation = new Quaternion(packet.imu_orientation_quat[1], packet.imu_orientation_quat[2], packet.imu_orientation_quat[3], packet.imu_orientation_quat[0]);

            // Debug.Log("[FlightDynamicsBridge] Drohne aktualisiert: Pos=" + droneController.position + " Rot=" + droneController.orientation);
        }
    }

    // Öffentliche Methode, um RC-Kanaldaten an das SITL zu senden
    // Dies wird typischerweise vom RCInputBridge-Skript aufgerufen
    public void SendRCChannels(int[] channels)
    {
        if (disableUDP || rcOutClient == null)
        {
            return; // Verhindere Fehler, wenn UDP deaktiviert ist
        }

        if (channels.Length > 16)
        {
            Debug.LogWarning("[FlightDynamicsBridge] SendRCChannels: Zu viele Kanäle (" + channels.Length + ")! Nur die ersten 16 werden gesendet.");
            System.Array.Resize(ref channels, 16);
        }

        try
        {
            // Erstelle das RC-Paket
            // Laut Betaflight-Dokumentation: https://www.betaflight.com/docs/development/autopilot/SITL_Autopilot_Testing_Gazebo
            // struct rc_packet {
            //     double timestamp;
            //     uint16_t channels[16];
            // };

            int packetSize = 8 + 16 * 2; // 8 Byte für timestamp, 2 Byte (uint16) pro Kanal
            byte[] packet = new byte[packetSize];
            int offset = 0;

            // Timestamp
            byte[] timestampBytes = System.BitConverter.GetBytes(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds);
            System.Buffer.BlockCopy(timestampBytes, 0, packet, offset, 8); offset += 8;

            // RC-Kanäle
            for (int i = 0; i < channels.Length && i < 16; i++)
            {
                // Konvertiere den Wert, wenn nötig (Unity verwendet typischerweise 0.0 - 1.0, Betaflight 1000-2000)
                // Annahme: Eingangswert in [0.0, 1.0]
                ushort channelValue = (ushort)(channels[i]); // Direktes Casting; in der Realität müsste hier die Konvertierung (0-1 -> 1000-2000) erfolgen
                byte[] channelBytes = System.BitConverter.GetBytes(channelValue);
                System.Buffer.BlockCopy(channelBytes, 0, packet, offset, 2); offset += 2;
            }

            // Fülle den Rest mit z.B. 1500 (Neutral)
            byte[] neutralBytes = System.BitConverter.GetBytes((ushort)1500);
            for (int i = channels.Length; i < 16; i++)
            {
                System.Buffer.BlockCopy(neutralBytes, 0, packet, offset, 2); offset += 2;
            }

            // Sende das Packet an den SITL (anstelle einer IP wird hier eine Endpoint verwendet)
            // Betaflight SITL hört normalerweise auf 127.0.0.1:9004 für RC-Eingabe
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(sitlIpAddress), rcInPort);
            rcOutClient.Send(packet, packet.Length, remoteEndPoint);
            // Debug.Log("[FlightDynamicsBridge] RC-Datensatz gesendet: " + channels.Length + " Kanäle.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FlightDynamicsBridge] Fehler beim Senden von RC-Daten: " + e.Message);
        }
    }
}
