# Architektur-Plan: SkyForge UI System (XFLIGHT Corporate Design)

Dieses Dokument definiert die technische Architektur und Spezifikation für das neue UI-System von SkyForge, einem FPV-Drohnensimulator. Das Design folgt strikt dem XFLIGHT Corporate Design und der macOS Human Interface Guidelines (HIG) für eine professionelle, moderne und konsistente Nutzererfahrung.

## 1. Technologie-Entscheidung

**Entscheidung: Unity UI Toolkit (UIElements)**

Die Implementierung des gesamten UI-Systems (außer dem finalen minimalen HUD-Overlay für Performance-Gründe) wird auf Basis des **Unity UI Toolkit** erfolgen. OnGUI wird nur für den finalen FlightHUD im Spiel verwendet, da hier absolute Performance-Priorität gilt. Für alle Menüs (MainMenu, ControllerSetup, MapSelection, About) wird UI Toolkit verwendet.

### Begründung

1.  **Styling-Paradigma (CSS-ähnlich):** UI Toolkit verwendet UXML (Markup) und USS (Style Sheets), was die strikte Umsetzung des CI (Farben, Radien, Schriftarten) über zentrale Stilregeln ermöglicht. Ein Wechsel zu einem anderen Design kann durch Ändern einiger USS-Regeln erfolgen.
2.  **Modern & Empfohlen:** UI Toolkit ist die offizielle, moderne Lösung von Unity für komplexe UIs. OnGUI ist veraltet und nur für Debugging oder sehr performante Anzeigen vorgesehen.
3.  **Native Komponenten:** UI Toolkit bietet native Unterstützung für runde Ecken (`border-radius`), Flexbox-Layouts (responsive), und komplexe Eingabefelder (Slider, Buttons, Tabs), was der Designvorgabe entspricht.
4.  **Separation of Concerns:** UXML definiert Struktur, USS das Styling, und C# das Verhalten. Dies fördert Wartbarkeit und Teamarbeit.
5.  **Skalierbarkeit:** UI Toolkit ist besser für große, strukturierte UIs wie das geplante Menüsystem geeignet.

Der einzige Punkt, an dem auf OnGUI zurückgegriffen wird, ist **Modul 4: FlightHUD Upgrade**. Die bestehende `HudOverlay.cs` verwendet OnGUI für maximale Performance während des Fluges. Dieser Teil wird nur visual upgegradet (neue Schriftart, bessere Farben, bessere Hintergrundtextur für runde Ecken) und nicht auf UI Toolkit portiert, um die FPS-Performance nicht zu gefährden.


## 2. Datei-Übersicht (neue Dateien)

Alle Pfade sind absolut angegeben und beziehen sich auf das Projektverzeichnis.

### Shared Components (Wiederverwendbare Elemente)
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/ButtonUss.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/ButtonUss.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/PanelUss.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/PanelUss.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/SliderUss.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/SliderUss.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/VirtualStick.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/VirtualStick.cs`

### Hauptmenüs
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.cs`

### Controller Setup Wizard
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.cs`

### Karten-Auswahl
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.cs`

### About Screen
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.uxml`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.uss`
- `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.cs`


## 3. Modul-Spezifikationen

### Modul 1: MainMenuScreen

**Layout-Beschreibung:**
- Vollbild-Fenster mit Hintergrundfarbe `#0A0A0F`.
- Zentrierte vertikale Spalte.
- Oben: `Image`-Element für das `xflight-logo-transparent.png` logo.
- Darunter: `Label` für den weißen "SkyForge" Titel.
- Darunter ein vertikaler `VisualElement`-Container für die Button-Liste.
- Der Hintergrund zeigt eine subtile, sich langsam bewegende Gradient-Animation von `#0A0A0F` zu `#111118`.

**Felder/Properties (in `MainMenu.cs`):**
- `VisualTreeAsset uxml;` (Referenz zur UXML)
- `StyleSheet uss;` (Referenz zur USS)
- `Image logoImage;` (Zugriff auf das Logo-Element)
- `Label titleLabel;` (Zugriff auf den "SkyForge"-Titel)
- `Button flyButton;`, `Button controllerButton;`, `Button settingsButton;`, `Button aboutButton;` (Referenzen zu den Haupt-Buttons)

**Public API / Event-System:**
- `event Action onFly;` (wird beim Klicken auf FLY ausgelöst)
- `event Action onControllerSetup;` (wird beim Klicken auf CONTROLLER SETUP ausgelöst)
- `event Action onSettings;` (wird beim Klicken auf SETTINGS ausgelöst)
- `event Action onAbout;` (wird beim Klicken auf ABOUT ausgelöst)

**Farbzuordnung:**
- Hintergrund: `#0A0A0F`
- Text ("SkyForge"): `#FFFFFF`
- Button (Normal): `#16161F`, Schrift `#FFFFFF`
- Button (Hover): Hintergrund leicht heller, z.B. `#1A1A24`, Umrandung `#00D4FF` (`Accent Cyan`), Hintergrundfüllung `#181821`
- Button (CTA): Der FLY-Button hat ggf. einen Orange-Gradienten `#FF6B00` zu `#E65100` als Hintergrund (Accent Orange)


### Modul 2: ControllerSetupWizard

**Layout-Beschreibung:**
- Geteiltes Layout (Oben/Unten). Oben: Anleitungstext. Unten: Inhalt.
- **Step 1:** `Label` mit "Verbinde deinen Controller".
- **Step 2:** `Label` mit Anweisung ("Bewege THROTTLE..."), `ProgressBar` für Fortschritt, `Label` zur Live-Anzeige der erkannten Achse.
- **Step 3:** `Label` mit Anweisung ("Drücke Throttle nach OBEN..."), `Label` zur Anzeige der Auto-Invert-Ergebnisse.
- **Step 4:** `VisualElement` mit den vier `SliderUss`-Komponenten (zwei pro Zeile), einer für Deadzone, einer für Expo, pro Achse (Roll, Pitch, Throttle, Yaw). `Toggle`-Buttons für Invertierung.
- Rechts im Bildschirm: `VisualElement` für zwei `VirtualStick`-Komponenten zur Live-Preview der Eingaben.
- Unten: `Button` "Save", "Load", "Back", "Next".

**Felder/Properties (in `ControllerSetupWizard.cs`):**
- `int currentStep;`
- `string[] stepMessages = { /* ... */ };`
- `Dictionary<string, AxisMapping> tempAxisConfig;` (Kopie der Config für Änderungen vor Speicherung)
- `Button backButton;`, `Button nextButton;`
- `Label instructionLabel;`
- `VisualElement livePreviewContainer;`
- `VirtualStick leftStick;`, `VirtualStick rightStick;` (Referenz auf die Live-Preview-Sticks)
- `Dictionary<string, Slider> deadzoneSliders;` // Key: "Roll", "Pitch", ...`
- `Dictionary<string, Slider> expoSliders;`
- `Dictionary<string, Toggle> invertToggles;`

**Public API / Event-System:**
- `event Action<ControllerConfig> onConfigSaved;` (wird beim Speichern ausgelöst, übergibt die neue Config)
- `event Action onConfigLoaded;`
- `event Action<ControllerConfig> onConfigConfirmed;` (wird beim Beenden des Wizards mit der finalen Config)
- Interne Events: `event Action<string> onAxisDetected;` (f.d. Auto-Detect)

**Farbzuordnung:**
- Hintergrund: `#0A0A0F`
- Text (Anleitung): `#FFFFFF`
- Text (Sekundär): `#9CA3AF`
- Button: Wie in `MainMenu`
- VirtualStick (Bodenelement): `#16161F`
- VirtualStick (Kugelelement): `#00D4FF`
- VirtualStick (Kugel bei Max): `#FF6B00` (Accent Orange)
- Slider (Fill): `#00D4FF`
- Slider (Background): `#16161F`


### Modul 3: MapSelectionScreen

**Layout-Beschreibung:**
- Hintergrundfarbe `#0A0A0F`.
- Ein flexibler `GridView`-ähnlicher Container mit `FlexDirection.RowWrap`, der Karten-`VisualElement`s (Cards) enthält.
- Jedes `CardElement` ist ein `PanelUss` mit einem Innenabstand.
- Innerhalb jedes Cards: Ein `VisualElement` für das Thumbnail, ein `Label` für den Namen, ein `Label` für Typ und Splat-Count (z.B. "Indoor, 120k Splats").
- Bei Hover: Card bekommt einen `#00D4FF` helleren Rand und hebt sich leicht ab.
- Bei Auswahl (Click): Card wird heller umrandet und mit einem `checkmark`-Icon gekennzeichnet.
- Oben Rechts: `Button` "Back to Menu".
- Unten: `Label` für Status, z.B. "Lade Karten..." oder "Keine Karten gefunden."

**Felder/Properties (in `MapSelection.cs`):**
- `List<MapCard> availableMaps;` // Datenklasse mit Name, Type, SplatCount, ScenePath, ThumbnailPath`
- `VisualElement mapViewContainer;`
- `Button backButton;`
- `Label statusLabel;`
- `MapCard selectedCard;`

**Public API / Event-System:**
- `event Action<MapCard> onMapSelected;` (wird beim Doppelklick auf eine Card ausgelöst)
- `event Action onBackRequested;` (wird beim Klicken auf "Back to Menu" ausgelöst)
- `event Action<List<MapCard>> onMapListLoaded;`

**Farbzuordnung:**
- Hintergrund: `#0A0A0F`
- Card (Hintergrund): `#16161F`
- Text (primär): `#FFFFFF`
- Text (sekundär): `#9CA3AF`
- Hover Rand: `#00D4FF`
- Selection Rand / Check: `#FF6B00`
- Status Text: `#FF6B00`


### Modul 4: FlightHUD (Upgrade)

**Layout-Beschreibung:**
- Verwendung und Weiterentwicklung der vorhandenen `HudOverlay.cs`.
- Anordnung der Elemente bleibt ähnlich (rechts unten).
- Anstatt eines einfachen, rechteckigen `GUI.Box` wird eine **semi-transparente, runde Hintergrundtextur** als `Texture2D` (aus `#16161F` mit 60% Alpha und `border-radius` 12px) gezeichnet.
- Alle `GUI.Label`-Elemente behalten ihre Position, jedoch mit neuer `Font Inter` und angepasster Farbe.
- Die Anzeige der Geschwindigkeit (SPD) und Höhe (ALT) erhält einen `#00D4FF`-Hintergrund und weiße Schrift.
- Die Anzeige der Batterie (BAT) wechselt ihre Farbe: `#00D4FF` (>50%), `#9CA3AF` (20-50%), `#FF6B00` (<20%).
- Die ARMED-Anzeige blinkt rot, wenn `ARMED`, und ist grün, wenn `DISARMED`.

**Felder/Properties (in bestehender `HudOverlay.cs`, Ergänzungen):**
- `public Texture2D roundedPanelTexture;` // Neue öffentliche Referenz für die gerundete Textur
- `public Font interFont;` // Neue öffentliche Referenz für Inter Font
- (Alle vorhandenen Felder bleiben)`

**Farbzuordnung:**
- Hintergrundbox: Textur in `#16161F` mit 60% Alpha
- Hauptschrift: `#FFFFFF`
- Wert-Hintergrund (ALT, SPD): `#00D4FF`
- Wert-Schrift (ALT, SPD): `#FFFFFF`
- ARMED (Angezeigt): `#FF6B00` (Orange, evtl. blinkt)
- ARMED (Nicht angezeigt): `#9CA3AF` (Grau)
- BAT 50-100%: `#00D4FF`
- BAT 20-50%: `#9CA3AF`
- BAT <20%: `#FF6B00`


### Modul 5: AboutScreen

**Layout-Beschreibung:**
- Vollbild-Fenster mit Hintergrundfarbe `#0A0A0F`.
- Zentrierte vertikale Spalte.
- Oben: Großes `Image`-Element für das weiße `xflight-logo-white.png`.
- Darunter: `Label` für "Powered by".
- Darunter: `VisualElement` mit horizontaler Ausrichtung (`flex-direction: row`) zur Anzeige der `logo_supporters.png`.
- Darunter: Versionsinformation `Label`, z.B. "SkyForge v1.0.0".
- Darunter: `Label` mit Credits, z.B. "Entwickelt von Rudi & Maggie".
- Darunter: `Label` mit Verlinkung `"xflight-aircrafts.com"`, klickbar.
- Unten Rechts: `Button` "Back".

**Felder/Properties (in `AboutScreen.cs`):**
- `VisualTreeAsset uxml;`
- `StyleSheet uss;`
- `Image mainLogo;`
- `Image partnerLogos;`
- `Label versionLabel;`
- `Label creditsLabel;`
- `Label websiteLabel;`
- `Button backButton;`
- `HyperlinkButton websiteButton;` // Ein spezielles, verlinkendes Button-Ereignis

**Public API / Event-System:**
- `event Action onBack;`
- `event Action onWebsiteClicked;`

**Farbzuordnung:**
- Hintergrund: `#0A0A0F`
- Text: `#FFFFFF`
- Website-Link (Hover): `#00D4FF`
- Button: Wie in `MainMenu`


## 4. Scene-Management

Die Szene wird über Unitys `SceneManager` verwaltet.
- Die Hauptszene `SkyForgeCore.unity` enthält die `CameraManager`, `RCInputBridge`, `HudOverlay` und den `DroneController`. 
- Weitere Szene: `MainMenuScene.unity`. Diese enthält das Root `VisualElement` für das UI Toolkit und eine Referenz zur `MainMenu`-MonoBehaviour-Klasse.
- **Ablauf:**
  1.  Das Spiel startet mit `MainMenuScene`. 
  2.  Der `MainMenu`-Code instanziiert das UI aus der UXML und bindet die Events.
  3.  Bei Klick auf FLY wird `onFly` ausgelöst.
  4.  Ein zentraler `GameManager` (Singleton) erfasst dieses Event, lädt die `SkyForgeCore.unity`-Szene asynchron mittels `SceneManager.LoadSceneAsync` und deaktiviert die `MainMenuScene`. 
  5.  Beim Klick auf CONTROLLER SETUP wird die `ControllerSetupScene.unity` geladen.
  6.  Beim Beenden des Setups wird in die ursprüngliche Szene zurück gewechselt.


## 5. Shared Components

Die folgenden UI-Komponenten werden als wiederverwendbare Einheiten in `/Assets/UI/Shared/` angelegt:

- **`ButtonUss`:** Ein `Button`-Element mit zentralem Styling. `border-radius: 10px`, `background-color: #16161F`, `color: #FFFFFF`, Hover: `border: 2px solid #00D4FF`, Aktiv: leichter Hintergrund in `#181821`.
- **`PanelUss`:** Ein allgemeines `VisualElement` für Panels. `background-color: #16161F`, `border-radius: 8px`, `padding: 16px`, `margin: 8px`.
- **`SliderUss`:** Eine Kombination aus `VisualElement` (Container), `Label`, `Slider` und `Label` (Wertanzeige). Hintergrund: `#16161F`, Fill: `#00D4FF`, Thumb: `#FF6B00`.
- **`VirtualStick`:** Ein benutzerdefiner `VisualElement` in C#, der zwei `VisualElement`s kombiniert: einen statischen Boden (rund, `#16161F`) und eine bewegliche Kugel (rund, `#00D4FF`). Die Position der Kugel wird durch die öffentliche Methode `SetValue(Vector2 value)` gesteuert, wobei `value` von (-1, -1) bis (1, 1) normalisiert ist.


## 6. Implementierer-Aufteilung (Codex-Tasks)

Es werden vier parallele Codex-Tasks definiert, um die Entwicklung zu beschleunigen. Jeder Task erhält einen spezifischen Auftrag.

### Task A: Shared Components + MainMenuScreen
- **Dateien (schreiben):**
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/ButtonUss.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/ButtonUss.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/PanelUss.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/PanelUss.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/SliderUss.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/SliderUss.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MainMenu/MainMenu.cs`
- **Specs:** Siehe Modul 1 & 5 in diesem Plan. Der FLY-Button ist primär und soll hervorgehobenen werden.
- **Farben:** `#0A0A0F`, `#16161F`, `#FFFFFF`, `#00D4FF`, `#FF6B00`

### Task B: ControllerSetupWizard
- **Dateien (schreiben):**
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/VirtualStick.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Shared/Components/VirtualStick.cs`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/ControllerSetup/ControllerSetupWizard.cs`
- **Specs:** Muss den 4-Schritt-Wizard vollständig implementieren, inkl. Auto-Detect, Live-Preview mit den beiden `VirtualStick`-Instanzen und der Konfiguration von Invertierung, Deadzone und Expo.
- **Farben:** `#0A0A0F`, `#16161F`, `#FFFFFF`, `#9CA3AF`, `#00D4FF`, `#FF6B00`

### Task C: MapSelectionScreen + AboutScreen
- **Dateien (schreiben):**
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/MapSelection/MapSelection.cs`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.uxml`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.uss`
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/Scenes/About/AboutScreen.cs`
- **Specs:** Implemeniert die Grid-Ansicht für Karten mit Hover- und Selektionszuständen. Das About-Screen zeigt Logo, Partner, Version und einen klickbaren Link.
- **Farben:** `#0A0A0F`, `#16161F`, `#FFFFFF`, `#9CA3AF`, `#00D4FF`, `#FF6B00`

### Task D: FlightHUD Upgrade
- **Dateien (bearbeiten):**
  - `/Users/rudi/Projects/SkyForge/src/SkyForge/Assets/UI/HudOverlay.cs` (bestehende Datei)
- **Specs:** Modifiziert die bestehende `OnGUI`-Methode, um eine runde, transparente Hintergrundtextur zu nutzen, die Schriftart zu `Inter` zu wechseln und die Farben gemäß der neuen CI-Vorgaben (`#00D4FF`, `#FF6B00`) für die Werte und Warnungen zu verwenden. Der Code muss performant bleiben.
- **Farben:** `#16161F` (Textur, 60% Alpha), `#FFFFFF`, `#00D4FF`, `#FF6B00`


## 7. Integration

Die Module werden durch ein Ereignis- und Zustandsmanagement verbunden:

- **Zentrale Steuerung (`GameManager`):** Ein Singleton-Skript, das als zentraler Hub fungiert. Es verwaltet den globalen Spielzustand (z.B. `GameState = MainMenu | Flying | ControllerSetup`), lädt und entlädt Szenen und übersetzt UI-Events in Szene-Wechsel.
- **Event Bus:** Die UI-Module kommunizieren über öffentliche Events (z.B. `onFly`, `onMapSelected`). Der `GameManager` abonniert diese Events und reagiert darauf (z.B. `SceneManager.LoadSceneAsync("SkyForgeCore")`).
- **State Management:** Die `ControllerConfig` und das ausgewählte `MapCard` werden vom `GameManager` gespeichert und können von der `RCInputBridge` und `SceneLoader` abgerufen werden.
- **Konfigurations-Datei:** Die `ControllerConfig` wird weiterhin als JSON in `Application.persistentDataPath` gespeichert.

Diese Architektur gewährleistet eine lockere Kopplung zwischen den UI-Komponenten und dem Spielkern, während eine klare Verbindung über definierte Schnittstellen (Events) hergestellt wird.