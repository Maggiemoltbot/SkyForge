# Phase 1A Learnings — SkyForge

## Was haben wir gelernt?

### INRIA Dataset
- Zu langsame Upload-Geschwindigkeit (Stabilitätsprobleme)
- Kein direkter Downloadlink für große .splats
- Nicht für Echtzeitsimulation geeignet

### Voxel51 GAIA Dataset
- Komplexe Verzeichnisstruktur
- Mehrere Teil-Szenen, mühsam zu kombinieren
- Kein einheitliches Format, hoher Integrationsaufwand

### mkkellogg GitHub (erfolgreich)
- Direkter .ksplat Download via GitHub Releases
- Kompakte, vollständige Szenen
- Perfekt für Unity-Import
- Verfügbare Szenen: bonsai, garden, stump, truck (jeweils _high Varianten)

### Format: .ksplat
- Funktioniert stabil mit UnityGaussianSplatting
- Native Kompression, kleine Dateigrößen
- Gute Performance auf M4 (erste Tests)
- Unity-kompatibel ohne Konvertierung

## Nächster Schritt: Phase 1B
- RunPod wird vorbereitet für automatisierte Performance-Benchmarks
- Upload der .ksplat Szenen auf RunPod
- Skript-basiertes Rendering-Testing mit verschiedenen Einstellungen
- Metriken: FPS, GPU/CPU Last, Speicherverbrauch

**Fazit:** mkkellogg ist die beste Datenquelle für Phase 1. Phase 1B startet mit RunPod-Setup.