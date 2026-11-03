# Portable EXE release 1.4.1-crossover.1

The portable release is a single self-contained Windows x64 EXE. Carry only `MW2.CrossOver.Unlocker.Portable.exe`; no DLLs, JSON profile, installer or separately installed .NET runtime are required. Run it through CrossOver in the same existing Steam bottle as MW2. Normal startup keeps the FPS-cap setting at 120 and FOV at 80; the same sliders and independent switches remain available.

The supported exact-game profile and MIT license are embedded in the application. Unknown executable hashes are still rejected. The **License** button displays the included MIT notice.

This is single-file distribution, not zero disk activity: .NET extracts bundled native libraries to a per-user temporary cache on startup, and the utility writes its normal diagnostic logs inside the bottle. See [Microsoft's single-file documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

## Build

macOS/Linux:

```sh
./scripts/build-portable.sh
```

Windows PowerShell:

```powershell
./scripts/build-portable.ps1
```

Output: `artifacts/portable-win-x64/MW2.CrossOver.Unlocker.exe`. The EXE can be moved or renamed. Build settings include self-contained win-x64, single-file publishing, bundled native libraries, compressed assemblies, embedded symbols, no trimming and no ReadyToRun compilation. SDK version is pinned in `global.json`.

## Validation

- Publish output contained exactly one file, the EXE (49.7 MiB).
- The application assembly contains the exact supported-game JSON and MIT license resources.
- A copy was placed in an isolated directory with no companion files and launched inside the existing CrossOver Steam bottle using read-only mode and a five-second shutdown timer. WinForms startup and clean shutdown succeeded with no logged exception/API failure or memory-write call. No game process was running for that packaging smoke test; live FPS/FOV behavior was previously tested with the same control logic in the folder build.
- [CI run 41558249168](https://github.com/carmelo343/mw2-fps-unlocker/actions/runs/41558249168) passed on Windows and macOS for source commit `02716cd`. Windows CI also launched the single EXE from an isolated directory and verified clean automatic shutdown. Profile-loading and release-default checks passed.

The supported environment, exact game fingerprint, quick in-game FPS/FOV results and limits remain documented in `CROSSOVER_RESULTS.md`. The portable package changes distribution/profile loading, not the FPS/FOV write policy. It does not establish broader game-version compatibility, measured rendered FPS, long-term stability or VAC safety.
