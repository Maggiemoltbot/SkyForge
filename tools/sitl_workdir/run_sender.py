import sys
sys.path.append('/Users/rudi/Projects/SkyForge/tools/sitl_workdir')

# Importiere den FDMSender
from fdm_sender import FDMSender

# Erstelle eine Instanz des Senders
sender = FDMSender()

# Starte den Takeoff-Test
sender.takeoff_test()