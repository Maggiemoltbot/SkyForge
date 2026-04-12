import sys
sys.path.append('/Users/rudi/Projects/SkyForge/tools/sitl_workdir')

# Importiere den FDMSender
from fdm_sender import FDMSender
import time
import math

# Erstelle eine Instanz des Senders
sender = FDMSender()

# Hilfsfunktion zum Ausgeben und flushen
def log(message):
    print(message)
    sys.stdout.flush()

# Schritt 1: Arming der Drohne
log("Schritt 1: Arming der Drohne...")
for i in range(100):
    sender.send_rc_packet(throttle=1000)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 25 == 0:
        log(f"Arming... {i}/100")

# Schritt 2: Langsame Erhöhung des Gaspedals bis 1500
log("Schritt 2: Langsame Erhöhung des Gaspedals bis 1500...")
for i in range(500):
    throttle = 1000 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        log(f"Erhöhe Gas: {throttle}/1500")

# Schritt 3: Halten bei 1500
log("Schritt 3: Halten bei 1500...")
for i in range(200):
    sender.send_rc_packet(throttle=1500)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 50 == 0:
        log(f"Halte bei 1500... {i}/200")

# Schritt 4: Erhöhe Gas bis 1900 (über der Disarm-Grenze)
log("Schritt 4: Erhöhe Gas bis 1900...")
for i in range(400):
    throttle = 1500 + i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        log(f"Erhöhe Gas: {throttle}/1900")

# Schritt 5: Halte bei 1900 und prüfe ob die Drohne stabil bleibt
log("Schritt 5: Halten bei 1900 und prüfen der Stabilität...")
for i in range(1000):  # 10 Sekunden Beobachtung
    sender.send_rc_packet(throttle=1900)
    sender.send_fdm_packet()
    time.sleep(0.01)
    if i % 100 == 0:
        log(f"Stabilitätsprüfung... {i}/1000")
        
# Schritt 6: Teste Roll, Pitch und Yaw Inputs
log("Schritt 6: Teste Roll, Pitch und Yaw Inputs...")
x = 0
for i in range(400):
    # Sinusförmige Bewegungen für sanftes Testen
    roll = 1500 + 100 * math.sin(x)
    pitch = 1500 + 100 * math.cos(x)
    yaw = 1500 + 100 * math.sin(x * 0.5)
    
    sender.send_rc_packet(roll=int(roll), pitch=int(pitch), throttle=1900, yaw=int(yaw))
    sender.send_fdm_packet()
    
    x += 0.1
    time.sleep(0.01)
    
    if i % 100 == 0:
        log(f"Teste Steuerung... {i}/400")

# Schritt 7: Landung
log("Schritt 7: Landung initiiert...")
x = 0
for i in range(500):
    # Langsame Reduzierung des Gaspedals
    throttle = 1900 - i
    sender.send_rc_packet(throttle=throttle)
    sender.send_fdm_packet()
    time.sleep(0.01)
    
    if i % 100 == 0:
        log(f"Landung: {throttle}/1000")
        
# Schritt 8: Disarm
log("Schritt 8: Disarm der Drohne...")
for i in range(100):
    sender.send_rc_packet(throttle=1000)
    sender.send_fdm_packet()
    time.sleep(0.01)
    
    if i % 25 == 0:
        log(f"Disarm... {i}/100")
        
log("Test erfolgreich abgeschlossen.")