#!/bin/bash

# ✅ Skript: fix-project-root.sh
# 🎯 Zweck: Bereinigt die Projektwurzel von SkyForge
#          von src/SkyForge/* → zu ./*
#          für klare Asset-Pfad-Referenzen
# 📅 Wann: 2026-03-30 – vor Commit: $(cd /Users/rudi/Projects/SkyForge && git log --oneline -1)
# 🛠️ Von: Maggie, CAIO @ XFLIGHT

set -euo pipefail

SCRIPT_DIR="$(dirname "$0")"
PROJECT_ROOT="/Users/rudi/Projects/SkyForge"
BACKUP_CONTEXT="${PROJECT_ROOT}/PROJECT_CONTEXT.md.bak_2026-03-30"

LOG_FILE="${SCRIPT_DIR}/fix-project-root.log"
exec > >(tee -a "$LOG_FILE") 2>&1

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $@"; }

log "🔷 Starte Umstrukturierung der Projektwurzel für SkyForge"

# ----------------------------
# 1. Prüfung: Im richtigen Verzeichnis?
# ----------------------------
cd "$PROJECT_ROOT" || { log "FEHLER: Projektverzeichnis nicht gefunden: $PROJECT_ROOT"; exit 1; }
log "Verzeichnis gewechselt zu: $PWD"

git_status=$(git status --porcelain)
if [[ -n "$git_status" ]]; then
  log "⚠️  Uncommittete Änderungen gefunden. Bitte committen oder stashen. Abbruch."
  echo "$git_status"
  exit 1
fi
log "Git-Status ist clean. Fortsetzung."

# ----------------------------
# 2. Prüfung: src/SkyForge existiert?
# ----------------------------
if [[ ! -d "src/SkyForge" ]]; then
  log "FEHLER: src/SkyForge existiert nicht. Abbruch."
  exit 1
fi
log "src/SkyForge gefunden. Fortsetzung."

# ----------------------------
# 3. Unity schließen (falls offen)
# ----------------------------
if pgrep -x "Unity" > /dev/null; then
  log "Unity ist offen. Bitte schließen – warte bis beendet... (oder drücke STRG+C zum Abbruch)"
  while pgrep -x "Unity" > /dev/null; do
    sleep 2
  done
  log "Unity wurde geschlossen. Fortsetzung."
fi

# ----------------------------
# 4. Verschieben aller Dateien aus src/SkyForge/* in Root
# ----------------------------
log "Verschiebe Dateien aus src/SkyForge/ in Projektwurzel..."
mv src/SkyForge/* .
log "Verschiebung abgeschlossen."

# ----------------------------
# 5. Leeren src/SkyForge-Ordner löschen
# ----------------------------
if [[ -d "src/SkyForge" && -z 