# Architekt-Plan: Hochfrequente Physik-Simulation

## Ziel
Physics-Update-Rate von 60 Hz (Framerate) auf 1-8 kHz anheben für realistischen BF-SITL-Flug.

## Phase 1: FixedUpdate @ 1 kHz (Quick Win)

### Neue Datei: `Assets/Scripts/PhysicsManager.cs`
- `Time.fixedDeltaTime = 0.001f` im Awake
- `Physics.simulationMode = SimulationMode.Script`
- Optional: `Physics.Simulate(Time.fixedDeltaTime)` manuell aufrufen
- MaxAllowedTimestep auf 0.05 (50ms, max 50 Substeps pro Frame)

### Änderung: `Assets/Modules/FlightBridge/FlightDynamicsBridge.cs`
- Sende IMU-Daten in `FixedUpdate()` statt `Update()`
- Empfange PWM in `FixedUpdate()`

### Modell: qwen3-coder

## Phase 2: IMU-Simulation

### Neue Datei: `Assets/Modules/Sensors/IMUSimulator.cs`
- Gyro: `rb.angularVelocity * Mathf.Rad2Deg` (°/s, 3-Achsen)
- Accelerometer: `InverseTransformDirection(accel + gravity)` (m/s², lokal)
- Magnetometer: Vereinfachter Kompass

### Neue Datei: `Assets/Modules/Sensors/SensorNoise.cs`
- Gauss-Rauschen auf Gyro (konfigurierbar)
- Bias-Drift auf Accelerometer
- Vibrations-Simulation (Motor-Frequenz-abhängig)

### Modell: qwen3-coder

## Phase 3: Custom Physics Thread @ 4 kHz

### Neue Datei: `Assets/Modules/Physics/DronePhysicsThread.cs`
- Separater Thread für Drohnen-Physik
- Euler-Integration @ 4 kHz (oder RK4)
- Lock-Free ConcurrentQueue für Main-Thread Sync
- Berechnet: Thrust, Drag, Gravity, Motor-Torque
- Main Thread wendet Transform einmal pro Frame an

### Neue Datei: `Assets/Modules/Physics/DronePhysicsModel.cs`
- Thrust pro Motor = f(PWM, Spannung, Propeller-Curve)
- Drag = 0.5 * Cd * A * rho * v²
- Ground Effect = 1 + (R/4h)² (Cheeseman & Bennett)
- Motor Torque = Counter-Rotation-Moment

### Modell: qwen3-coder (Phase 3 optional, erst nach Phase 1+2)

## Abhängigkeiten
- Phase 1 ist unabhängig, kann sofort implementiert werden
- Phase 2 braucht Phase 1 (FixedUpdate für Timing)
- Phase 3 braucht Phase 2 (IMU-Daten als Input)

## Test-Kriterien
- FixedUpdate wird @ 1000 Hz aufgerufen (Debug.Log Counter)
- BF SITL empfängt Gyro-Daten mit >500 Hz
- Drohne reagiert spürbar realistischer auf PID-Inputs
- Kein Frame-Drop durch Physik-Overhead (Profiler checken)
