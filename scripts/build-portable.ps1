param([string]$OutputDirectory = "artifacts/portable-win-x64")
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet publish mw2-fps-unlocker.csproj -c Release -r win-x64 --self-contained true `
  -p:EnableMemoryWrites=true -p:CrossoverRelease=true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -p:DebugType=embedded -p:PublishReadyToRun=false -p:PublishTrimmed=false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw "Portable publish failed" }
