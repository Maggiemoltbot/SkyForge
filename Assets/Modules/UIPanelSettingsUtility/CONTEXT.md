# UIPanelSettingsUtility Module — CONTEXT.md
**Modul:** UIPanelSettingsUtility
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Scripts/UI/UIPanelSettingsUtility.cs
**Beschreibung:** Stellt sicher, dass alle `UIDocument`-Komponenten im UI Toolkit über ein korrektes `PanelSettings`-Asset verfügen. Dient als Fallback-Mechanismus, um Fehler beim UI-Rendering zu verhindern.

## Funktionsweise
- Prüft bei Aufruf von `Ensure()` ob ein `UIDocument.panelSettings`-Asset verknüpft ist
- Falls nein, weist es entweder ein geladenes `Resources/UI/PanelSettings/SkyForgePanelSettings.asset` zu ODER erzeugt ein temporäres Laufzeit-Asset
- Lädt das zugehörige `ThemeStyleSheet` (`SkyForgeTheme.uss`) aus `Resources/Styles/`, falls vorhanden
- Loggt einen `Debug.LogWarning` mit Kontextobjekt, um fehlende Assets in der Produktion sichtbar zu machen

## Architektur
- **Statische Utility-Klasse** (`UIPanelSettingsUtility`)
  - Kein MonoBehaviour notwendig
  - Wird von `StartScreenController`, `ControllerSetupController`, `HUDController` etc. aufgerufen

### Resource-Verwaltung
- Lädt `PanelSettings` aus `Resources/UI/PanelSettings/SkyForgePanelSettings`
- Lädt `ThemeStyleSheet` aus `Resources/Styles/SkyForgeTheme.uss`
- Falls `Resources/Styles/SkyForgeTheme` nicht existiert, wird nach `SkyForgeTheme` (ohne Pfad) gesucht
- Falls nur ein `StyleSheet` existsiert (kein `ThemeStyleSheet`), wird es genutzt und eine Warnung ausgegeben

### Fallback-Mechanismus
1. Suche nach persistiertem `PanelSettings` Asset in Resources
2. Falls nicht gefunden → Erzeuge temporäres `ScriptableObject` (Laufzeit)
3. Setze Referenzauflösung (1920x1080), Scale Mode, DPI
4. Weise `ThemeStyleSheet` zu, falls geladen

## Integrierte Module
- `StartScreenController`
- `ControllerSetupController`
- `HUDController`
- `UIDocument`-basierte UI-Panels im gesamten Projekt

## Dependencies
- Unity UI Toolkit
- `Resources`-System (für `Resources.Load<T>()`)
- `SkyForgeTheme.uss` im `Resources/Styles/` Ordner

## Problembehebung
- **Symptom:** UI erscheint nicht oder ist abgeschnitten
  - **Ursache:** Fehlendes `PanelSettings` oder falscher Scale Mode
  - **Lösung:** `UIPanelSettingsUtility.Ensure()` in `Awake()` aufrufen

- **Symptom:** Thema wird nicht angewendet
  - **Ursache:** `SkyForgeTheme.uss` fehlt in `Resources/Styles/`
  - **Lösung:** Kopiere die USS-Datei nach `Assets/Resources/Styles/`

## Status
- Funktioniert robust in allen Test-Szenarien
- Wurde in Sprint 4 als UI-Fix eingeführt
- Behebt konsistent die Probleme mit grauen UI-Feldern

## Nächste Schritte
- Automatisierten UI-Regression-Test hinzufügen
- Unterstützung für mehrere Themes (Tagging/Erkennung)