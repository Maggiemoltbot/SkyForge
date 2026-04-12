import sys
print("Sys-Path:", sys.path)

# Pfad hinzufügen
sys.path.append('/Users/rudi/Projects/SkyForge/tools/sitl_workdir')
print("Sys-Path aktualisiert:", sys.path)

# Importversuch numpy
try:
    import numpy as np
    print("numpy erfolgreich importiert, Version:", np.__version__)
except Exception as e:
    print("Fehler bei numpy-Import:", e)

# Importversuch scipy
try:
    import scipy
    print("scipy erfolgreich importiert, Version:", scipy.__version__)
except Exception as e:
    print("Fehler bei scipy-Import:", e)

# Importversuch fdm_sender
try:
    import fdm_sender
    print("fdm_sender erfolgreich importiert")
    print("fdm_sender Modul-Pfad:", fdm_sender.__file__)
except Exception as e:
    print("Fehler bei fdm_sender-Import:", e)

# Versuch, FDMSender zu instanziieren
try:
    sender = fdm_sender.FDMSender()
    print("FDMSender-Instanz erfolgreich erstellt")
except Exception as e:
    print("Fehler bei FDMSender-Instanziierung:", e)