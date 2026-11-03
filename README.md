# MW2 2009 FPS/FOV Unlocker — CrossOver investigation

This MIT-licensed fork of [andersblomqvist/mw2-fps-unlocker](https://github.com/andersblomqvist/mw2-fps-unlocker) adds staged diagnostics for Steam MW2 (2009) multiplayer running in Wine/CrossOver.

**Detection and read-only inspection passed limited CrossOver main-menu tests. FPS/FOV patching remains untested. The default build cannot write game memory.** It starts in detection-only mode; `--mode read-only` adds validated module/header inspection and timestamped API logs. Neither mode applies FPS or FOV changes.

Read [CROSSOVER.md](CROSSOVER.md) for build/run commands, the source-based crash hypothesis, the incremental test procedure, and limitations. [TEST_RESULTS.md](TEST_RESULTS.md) distinguishes completed build checks from pending game testing.

```sh
dotnet run --project tests/Diagnostics.Tests.csproj -c Release
./scripts/build.sh
```

Requires the .NET SDK pinned in `global.json` and Python 3 for the read-only assembly check. Builds a self-contained Windows x64 folder on macOS or Windows (`scripts/build.ps1`); keep all published files beside the executable. No game files are replaced.

The original FPS/FOV ranges and polling functionality remain in an experimental source path, requiring a separately enabled build, an independently verified exact-executable profile, explicit patch mode, and individual GUI toggles. No profile is shipped. Do not test writes against a live game without explicit authorization for that stage. No VAC safety claim, anti-cheat bypass, injection or stealth behavior is provided.
