# GS Scene Manager — CONTEXT.md
**Modul:** GSScene
**Pfad:** /Users/rudi/Projects/SkyForge/Assets/Modules/GSScene
**Beschreibung:** Lädt und verwaltet Gaussian Splatting Szenen (.ksplat) in Unity. Bietet Schnittstelle für Dynamik (z. B. Wind auf Bäume) und Level-of-Detail-Steuerung.

## Verfügbare GS-Modelle
- bonsai.ksplat (Standard)
- bonsai_high.ksplat (High Poly)
- bonsai_trimmed.ksplat (gekürzt)
- garden.ksplat
- garden_high.ksplat
- stump.ksplat
- stump_high.ksplat
- truck.ksplat
- truck_high.ksplat

## Import-Workflow
1. GS-Dateien nach `/Users/rudi/Projects/SkyForge/assets/mkkellogg_gs/[szene]/` kopieren
2. In Unity: Rechtsklick im Project-Fenster → Import New Asset
3. Mesh erstellen oder direkt als Splat laden
4. Prefab anlegen (z. B. `GS_Bonsai.prefab`)
5. Im Scene Manager per Script laden

## Performance-Metriken
- Wird in Phase 1B auf RunPod gemessen
- Ziel: >30 FPS bei 4K, DLSS aktiviert
- CPU Last <70% (M4 Pro)
- GPU Last <85% (M4 Pro Grafik)

## Nächster Schritt
Phase 1B: Setup RunPod für automatisierte Performance-Tests und Benchmarking.