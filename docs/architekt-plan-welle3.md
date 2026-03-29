# Architektur-Plan Welle 3 – SkyForge Drohnensimulator

**Ziel:** Erweiterung des SkyForge Simulators um erweiterte Usability-Features: Controller-Konfigurator, HUD Overlay und visuelle Verbesserungen am Drohnenmodell.

---

## 1. Module-Übersicht

### A) Controller-Konfigurator (F4 Panel)
- **`Assets/UI/ControllerConfigPanel.cs`** – Haupt-UI-Script für das Konfigurations-Panel
- **`Assets/UI/ControllerMappingManager.cs`** – Lädt/speichert die JSON-Config und synchronisiert mit `ControllerConfig` ScriptableObject
- **`Assets/UI/Prefabs/ControllerConfigPanel.prefab`** – UI-Prefab im Canvas-Format
- **`Assets/Configs/ControllerMapping.json`** – Persistente Speicherung der Kanal-Mappings (erzeugt bei Export)

### B) HUD Overlay
- **`Assets/UI/HudOverlay.cs`** – Zeigt Flugdaten in Echtzeit an
- **`Assets/UI/Prefabs/HudOverlay.prefab`** – UI-Overlay-Prefab (Canvas, Screen Space Overlay)

### C) Verbessertes Drone Model
- **`Assets/Drone/LEDThrustIndicator.cs`** – Steuert LED-Farben an Motoren basierend auf PWM-Werten
- **`Assets/Drone/PropellerRotation.cs`** – Rotiert Propeller-Objekte proportional zum PWM-Signal

## 2. Abhängigkeiten

- `ControllerConfigPanel` ↔ `ControllerConfig` (ScriptableObject) – liest/schreibt Mapping-Config
- `ControllerMappingManager` ↔ `ControllerMapping.json` – Serialisierung/Deserialisierung
- `HudOverlay` ↔ `DroneController` – liest Höhe, Geschwindigkeit, Armed-Status
- `HudOverlay` ↔ `CameraManager` – liest aktiven Kameramodus
- `HudOverlay` ↔ `RCInputBridge` – SITL-Verbindungsstatus
- `LEDThrustIndicator` & `PropellerRotation` ↔ `DroneController` – erhalten PWM-Werte pro Motor
- Alle UI-Komponenten verwenden `UnityEngine.UI` (kein TextMeshPro)

## 3. Implementierungs-Reihenfolge

1. **Initialisierung (`SkyForgeIntegrator` Erweiterung)** – Erzeugt neue UI-Objekte (HUD & Panel)
2. **Controller-Konfigurator** – Kernmodule anlegen (Priorität 1)
3. **HUD Overlay** – Parallel zum Konfigurator, aber mit späterem Integrationstest
4. **LED + Propeller** – Kann parallel implementiert werden, da unabhängig von UI
5. **Test-Modus & Live-Preview** – Integration im Controller-Konfigurator
6. **Finaler Integrationstest & Speicherung**

⚠️ Der Konfigurator hat höchste Priorität. HUD und Drone-Model-Verbesserungen können parallel umgesetzt werden.

## 4. Code-Skelette

### `ControllerConfigPanel.cs`
```csharp
public class ControllerConfigPanel : MonoBehaviour {
    public GameObject panel;
    public ControllerConfig config;
    public Toggle[] axisInvertToggles;
    public Slider[] deadzoneSliders;
    public Slider[] expoSliders;
    public Text[] liveValueTexts;
    public Button saveButton, loadButton, testModeButton;

    void Update(); // Aktualisiert Live-Werte
    void TogglePanel(); // F4-Handling
    void ApplyConfiguration();
    void EnterTestMode();
}
```

### `ControllerMappingManager.cs`
```csharp
[Serializable]
class AxisMapping {
    public string axisName;
    public int rcChannel;
    public bool invert;
    public float deadzone;
    public float expo;
}

class ControllerMappingData {
    public List<AxisMapping> axisMappings;
    public List<AxisMapping> buttonMappings;
    public string lastSavedAt;
}

class ControllerMappingManager {
    string configPath = "Assets/Configs/ControllerMapping.json";
    ControllerMappingData currentData;

    void SaveConfig();
    void LoadConfig();
    void ApplyToControllerConfig(ControllerConfig config);
    void ExportToJson();
}
```

### `HudOverlay.cs`
```csharp
public class HudOverlay : MonoBehaviour {
    public Text altitudeText;
    public Text speedText;
    public Text armedText;
    public Text batteryText;
    public Text cameraModeText;
    public Text sitlStatusText;

    public DroneController drone;
    public CameraManager cameraManager;
    public RCInputBridge rcBridge;

    void Update();
    void UpdateBattery();
}
```

### `LEDThrustIndicator.cs`
```csharp
public class LEDThrustIndicator : MonoBehaviour {
    public Light ledLight;
    public Gradient thrustColorGradient; // Grün → Gelb → Rot
    public float maxPwm = 2000f;

    public void UpdateThrust(float pwmValue);
}
```

### `PropellerRotation.cs`
```csharp
public class PropellerRotation : MonoBehaviour {
    public float baseRotationSpeed = 1000f;
    public float maxRotationSpeed = 5000f;

    void Update() {
        // Rotation proportional zum PWM
    }
}
```

