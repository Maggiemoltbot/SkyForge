using UnityEngine;

public static class DroneSetup
{
    public static void Setup()
    {
        Debug.Log("[DroneSetup] Initialisierung der Drohne startet...");

        // Schritt 1: Finde den Drohnen-Controller
        DroneController droneController = Object.FindObjectOfType<DroneController>();
        if (droneController == null)
        {
            Debug.LogError("[DroneSetup] Kein DroneController im Szenen-Setup gefunden!");
            return;
        }
        
        droneController.name = "SkyForge_Drone";
        Debug.Log("[DroneSetup] DroneController gefunden und benannt.");

        // Schritt 2: Finde und konfiguriere die Flight Dynamics Bridge
        FlightDynamicsBridge fdb = Object.FindInChildrenWithTag<FlightDynamicsBridge>(GameObject.Find("SkyForge_Drone"), "FlightBridgeTag");
        if (fdb == null)
        {
            // Falls kein spezieller Bridge-GameObject existiert, versuche, das Skript direkt zu finden
            fdb = Object.FindObjectOfType<FlightDynamicsBridge>();
        }
        
        if (fdb == null)
        {
            Debug.LogError("[DroneSetup] Keine FlightDynamicsBridge im Szenen-Setup gefunden!");
            return;
        }
        
        fdb.droneController = droneController;
        fdb.Start(); // Startet die UDP-Verbindung
        Debug.Log("[DroneSetup] FlightDynamicsBridge konfiguriert und UDP-Verbindung gestartet.");
        
        // Schritt 3: Finde und verbinde den RC Input Bridge
        RCInputBridge rcInput = Object.FindObjectOfType<RCInputBridge>();
        if (rcInput != null)
        {
            rcInput.flightDynamicsBridge = fdb;
            Debug.Log("[DroneSetup] RCInputBridge mit FlightDynamicsBridge verknüpft.");
        }
        else
        {
            Debug.LogWarning("[DroneSetup] Kein RCInputBridge in der Szene gefunden. RC-Eingaben können nicht verarbeitet werden.");
        }

        // Schritt 4: Finde und verbinde die FPV-Kamera
        Camera fpvCamera = droneController.GetComponentInChildren<Camera>(true);
        if (fpvCamera != null)
        {
            fpvCamera.gameObject.tag = "MainCamera";
            fpvCamera.gameObject.name = "FPVCamera";
            Debug.Log("[DroneSetup] FPV-Kamera als 'MainCamera' markiert.");
        }
        else
        {
            Debug.LogWarning("[DroneSetup] Keine FPV-Kamera auf dem Drohnen-Objekt gefunden!");
        }

        // Schritt 5: Finde und aktiviere ein vorhandenes GS-Rendering-Objekt
        var gsRenderer = Object.FindObjectOfType<UnityGaussianSplatting>();
        if (gsRenderer != null)
        {
            gsRenderer.gameObject.SetActive(true);
            Debug.Log("[DroneSetup] UnityGaussianSplatting-Renderer gefunden und aktiviert.");
        }
        else
        {
            Debug.Log("[DroneSetup] Kein UnityGaussianSplatting-Renderer in der Szene. Lade nun die Standard-GS-Map (GS-Map-01).");
            // Hier könnte der Code zum dynamischen Laden der Standard-Map stehen
        }

        Debug.Log("[DroneSetup] Komplette Initialisierung abgeschlossen.");
    }

    // Hilfsmethode, um Object.FindObjectOfType in Kind-Objekten zu emulieren (Unity hat keinen direkten Befehl dafür)
    public static T Object.FindInChildrenWithTag<T>(GameObject parent, string tag) where T : Component
    {
        if (parent == null) return null;
        
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
            {
                return child.GetComponent<T>();
            }

            // Rekursiver Aufruf für Unterelemente
            T found = Object.FindInChildrenWithTag<T>(child.gameObject, tag);
            if (found != null)
                return found;
        }
        return null;
    }
}
