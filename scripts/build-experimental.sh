#!/usr/bin/env bash
# Builds only; does not launch or arm a game-memory write.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet_cmd="${DOTNET:-dotnet}"
out="${1:-artifacts/experimental-win-x64}"
"$dotnet_cmd" publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true \
  -p:EnableMemoryWrites=true -p:PublishSingleFile=false -p:PublishReadyToRun=false \
  -p:PublishTrimmed=false -o "$out"
