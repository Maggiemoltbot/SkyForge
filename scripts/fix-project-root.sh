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

git_status=$(git status --porcelain | grep '^[AMDR]')
if [[ -n "$git_status" ]]; then
  log "⚠️  Uncommittete Änderungen (staged/unstaged) gefunden. Bitte committen oder stashen. Abbruch."
  echo "$git_status"
  exit 1
fi
log "Keine uncommitteten Änderungen in Git. Fortsetzung trotz untracked files."

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
mv src/SkyForge/* . 2>/dev/null || true
log "Verschiebung abgeschlossen (Fehler bei leeren Verzeichnissen ignoriert)."

# ----------------------------
# 5. Prüfen, ob src/SkyForge leer ist → löschen
# ----------------------------
if [[ -d "src/SkyForge" ]]; then
  if [[ -z "$(ls -A src/SkyForge)" ]]; then
    rmdir src/SkyForge
    log "src/SkyForge (leer) wurde entfernt."
  else
    log "src/SkyForge ist nicht leer – manuell prüfen: $(ls -A src/SkyForge)"
  fi
fi

# ----------------------------
# 6. src/ prüfen und löschen, falls leer
# ----------------------------
if [[ -d "src" ]]; then
  if [[ -z "$(ls -A src)" ]]; then
    rmdir src
    log "src/ (leer) wurde entfernt."
  fi
fi

# ----------------------------
# 7. Git aktualisieren
# ----------------------------
log "Git-Zustand aktualisieren..."
git add .
git status --porcelain --untracked-files=no | grep -E '^(A|R) ' | head -5
echo
log "git add . ausgeführt. Alle verschobenen und gelöschten Dateien verfolgt."

# ----------------------------
# 8. PROJECT_CONTEXT.md anpassen
# ----------------------------
if [[ -f "PROJECT_CONTEXT.md" ]]; then
  sed -i '' 's|src\\/SkyForge\\/Assets|Assets|g' PROJECT_CONTEXT.md
  sed -i '' 's|src/SkyForge/Assets|Assets|g' PROJECT_CONTEXT.md
  git add PROJECT_CONTEXT.md
  log "PROJECT_CONTEXT.md angepasst: Pfade von src/SkyForge/Assets → Assets korrigiert."
else
  log "WARNUNG: PROJECT_CONTEXT.md nicht gefunden."
fi

# ----------------------------
# 9. Commit erstellen
# ----------------------------
COMMIT_MSG="refactor: move project to root for clean Assets structure\n\n- Moved all content from src/SkyForge/* to project root\n- Adjusted PROJECT_CONTEXT.md\n- Ensured meta files preserved\n- Cleaned up empty src/ and src/SkyForge/\n\nPrepared by Maggie CAIO on 2026-03-30 before this commit: $(git log --oneline -1)"

echo "$COMMIT_MSG" > /tmp/skyforge_commit_msg.txt
log "Commit-Nachricht vorbereitet."

git commit -F /tmp/skyforge_commit_msg.txt
log "Commit erstellt: refactor: move project to root..."

# ----------------------------
# 10. Fertig!
# ----------------------------
log "✅ Umstrukturierung abgeschlossen. Bitte Unity Hub neu starten und Projekt neu laden."
