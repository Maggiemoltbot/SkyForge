using NUnit.Framework;
using UnityEngine;
using System.Collections;

/// <summary>
/// Unit Tests für DroneRLEnvironment.
/// 
/// Testet:
/// - Observation Space Dimensionen
/// - Action Space Grenzen
/// - Reward-Berechnung
/// - Done-Bedingungen
/// - Reset-Verhalten
/// 
/// Quality Gate: G2 (Unit Tests grün)
/// </summary>
[TestFixture]
public class RLEnvironmentTests
{
    private GameObject envGO;
    private GameObject droneGO;
    private DroneRLEnvironment rlEnv;
    private DroneController droneController;

    [SetUp]
    public void SetUp()
    {
        // Drone aufbauen
        droneGO = new GameObject("TestDrone");
        droneGO.AddComponent<Rigidbody>();
        droneController = droneGO.AddComponent<DroneController>();

        // RL Environment aufbauen
        envGO = new GameObject("RLEnvironment");
        rlEnv = envGO.AddComponent<DroneRLEnvironment>();

        // DroneController zuweisen (via Reflection, da private SerializeField)
        var field = typeof(DroneRLEnvironment).GetField(
            "droneController",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        field?.SetValue(rlEnv, droneController);

        // Socket-Server NICHT starten (kein Start() in Tests)
    }

    [TearDown]
    public void TearDown()
    {
        if (envGO != null)
            Object.DestroyImmediate(envGO);
        if (droneGO != null)
            Object.DestroyImmediate(droneGO);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Observation Space Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void GetObservation_Returns18DimensionalVector()
    {
        float[] obs = rlEnv.GetObservation();
        Assert.AreEqual(18, obs.Length,
            "Observation Space muss 18-dimensional sein.");
    }

    [Test]
    public void GetObservation_AtStartPosition_HeightIsCorrect()
    {
        // Drohne auf Startposition (Y=1.0 default)
        droneController.transform.position = new Vector3(0f, 1f, 0f);

        float[] obs = rlEnv.GetObservation();

        // obs[16] = Höhe über Boden
        Assert.AreEqual(1.0f, obs[16], 0.01f,
            "Höhe (obs[16]) muss der Y-Position entsprechen.");
    }

    [Test]
    public void GetObservation_MotorPWM_InitiallyZero()
    {
        float[] obs = rlEnv.GetObservation();

        // obs[12-15] = Motor PWM Werte
        for (int i = 12; i <= 15; i++)
        {
            Assert.AreEqual(0f, obs[i], 0.0001f,
                $"Motor-PWM obs[{i}] muss initial 0 sein.");
        }
    }

    [Test]
    public void GetObservation_AllValuesAreFinite()
    {
        float[] obs = rlEnv.GetObservation();

        for (int i = 0; i < obs.Length; i++)
        {
            Assert.IsFalse(float.IsNaN(obs[i]),
                $"obs[{i}] darf nicht NaN sein.");
            Assert.IsFalse(float.IsInfinity(obs[i]),
                $"obs[{i}] darf nicht Infinity sein.");
        }
    }

    [Test]
    public void GetObservation_Position_IsClampedTo50m()
    {
        // Drohne weit weg positionieren
        droneController.transform.position = new Vector3(100f, 100f, 100f);

        float[] obs = rlEnv.GetObservation();

        // obs[0-2] = relative Position zum Ziel (geclampt auf [-50, 50])
        Assert.LessOrEqual(Mathf.Abs(obs[0]), 50f, "obs[0] (relX) muss auf ±50m geclampt sein.");
        Assert.LessOrEqual(Mathf.Abs(obs[1]), 50f, "obs[1] (relY) muss auf ±50m geclampt sein.");
        Assert.LessOrEqual(Mathf.Abs(obs[2]), 50f, "obs[2] (relZ) muss auf ±50m geclampt sein.");
    }

    [Test]
    public void GetObservation_Distance_IsNonNegative()
    {
        float[] obs = rlEnv.GetObservation();

        // obs[17] = Distanz zum Ziel
        Assert.GreaterOrEqual(obs[17], 0f,
            "Distanz (obs[17]) muss nicht-negativ sein.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Action Space Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ApplyAction_ValidAction_DoesNotThrow()
    {
        float[] action = { 0.1f, 0.1f, 0.1f, 0.1f };
        Assert.DoesNotThrow(() => rlEnv.ApplyAction(action),
            "ApplyAction mit gültiger Action darf keine Exception werfen.");
    }

    [Test]
    public void ApplyAction_NullAction_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => rlEnv.ApplyAction(null),
            "ApplyAction mit null darf keine Exception werfen (defensive Implementierung).");
    }

    [Test]
    public void ApplyAction_ExtremePositiveAction_ClampsPWMToOne()
    {
        // Maximale positive Action wiederholt anwenden
        float[] action = { 1.0f, 1.0f, 1.0f, 1.0f };
        for (int i = 0; i < 20; i++)
            rlEnv.ApplyAction(action);

        // Motor-PWM muss geclampt sein (max 1.0)
        for (int i = 0; i < 4; i++)
        {
            Assert.LessOrEqual(droneController.motorPWM[i], 1.0f,
                $"Motor {i} PWM darf nicht > 1.0 sein.");
        }
    }

    [Test]
    public void ApplyAction_ExtremeNegativeAction_ClampsPWMToZero()
    {
        // Maximale negative Action wiederholt anwenden
        float[] action = { -1.0f, -1.0f, -1.0f, -1.0f };
        for (int i = 0; i < 20; i++)
            rlEnv.ApplyAction(action);

        // Motor-PWM muss geclampt sein (min 0.0)
        for (int i = 0; i < 4; i++)
        {
            Assert.GreaterOrEqual(droneController.motorPWM[i], 0.0f,
                $"Motor {i} PWM darf nicht < 0.0 sein.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reward Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void CalculateReward_AtTarget_IsHigherThanFarAway()
    {
        // Drohne am Ziel (default: 0,5,0)
        droneController.transform.position = new Vector3(0f, 5f, 0f);
        float rewardAtTarget = rlEnv.CalculateReward();

        // Drohne weit weg
        droneController.transform.position = new Vector3(20f, 5f, 0f);
        float rewardFarAway = rlEnv.CalculateReward();

        Assert.Greater(rewardAtTarget, rewardFarAway,
            "Reward am Ziel muss größer sein als Reward weit weg.");
    }

    [Test]
    public void CalculateReward_BelowCrashHeight_ReturnsCrashPenalty()
    {
        // Drohne unter crashHeight (default: -0.1m)
        droneController.transform.position = new Vector3(0f, -1f, 0f);

        float reward = rlEnv.CalculateReward();

        // Crash-Penalty ist negativ und groß
        Assert.Less(reward, -50f,
            "Reward bei Crash muss stark negativ sein (Crash-Penalty).");
    }

    [Test]
    public void CalculateReward_IsFinite()
    {
        float reward = rlEnv.CalculateReward();
        Assert.IsFalse(float.IsNaN(reward), "Reward darf nicht NaN sein.");
        Assert.IsFalse(float.IsInfinity(reward), "Reward darf nicht Infinity sein.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Done-Bedingung Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void CheckDone_AtStartPosition_IsFalse()
    {
        droneController.transform.position = new Vector3(0f, 1f, 0f);

        bool done = rlEnv.CheckDone();

        Assert.IsFalse(done,
            "Episode darf nicht beendet sein wenn Drohne auf Startposition ist.");
    }

    [Test]
    public void CheckDone_BelowCrashHeight_IsTrue()
    {
        droneController.transform.position = new Vector3(0f, -1f, 0f);

        bool done = rlEnv.CheckDone();

        Assert.IsTrue(done,
            "Episode muss beendet sein wenn Drohne unter crashHeight ist.");
    }

    [Test]
    public void CheckDone_OutOfBounds_IsTrue()
    {
        // Drohne weit außerhalb des Out-of-Bounds-Radius (default: 50m)
        droneController.transform.position = new Vector3(100f, 1f, 0f);

        bool done = rlEnv.CheckDone();

        Assert.IsTrue(done,
            "Episode muss beendet sein wenn Drohne out-of-bounds ist.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reset Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ResetEnvironment_DronePosition_IsAtStartPosition()
    {
        // Drohne wegbewegen
        droneController.transform.position = new Vector3(10f, 20f, 30f);

        // Reset aufrufen (ohne Socket-Kommunikation)
        // Wir testen nur die physische Reset-Funktionalität
        droneController.ResetDrone();

        // DroneController.ResetDrone setzt auf Vector3.zero
        Assert.AreEqual(Vector3.zero, droneController.transform.position,
            "Drohne muss nach Reset auf Startposition sein.");
    }

    [Test]
    public void ResetEnvironment_DroneMotors_AreZero()
    {
        // Motoren setzen
        for (int i = 0; i < 4; i++)
            droneController.motorPWM[i] = 0.8f;

        droneController.ResetDrone();

        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(0f, droneController.motorPWM[i], 0.0001f,
                $"Motor {i} muss nach Reset auf 0 sein.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Observation Space Konsistenz-Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ObservationSpace_Dimension_Matches_RLTrainingContextSpec()
    {
        // Laut CONTEXT.md: 18-dim Observation Space
        float[] obs = rlEnv.GetObservation();
        Assert.AreEqual(18, obs.Length,
            "Observation Space muss 18-dimensional sein (gemäß RLTraining CONTEXT.md).");
    }

    [Test]
    public void ActionSpace_Dimension_Matches_RLTrainingContextSpec()
    {
        // Laut CONTEXT.md: 4-dim Action Space
        float[] action = new float[4];
        Assert.DoesNotThrow(() => rlEnv.ApplyAction(action),
            "4-dimensionale Action muss akzeptiert werden.");
    }
}
