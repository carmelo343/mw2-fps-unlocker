#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet_cmd="${DOTNET:-dotnet}"
out="${1:-artifacts/portable-win-x64}"
"$dotnet_cmd" publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true \
  -p:EnableMemoryWrites=true -p:CrossoverRelease=true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -p:DebugType=embedded -p:PublishReadyToRun=false -p:PublishTrimmed=false -o "$out"
