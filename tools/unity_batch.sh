#!/bin/bash
# SkyForge Unity Batch Mode Runner
UNITY="/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity"
PROJECT="/Users/rudi/Projects/SkyForge/src/SkyForge"
LOG="/Users/rudi/Projects/SkyForge/logs/batch_$(date +%Y%m%d_%H%M%S).log"

mkdir -p "$(dirname "$LOG")"

METHOD="${1:-SkyForge.Editor.SkyForgeCommandHandler.HealthCheck}"

echo "Running: $METHOD"
echo "Log: $LOG"

"$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod "$METHOD" -quit -logFile "$LOG" 2>&1

echo "Exit code: $?"
echo "Last 20 lines of log:"
tail -20 "$LOG"