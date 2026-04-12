import sys
print("1. Sys-Path: ", sys.path)
sys.stdout.flush()

# Pfad hinzufügen
sys.path.append('/Users/rudi/Projects/SkyForge/tools/sitl_workdir')
print("2. Sys-Path aktualisiert: ", sys.path)
sys.stdout.flush()

# Importversuch numpy
try:
    import numpy as np
    print("3. numpy erfolgreich importiert, Version: ", np.__version__)
    sys.stdout.flush()
except Exception as e:
    print("Fehler bei numpy-Import: ", e)
    sys.stdout.flush()

# Importversuch fdm_sender
try:
    from fdm_sender import FDMSender
    print("4. fdm_sender erfolgreich importiert")
    sys.stdout.flush()
except Exception as e:
    print("Fehler bei fdm_sender-Import: ", e)
    sys.stdout.flush()
    sys.exit(1)

import time
print("5. Zeitmodul importiert")
sys.stdout.flush()

# Erstelle eine Instanz des Senders
sender = FDMSender()
print("6. FDMSender-Instanz erstellt")
sys.stdout.flush()

# Schritt 1: Arm die Drohne
print("Schritt 1: Arming der Drohne...")
for i in range(100):
    sender.send_rc_packet(throttle=1000)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 25 == 0:
        print(f"Arming... {i}/100")
        sys.stdout.flush()

# Schritt 2: Langsame Erhöhung des Gaspedals bis 1500
print("Schritt 2: Langsame Erhöhung des Gaspedals bis 1500...")
for i in range(500):
    throttle = 1000 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        print(f"Erhöhe Gas: {throttle}/1500")
        sys.stdout.flush()

# Schritt 3: Halte bei 1500
print("Schritt 3: Halten bei 1500...")
for i in range(200):
    sender.send_rc_packet(throttle=1500)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 50 == 0:
        print(f"Halte bei 1500... {i}/200")
        sys.stdout.flush()

# Schritt 4: Versuche, weiter zu erhöhen bis 1800
print("Schritt 4: Erhöhe Gas bis 1800...")
for i in range(300):
    throttle = 1500 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 75 == 0:
        print(f"Erhöhe Gas: {throttle}/1800")
        sys.stdout.flush()

# Schritt 5: Halte bei 1800
print("Schritt 5: Halten bei 1800...")
for i in range(500):
    sender.send_rc_packet(throttle=1800)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        print(f"Halte bei 1800... {i}/500")
        sys.stdout.flush()

print("Test abgeschlossen.")
sys.stdout.flush()