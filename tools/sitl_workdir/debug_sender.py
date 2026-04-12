import sys
sys.path.append('/Users/rudi/Projects/SkyForge/tools/sitl_workdir')

# Importiere den FDMSender
from fdm_sender import FDMSender
import time

# Erstelle eine Instanz des Senders
sender = FDMSender()

# Schritt 1: Arm die Drohne
print("Schritt 1: Arming der Drohne...")
for i in range(100):
    sender.send_rc_packet(throttle=1000)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 25 == 0:
        print(f"Arming... {i}/100")

# Schritt 2: Langsame Erhöhung des Gaspedals bis 1500
print("Schritt 2: Langsame Erhöhung des Gaspedals bis 1500...")
for i in range(500):
    throttle = 1000 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        print(f"Erhöhe Gas: {throttle}/1500")

# Schritt 3: Halte bei 1500
print("Schritt 3: Halten bei 1500...")
for i in range(200):
    sender.send_rc_packet(throttle=1500)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 50 == 0:
        print(f"Halte bei 1500... {i}/200")

# Schritt 4: Versuche, weiter zu erhöhen bis 1800
print("Schritt 4: Erhöhe Gas bis 1800...")
for i in range(300):
    throttle = 1500 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 75 == 0:
        print(f"Erhöhe Gas: {throttle}/1800")

# Schritt 5: Halte bei 1800
print("Schritt 5: Halten bei 1800...")
for i in range(500):
    sender.send_rc_packet(throttle=1800)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        print(f"Halte bei 1800... {i}/500")

print("Test abgeschlossen.")