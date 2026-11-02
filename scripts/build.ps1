param([string]$OutputDirectory = "artifacts/diagnostic-win-x64")
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true `
  -p:EnableMemoryWrites=false -p:PublishSingleFile=false -p:PublishReadyToRun=false `
  -p:PublishTrimmed=false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
python scripts/verify_readonly.py "$OutputDirectory/MW2.FPS.Unlocker.dll"
if ($LASTEXITCODE -ne 0) { throw "Read-only binary verification failed" }
