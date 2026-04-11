# InputRouter Module — CONTEXT.md
**Modul:** InputRouter
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/InputRouter
**Beschreibung:** Zentraler Input-Router, der Eingaben aus verschiedenen Quellen (Gamepad, Tastatur, RL-Agent, Replay) normalisiert und an die Drohnensteuerung weiterleitet. Ersetzt die direkte Kopplung zwischen Input-Quelle und FlightBridge.

## Architektur

### Kernkomponenten (geplant)
- **InputRouter.cs**
  - Singleton-MonoBehaviour als zentraler Eingabe-Hub
  - Empfängt Eingaben von registrierten `IInputSource`-Providern
  - Priorisierung: Manuell > RL-Agent > Replay > Failsafe
  - Normalisiert alle Eingaben auf 16 RC-Kanäle (1000-2000 µs)
  - Leitet normalisierte Kanäle an `FlightDynamicsBridge.SendRCChannels()` weiter

- **IInputSource (Interface)**
  - `bool IsActive { get; }` — Ob die Quelle aktiv Daten liefert
  - `int Priority { get; }` — Höhere Priorität überschreibt niedrigere
  - `int[] GetChannels()` — 16 RC-Kanäle (1000-2000 µs)
  - `string SourceName { get; }` — Anzeigename ("Gamepad", "RL Agent", etc.)

### Input-Quellen (Implementierungen von IInputSource)
- **GamepadInputSource** — Kapselt `RCInputBridge`-Logik (Gamepad/Joystick)
- **KeyboardInputSource** — Debug-Tastatureingaben (W/A/S/D/Q/E)
- **RLAgentInputSource** — Empfängt Actions vom RL-Training-Modul
- **ReplayInputSource** — Spielt aufgezeichnete Eingaben ab (für Testing/Demo)
- **FailsafeInputSource** — Standardwerte bei Verbindungsverlust (alle Kanäle 1000, Throttle 1000)

### Routing-Logik
```
1. Alle aktiven Quellen abfragen
2. Nach Priorität sortieren (höchste zuerst)
3. Erste aktive Quelle mit validen Daten gewinnt
4. Bei Verbindungsverlust aller Quellen → Failsafe
5. Mode-Switch via UI oder Hotkey (F6 = nächste Quelle)
```

## Integration
- **Eingang:** `RCInputBridge` (refactored als `GamepadInputSource`), RL-Agent, Replay-System
- **Ausgang:** `FlightDynamicsBridge.SendRCChannels(int[] channels)`
- **UI:** Zeigt aktive Quelle im HUD an (z.B. "🎮 Gamepad" / "🤖 RL Agent")

## Dependencies
- `FlightDynamicsBridge` (Ausgang)
- `RCInputBridge` / `ControllerConfig` (Gamepad-Quelle)
- `RLTraining/RLBridge` (RL-Quelle, optional)
- Unity Input System (für Gamepad/Keyboard)

## Konfiguration
- Prioritäten der Quellen über Inspector konfigurierbar
- Failsafe-Timeout (Standard: 500ms ohne Input → Failsafe)
- Mode-Switch Hotkey (Standard: F6)

## Status
- **Phase:** Konzept/Design erstellt
- Interface-Design spezifiziert
- Integration mit bestehenden Modulen geplant
- Implementierung steht noch aus

## Nächste Schritte
- [ ] `IInputSource` Interface definieren
- [ ] `InputRouter.cs` Singleton implementieren
- [ ] `RCInputBridge` zu `GamepadInputSource` refactoren
- [ ] `KeyboardInputSource` für Debug implementieren
- [ ] `RLAgentInputSource` als Bridge zum RL-Modul
- [ ] HUD-Anzeige für aktive Input-Quelle
- [ ] Unit Tests für Prioritäts-Routing
