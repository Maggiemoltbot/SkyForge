using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Unit Tests für DroneController und DroneModel.
/// 
/// Testet:
/// - DroneController Initialisierung
/// - ResetDrone() Funktionalität
/// - MotorModel Berechnungen
/// - Observation-Getter
/// 
/// Quality Gate: G2 (Unit Tests grün)
/// </summary>
[TestFixture]
public class DroneModelTests
{
    private GameObject droneGO;
    private DroneController droneController;
    private DroneConfig droneConfig;

    [SetUp]
    public void SetUp()
    {
        // Drone GameObject mit Rigidbody und DroneController aufbauen
        droneGO = new GameObject("TestDrone");
        droneGO.AddComponent<Rigidbody>();
        droneController = droneGO.AddComponent<DroneController>();

        // DroneConfig erstellen (ScriptableObject)
        droneConfig = ScriptableObject.CreateInstance<DroneConfig>();
        droneConfig.mass = 0.5f;
        droneConfig.armLength = 0.12f;
        droneConfig.maxThrustPerMotor = 5.0f;
        droneConfig.dragCoefficient = 0.1f;
        droneConfig.angularDragCoefficient = 0.05f;
    }

    [TearDown]
    public void TearDown()
    {
        if (droneGO != null)
            Object.DestroyImmediate(droneGO);
        if (droneConfig != null)
            Object.DestroyImmediate(droneConfig);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MotorModel Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void MotorModel_CalculateThrust_ZeroPWM_ReturnsZero()
    {
        float thrust = MotorModel.CalculateThrust(0f, 5.0f);
        Assert.AreEqual(0f, thrust, 0.0001f,
            "Thrust bei PWM=0 muss 0 sein.");
    }

    [Test]
    public void MotorModel_CalculateThrust_FullPWM_ReturnsMaxThrust()
    {
        float maxThrust = 5.0f;
        float thrust = MotorModel.CalculateThrust(1f, maxThrust);
        Assert.AreEqual(maxThrust, thrust, 0.0001f,
            "Thrust bei PWM=1 muss maxThrust sein.");
    }

    [Test]
    public void MotorModel_CalculateThrust_HalfPWM_IsQuadratic()
    {
        float maxThrust = 4.0f;
        float thrust = MotorModel.CalculateThrust(0.5f, maxThrust);
        // Erwartung: 4.0 * 0.5 * 0.5 = 1.0
        Assert.AreEqual(1.0f, thrust, 0.0001f,
            "Thrust bei PWM=0.5 muss quadratisch sein (maxThrust * 0.25).");
    }

    [Test]
    public void MotorModel_GetMotorPositions_ReturnsCorrectCount()
    {
        Vector3[] positions = MotorModel.GetMotorPositions(0.12f);
        Assert.AreEqual(4, positions.Length,
            "MotorModel muss genau 4 Motor-Positionen zurückgeben.");
    }

    [Test]
    public void MotorModel_GetMotorPositions_FrontLeftIsPositiveXZ()
    {
        float arm = 0.12f;
        Vector3[] positions = MotorModel.GetMotorPositions(arm);
        // FrontLeft: (+armLength, 0, +armLength)
        Vector3 fl = positions[0];
        Assert.Greater(fl.x, 0f, "FrontLeft X muss positiv sein.");
        Assert.Greater(fl.z, 0f, "FrontLeft Z muss positiv sein.");
        Assert.AreEqual(0f, fl.y, 0.0001f, "FrontLeft Y muss 0 sein.");
    }

    [Test]
    public void MotorModel_RemapSitlMotorOrder_MapsCorrectly()
    {
        float[] target = new float[4];
        MotorModel.RemapSitlMotorOrder(target,
            motor1: 0.1f,  // BF1 → BackRight (index 3)
            motor2: 0.2f,  // BF2 → BackLeft  (index 2)
            motor3: 0.3f,  // BF3 → FrontLeft (index 0)
            motor4: 0.4f   // BF0 → FrontRight(index 1)
        );

        Assert.AreEqual(0.3f, target[0], 0.0001f, "FrontLeft  ← BF3 (motor3=0.3)");
        Assert.AreEqual(0.4f, target[1], 0.0001f, "FrontRight ← BF0 (motor4=0.4)");
        Assert.AreEqual(0.2f, target[2], 0.0001f, "BackLeft   ← BF2 (motor2=0.2)");
        Assert.AreEqual(0.1f, target[3], 0.0001f, "BackRight  ← BF1 (motor1=0.1)");
    }

    [Test]
    public void MotorModel_RemapSitlMotorOrder_NullTarget_LogsError()
    {
        // Erwartet eine LogError-Meldung, keinen Crash
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
        MotorModel.RemapSitlMotorOrder(null, 0.1f, 0.2f, 0.3f, 0.4f);
    }

    [Test]
    public void MotorModel_CalculateTorque_SymmetricThrust_YawTorqueIsZero()
    {
        // Bei identischen Thrusts auf allen Motoren hebt sich das Drehmoment auf
        float[] thrusts = { 1f, 1f, 1f, 1f };
        bool[] directions = { true, false, false, true }; // FL=CW, FR=CCW, BL=CCW, BR=CW
        Vector3 torque = MotorModel.CalculateTorque(thrusts, 0.12f, directions);

        // Y-Torque (Yaw) muss bei symmetrischen Thrusts nahe 0 sein
        // (CW und CCW heben sich auf)
        Assert.AreEqual(0f, torque.y, 0.01f,
            "Yaw-Torque muss bei symmetrischen Thrusts ~0 sein.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DroneController Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void DroneController_MotorPWM_InitiallyZero()
    {
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(0f, droneController.motorPWM[i], 0.0001f,
                $"Motor {i} PWM muss initial 0 sein.");
        }
    }

    [Test]
    public void DroneController_ResetDrone_SetsPositionToZero()
    {
        // Drohne bewegen
        droneController.transform.position = new Vector3(5f, 10f, 3f);
        droneController.transform.rotation = Quaternion.Euler(45f, 90f, 30f);

        droneController.ResetDrone();

        Assert.AreEqual(Vector3.zero, droneController.transform.position,
            "Position nach Reset muss Vector3.zero sein.");
    }

    [Test]
    public void DroneController_ResetDrone_SetsRotationToIdentity()
    {
        droneController.transform.rotation = Quaternion.Euler(45f, 90f, 30f);

        droneController.ResetDrone();

        Assert.AreEqual(Quaternion.identity, droneController.transform.rotation,
            "Rotation nach Reset muss Quaternion.identity sein.");
    }

    [Test]
    public void DroneController_ResetDrone_ResetsMotorPWM()
    {
        // Motoren setzen
        droneController.motorPWM[0] = 0.8f;
        droneController.motorPWM[1] = 0.7f;
        droneController.motorPWM[2] = 0.9f;
        droneController.motorPWM[3] = 0.6f;

        droneController.ResetDrone();

        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(0f, droneController.motorPWM[i], 0.0001f,
                $"Motor {i} PWM muss nach Reset 0 sein.");
        }
    }

    [Test]
    public void DroneController_GetPosition_ReturnsTransformPosition()
    {
        Vector3 expected = new Vector3(1f, 2f, 3f);
        droneController.transform.position = expected;

        Vector3 actual = droneController.GetPosition();

        Assert.AreEqual(expected, actual,
            "GetPosition() muss transform.position zurückgeben.");
    }

    [Test]
    public void DroneController_GetRotation_ReturnsTransformRotation()
    {
        Quaternion expected = Quaternion.Euler(10f, 20f, 30f);
        droneController.transform.rotation = expected;

        Quaternion actual = droneController.GetRotation();

        Assert.That(Quaternion.Angle(expected, actual), Is.LessThan(0.01f),
            "GetRotation() muss transform.rotation zurückgeben.");
    }

    [Test]
    public void DroneController_CurrentPosition_MatchesGetPosition()
    {
        droneController.transform.position = new Vector3(4f, 5f, 6f);

        Assert.AreEqual(droneController.GetPosition(), droneController.CurrentPosition,
            "CurrentPosition und GetPosition() müssen identisch sein.");
    }

    [Test]
    public void DroneController_CurrentPressure_IsStandardAtmosphere()
    {
        // Standard Sea Level Pressure = 1013.25 hPa
        Assert.AreEqual(1013.25f, droneController.CurrentPressure, 0.01f,
            "CurrentPressure muss Standard-Atmosphärendruck sein.");
    }
}
