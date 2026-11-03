#!/usr/bin/env bash
# The supported-game profile is copied after the explicit release build.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet_cmd="${DOTNET:-dotnet}"
out="${1:-artifacts/crossover-win-x64}"
"$dotnet_cmd" publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true \
  -p:EnableMemoryWrites=true -p:CrossoverRelease=true -p:PublishSingleFile=false \
  -p:PublishReadyToRun=false -p:PublishTrimmed=false -o "$out"
cp profiles/mw2-profile.json "$out/mw2-profile.json"
