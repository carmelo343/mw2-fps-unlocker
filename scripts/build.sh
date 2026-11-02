#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet_cmd="${DOTNET:-dotnet}"
out="${1:-artifacts/diagnostic-win-x64}"
"$dotnet_cmd" publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true \
  -p:EnableMemoryWrites=false -p:PublishSingleFile=false -p:PublishReadyToRun=false \
  -p:PublishTrimmed=false -o "$out"
python3 scripts/verify_readonly.py "$out/MW2.FPS.Unlocker.dll"
