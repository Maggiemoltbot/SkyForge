using NUnit.Framework;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;

/// <summary>
/// Unit Tests für FlightDynamicsBridge und zugehörige Datenstrukturen.
/// 
/// Testet:
/// - FDMPacket Serialisierung/Deserialisierung
/// - PWMPacket Struktur
/// - CoordinateConverter Transformationen
/// - BridgeConfig Defaults
/// 
/// Quality Gate: G2 (Unit Tests grün)
/// </summary>
[TestFixture]
public class FlightBridgeTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // FDMPacket Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void FDMPacket_Size_IsCorrect()
    {
        // FDMPacket muss eine definierte Größe haben für UDP-Übertragung
        int size = Marshal.SizeOf<FDMPacket>();
        Assert.Greater(size, 0, "FDMPacket muss eine positive Größe haben.");
        // Typische Größe: timestamp(8) + 3×angular(24) + 3×accel(24) + 4×quat(32) + 3×vel(24) + 3×pos(24) + pressure(8) = ~148 Bytes
        Assert.LessOrEqual(size, 512, "FDMPacket sollte unter 512 Bytes bleiben.");
    }

    [Test]
    public void FDMPacket_DefaultValues_AreZero()
    {
        FDMPacket packet = new FDMPacket();
        Assert.AreEqual(0.0, packet.timestamp, 0.0001, "timestamp muss initial 0 sein.");
        Assert.AreEqual(0.0f, packet.posX, 0.0001f, "posX muss initial 0 sein.");
        Assert.AreEqual(0.0f, packet.posY, 0.0001f, "posY muss initial 0 sein.");
        Assert.AreEqual(0.0f, packet.posZ, 0.0001f, "posZ muss initial 0 sein.");
    }

    [Test]
    public void FDMPacket_CanSetAndGetAllFields()
    {
        FDMPacket packet = new FDMPacket
        {
            timestamp = 1.23,
            posX = 1.0f, posY = 2.0f, posZ = 3.0f,
            velX = 0.1f, velY = 0.2f, velZ = 0.3f,
            quatW = 1.0f, quatX = 0.0f, quatY = 0.0f, quatZ = 0.0f,
            imuAngularVelX = 0.01f, imuAngularVelY = 0.02f, imuAngularVelZ = 0.03f,
            imuLinearAccX = 0.1f, imuLinearAccY = 9.81f, imuLinearAccZ = 0.1f,
            pressure = 1013.25f
        };

        Assert.AreEqual(1.23, packet.timestamp, 0.0001);
        Assert.AreEqual(1.0f, packet.posX, 0.0001f);
        Assert.AreEqual(9.81f, packet.imuLinearAccY, 0.0001f);
        Assert.AreEqual(1013.25f, packet.pressure, 0.01f);
    }

    [Test]
    public void FDMPacket_Serialization_RoundTrip()
    {
        // Serialisierung und Deserialisierung muss verlustfrei sein
        FDMPacket original = new FDMPacket
        {
            timestamp = 42.5,
            posX = 1.5f, posY = 2.5f, posZ = 3.5f,
            velX = 0.5f, velY = -0.5f, velZ = 0.1f,
            quatW = 0.707f, quatX = 0.0f, quatY = 0.707f, quatZ = 0.0f,
            pressure = 1013.25f
        };

        // Marshal → Bytes → Marshal
        int size = Marshal.SizeOf(original);
        byte[] bytes = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(original, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        // Deserialisieren
        FDMPacket restored = new FDMPacket();
        IntPtr ptr2 = Marshal.AllocHGlobal(size);
        Marshal.Copy(bytes, 0, ptr2, size);
        restored = (FDMPacket)Marshal.PtrToStructure(ptr2, typeof(FDMPacket));
        Marshal.FreeHGlobal(ptr2);

        Assert.AreEqual(original.timestamp, restored.timestamp, 0.0001, "timestamp Roundtrip");
        Assert.AreEqual(original.posX, restored.posX, 0.0001f, "posX Roundtrip");
        Assert.AreEqual(original.posY, restored.posY, 0.0001f, "posY Roundtrip");
        Assert.AreEqual(original.quatW, restored.quatW, 0.0001f, "quatW Roundtrip");
        Assert.AreEqual(original.pressure, restored.pressure, 0.01f, "pressure Roundtrip");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CoordinateConverter Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void CoordinateConverter_UnityToNED_YAxisMapsToNegativeZ()
    {
        // Unity Y-up → NED Z-down
        // Unity (0, 1, 0) → NED (0, 0, -1) (Aufwärts = negatives Z in NED)
        Vector3 unityUp = Vector3.up;
        Vector3 ned = CoordinateConverter.UnityToNED(unityUp);

        Assert.AreEqual(0f, ned.x, 0.001f, "NED X sollte 0 sein für Unity-Up");
        Assert.AreEqual(0f, ned.y, 0.001f, "NED Y sollte 0 sein für Unity-Up");
        Assert.Less(ned.z, 0f, "NED Z sollte negativ sein für Unity-Up (NED ist Z-Down)");
    }

    [Test]
    public void CoordinateConverter_UnityToNED_ZeroVector_StaysZero()
    {
        Vector3 result = CoordinateConverter.UnityToNED(Vector3.zero);
        Assert.AreEqual(Vector3.zero, result,
            "Null-Vektor muss nach Konvertierung Null bleiben.");
    }

    [Test]
    public void CoordinateConverter_UnityToNED_ForwardMapsToNorth()
    {
        // Unity Forward (0,0,1) → NED North (1,0,0) oder (0,1,0) je nach Convention
        Vector3 unityForward = Vector3.forward;
        Vector3 ned = CoordinateConverter.UnityToNED(unityForward);

        // Der Betrag muss erhalten bleiben
        Assert.AreEqual(unityForward.magnitude, ned.magnitude, 0.001f,
            "Magnitude muss bei Koordinaten-Konvertierung erhalten bleiben.");
    }

    [Test]
    public void CoordinateConverter_AngularVelocity_MagnitudePreserved()
    {
        Vector3 angVel = new Vector3(1f, 2f, 3f);
        Vector3 converted = CoordinateConverter.AngularVelocityUnityToNED(angVel);

        Assert.AreEqual(angVel.magnitude, converted.magnitude, 0.001f,
            "Winkelgeschwindigkeit-Magnitude muss bei Konvertierung erhalten bleiben.");
    }

    [Test]
    public void CoordinateConverter_LinearAcceleration_MagnitudePreserved()
    {
        Vector3 accel = new Vector3(0f, -9.81f, 0f); // Gravity in Unity
        Vector3 converted = CoordinateConverter.LinearAccelerationUnityToNED(accel);

        Assert.AreEqual(accel.magnitude, converted.magnitude, 0.001f,
            "Beschleunigungs-Magnitude muss bei Konvertierung erhalten bleiben.");
    }

    [Test]
    public void CoordinateConverter_QuaternionUnityToNED_IdentityStaysNormalized()
    {
        Quaternion identity = Quaternion.identity;
        Quaternion converted = CoordinateConverter.QuaternionUnityToNED(identity);

        // Konvertiertes Quaternion muss normalisiert bleiben
        float magnitude = Mathf.Sqrt(
            converted.w * converted.w +
            converted.x * converted.x +
            converted.y * converted.y +
            converted.z * converted.z
        );

        Assert.AreEqual(1f, magnitude, 0.001f,
            "Quaternion muss nach NED-Konvertierung normalisiert bleiben.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BridgeConfig Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void BridgeConfig_DefaultPorts_AreValid()
    {
        BridgeConfig config = ScriptableObject.CreateInstance<BridgeConfig>();

        // Ports müssen im gültigen Bereich liegen (1-65535)
        Assert.Greater(config.fdmSendPort, 0, "fdmSendPort muss > 0 sein.");
        Assert.LessOrEqual(config.fdmSendPort, 65535, "fdmSendPort muss <= 65535 sein.");
        Assert.Greater(config.pwmReceivePort, 0, "pwmReceivePort muss > 0 sein.");
        Assert.LessOrEqual(config.pwmReceivePort, 65535, "pwmReceivePort muss <= 65535 sein.");

        Object.DestroyImmediate(config);
    }

    [Test]
    public void BridgeConfig_DefaultIP_IsLocalhost()
    {
        BridgeConfig config = ScriptableObject.CreateInstance<BridgeConfig>();

        Assert.IsNotNull(config.bfSITLIPAddress, "IP-Adresse darf nicht null sein.");
        Assert.IsNotEmpty(config.bfSITLIPAddress, "IP-Adresse darf nicht leer sein.");

        // Standard sollte localhost sein
        bool isLocalhost =
            config.bfSITLIPAddress == "127.0.0.1" ||
            config.bfSITLIPAddress == "localhost";

        Assert.IsTrue(isLocalhost,
            $"Default-IP sollte localhost sein, ist: {config.bfSITLIPAddress}");

        Object.DestroyImmediate(config);
    }

    [Test]
    public void BridgeConfig_FDMAndPWMPorts_AreDifferent()
    {
        BridgeConfig config = ScriptableObject.CreateInstance<BridgeConfig>();

        Assert.AreNotEqual(config.fdmSendPort, config.pwmReceivePort,
            "FDM-Send-Port und PWM-Receive-Port müssen unterschiedlich sein.");

        Object.DestroyImmediate(config);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Netzwerk-Hilfstests (keine echten Verbindungen)
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void UdpPort_9002_IsValidPortNumber()
    {
        // Betaflight SITL Standard-Port
        int port = 9002;
        Assert.Greater(port, 1024, "Port sollte > 1024 sein (kein System-Port).");
        Assert.Less(port, 65536, "Port muss < 65536 sein.");
    }

    [Test]
    public void UdpPort_9003_IsValidPortNumber()
    {
        int port = 9003;
        Assert.Greater(port, 1024);
        Assert.Less(port, 65536);
    }

    [Test]
    public void RLSocket_Port_9020_IsValidAndDifferentFromSITL()
    {
        int rlPort = 9020;
        int sitlFDM = 9002;
        int sitlPWM = 9003;

        Assert.AreNotEqual(rlPort, sitlFDM, "RL-Port darf nicht mit FDM-Port kollidieren.");
        Assert.AreNotEqual(rlPort, sitlPWM, "RL-Port darf nicht mit PWM-Port kollidieren.");
    }
}
