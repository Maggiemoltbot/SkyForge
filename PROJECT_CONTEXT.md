# PROJECT_CONTEXT.md – SkyForge

## Projektstruktur
- Root: `/Users/rudi/Projects/SkyForge`
- Assets: `Assets/`
- Scripts: `Assets/Scripts/`
- Prefabs: `Assets/Prefabs/`
- Scenes: `Assets/Scenes/`
- UI: `Assets/UI/`
- Project Settings: `ProjectSettings/`
- Packages: `Packages/`

## Letzte Anpassung
- **2026-03-31**: FlightDynamicsBridge Socket-Reinitialisierung, StartScreen Null-Schutz, SITL-Build auf macOS gefixt (Makefile + Script-Update).
  - Durchgeführt von: Maggie, CAIO @ XFLIGHT
- **2026-03-30**: Projektwurzel bereinigt – `src/SkyForge/` entfernt, `Assets/` direkt in Root verschoben.
  - Durchgeführt von: Maggie, CAIO @ XFLIGHT
  - Vorheriger Zustand: `src/SkyForge/Assets/...`
  - Neuer Zustand: `Assets/...`


## Git
- Repository: `https://github.com/Maggiemoltbot/SkyForge.git`
- Branch: `main`
- Letzter Commit: $(cd /Users/rudi/Projects/SkyForge && git log --oneline -1)

---

*Diese Datei wird automatisch gepflegt, um die Projektstruktur nachvollziehbar zu halten