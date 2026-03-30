@echo off
setlocal ENABLEDELAYEDEXPANSION

set SCRIPT_DIR=%~dp0
set CLI_FILE=%SCRIPT_DIR%betaflight-arm-aux2.cli

if not exist "%CLI_FILE%" (
    echo [ERROR] CLI-Vorlage nicht gefunden: %CLI_FILE%
    exit /b 1
)

where clip >nul 2>nul
if errorlevel 1 (
    echo [WARN] Windows "clip"-Befehl nicht gefunden. Befehle werden nicht automatisch kopiert.
    goto :show
)

type "%CLI_FILE%" | clip
if errorlevel 1 (
    echo [WARN] Konnte Befehle nicht in die Zwischenablage kopieren.
    goto :show
)

echo Betaflight CLI-Befehle wurden in die Zwischenablage kopiert.

echo --- CLI-Befehle ---
:show
type "%CLI_FILE%"

echo --------------------

echo Schritte zur Anwendung:
echo   1. Betaflight SITL oder den Flight Controller starten und verbinden.
echo   2. Betaflight Configurator oeffnen und per TCP (127.0.0.1:5761) verbinden.
echo   3. Zum Tab "CLI" wechseln und die Befehle einfuegen (Strg+V).
echo   4. Mit Enter bestaetigen, auf "save" warten und den Neustart abwarten.
echo   5. Im Tab "Modes" kontrollieren, dass ARM auf AUX2 (2000-Range) liegt.

echo Fertig. Button East (B) steuert nun AUX2 -> ARM.

pause
