# Betaflight ARM-Konfiguration (Button East → AUX2 → ARM)

## Ziel

Der Xbox/PlayStation **Button East (B / Kreis)** schaltet in SkyForge den Kanal **AUX2** zwischen 1000 µs (aus) und 2000 µs (ein). Diese Anleitung verknüpft den Kanal in Betaflight mit dem Flight Mode **ARM**, so dass das Modell nur dann scharf geschaltet wird, wenn der Button gedrückt ist.

## Voraussetzungen

- Betaflight SITL läuft lokal (`./tools/start_sitl.sh`) **oder** ein per USB angeschlossener Flight Controller
- Betaflight Configurator ≥ 10.10
- Gamepad ist in Unity / RCInputBridge konfiguriert (B-Button → AUX2)
- Optional (Windows): Zugriff auf `clip`-Befehl für Zwischenablage (wird vom Batch-Script genutzt)

## Schnellanleitung (Configurator GUI)

1. **Verbinden**
   - Betaflight Configurator starten → Verbindungstyp **TCP** wählen → `127.0.0.1:5761` (SITL) oder den jeweiligen COM-Port (Hardware)
2. **Modes-Tab öffnen**
   - `Modes` auswählen
3. **ARM-Mode konfigurieren**
   - Falls bereits ein ARM-Eintrag existiert: Kanal auf **AUX 2** umstellen
   - Range auf ca. **1800 – 2100** setzen (AUX2 = 2000 µs bei gedrücktem Button)
   - `Save` klicken
4. **Prüfen**
   - Button B drücken → gelber Balken im ARM-Mode sollte in den 1800–2100 Bereich springen und der Mode aktiviert werden

## CLI-Befehle (Direkteingabe)

Betaflight CLI öffnen (`Modes` → `CLI`) und folgenden Block einfügen:

```
aux 0 0 1 1800 2100 0 0
save
```

**Bedeutung:**
- `aux 0` – erster Mode-Slot (ARM)
- `0` – Mode-ID für ARM
- `1` – AUX-Kanal (0 = AUX1, 1 = AUX2, …)
- `1800 2100` – Aktivierungsbereich in µs
- `save` – Konfiguration sichern und neu starten

> Hinweis: Existiert bereits ein weiterer Mode-Eintrag für ARM (z. B. auf AUX1), wird dieser durch den Befehl überschrieben.

## Batch-Script (Windows)

Datei: [`tools/configure_arm_aux2.bat`](../tools/configure_arm_aux2.bat)

Funktionen:
- Kopiert die CLI-Befehle aus [`tools/betaflight-arm-aux2.cli`](../tools/betaflight-arm-aux2.cli) automatisch in die Windows-Zwischenablage (`clip`)
- Zeigt Schritt-für-Schritt-Anweisungen für die Anwendung über den Betaflight Configurator

**Anwendung:**
1. Doppelklick auf `configure_arm_aux2.bat`
2. Hinweise im Terminal folgen (CLI-Befehle sind nun in der Zwischenablage)
3. In Betaflight Configurator → CLI → Befehle einfügen (Strg + V) → Enter → Reboot abwarten

## Verifikation

- In Unity → `RCInputBridge` → „Show Channel Preview“ aktivieren → AUX2 sollte zwischen `1000` und `2000` umschalten
- Controller Debug Window (`SkyForge/Controller Debug`) zeigt nun PWM-Werte im Bereich `1000–2000` für Kanal 5 (AUX2)
- Betaflight Configurator → Tab `Modes` → ARM-Range leuchtet gelb, wenn Button B gedrückt ist

## Troubleshooting

| Problem | Ursache | Lösung |
|---------|---------|--------|
| ARM bleibt auf AUX1 | Alter Mode-Eintrag wurde nicht überschrieben | In CLI `aux` ohne Parameter eingeben, vorhandene Einträge prüfen, ggf. manuell löschen (`aux <index> 0 1 900 900 0 0` → neutralisieren) |
| CLI-Befehle lassen sich nicht einfügen | `clip` nicht verfügbar / Batch-Script nicht genutzt | Befehle manuell aus `tools/betaflight-arm-aux2.cli` kopieren |
| Mode wird nicht aktiv | AUX2 erreicht nicht 1800 µs | Controller-Setup prüfen, Deadzone/Invert im `ControllerConfig` anpassen |
| Betaflight speichert nicht | `save` ausgeführt aber keine Rückmeldung | Nach `save` wartet Betaflight auf Reboot – ggf. SITL kurz stoppen und neu starten |

## Referenzen

- Betaflight Dokumentation → Flight Modes & AUX Channel Mapping
- SkyForge Dokumentation → [`docs/controller-input-setup.md`](controller-input-setup.md)
- CLI Snippet → [`tools/betaflight-arm-aux2.cli`](../tools/betaflight-arm-aux2.cli)
