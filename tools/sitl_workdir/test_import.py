import sys
print("Python Path:", sys.path)

# Versuche numpy zu importieren
try:
    import numpy as np
    print("numpy erfolgreich importiert, Version:", np.__version__)
except ImportError as e:
    print("Fehler beim Importieren von numpy:", e)

# Versuche scipy zu importieren
try:
    import scipy
    print("scipy erfolgreich importiert, Version:", scipy.__version__)
except ImportError as e:
    print("Fehler beim Importieren von scipy:", e)

# Versuche fdm_sender zu importieren
try:
    sys.path.append('.')
    import fdm_sender
    print("fdm_sender erfolgreich importiert")
except ImportError as e:
    print("Fehler beim Importieren von fdm_sender:", e)