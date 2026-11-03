# Using the CrossOver edition

The FPS/FOV edition passed a short in-game test at **FPS-cap setting 120 / FOV 80**, including correcting resets of both settings. See [CROSSOVER_RESULTS.md](CROSSOVER_RESULTS.md) for exact evidence and limits.

The single-file edition needs only its EXE; see [PORTABLE_RELEASE.md](PORTABLE_RELEASE.md). The profile is embedded in current release builds.

## Run

1. Extract the complete CrossOver unlocker folder somewhere separate from the game installation.
2. In CrossOver, select the existing **Steam** bottle and choose **Run Command**.
3. Browse to `MW2.CrossOver.Unlocker.exe` in that folder, then Run. No extra arguments are needed.
4. Start MW2 normally through Steam in the same bottle, or start the utility after the game. Once the executable and settings are validated, both features enable automatically at 120 / 80.
5. Use the sliders to adjust FPS/FOV. Use the individual switches to stop applying either feature. Keep the utility open while playing. Closing it stops further updates; it does not force old values back into memory.

A launcher named **MW2.CrossOver.Unlocker** has already been saved in this Mac's Steam bottle. It points to the complete `outputs/MW2-CrossOver-Unlocker` folder from this task; keep that folder intact.

## Build

Install the SDK pinned in `global.json` and run:

```sh
./scripts/build-crossover.sh
```

Output: `artifacts/crossover-win-x64`. Copy the entire directory, including the runtime files for folder builds. The portable build instead bundles everything into the EXE. The script bundles Windows x64 .NET/WinForms, so a separate .NET install inside the bottle is not required. Microsoft Windows targeting/runtime packs require NuGet access. The MIT license and upstream copyright remain unchanged.

`./scripts/build.sh` builds the separate diagnostic-only edition with no native memory-write import. The experimental build retains explicit patch/single-write modes and optional `--duration` for automatic test shutdown. Use diagnostic `--mode detect` or `--mode read-only` if investigating another game build.

## Supported test environment and limits

Observed environment: Apple Silicon, macOS 26.5.1, CrossOver 26.3, Steam Windows 10 bottle, graphics Wine, MSync enabled. The packaged profile permits only game SHA-256 `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492`, x64. If Steam updates the executable, the profile match fails and writes stop; do not substitute guessed offsets or just change the hash.

The functional tests confirmed FPS-setting writes, FOV 80 visibly in-game, and correction of resets to both settings. This does not certify long-term stability or measured rendered FPS. The original ranges are retained (75–500 FPS and 60–100 FOV); defaults 120/80 are the tested combination. No VAC safety is guaranteed.

## Troubleshooting

- **Unknown build / inspection stopped:** click **Show log path**. Send the log and exact game version. Do not edit the profile to bypass identity checks.
- **FPS/FOV resets:** keep the utility running and the relevant apply switch enabled. It checks every 250 ms and rewrites only changed values.
- **Performance below 120:** the FPS setting is a cap, not a guarantee of rendered frame rate. Performance depends on the game and environment.
- **Crash:** close the utility and preserve its log plus the Wine Debugger report. Do not keep retrying writes against an unvalidated build.
- **Logs:** normally `%LOCALAPPDATA%\MW2FPSUnlocker\logs` inside the bottle; **Show log path** displays the exact location. Logs rotate at 10 MiB with one preceding segment.

Handles, pointer width, PE identity, dvar names/types, current values, committed page permissions, API results, transfer counts and readback are checked. No code injection, protection changes, bypasses or stealth features are used. The original crash cause remains unknown; current-upstream FPS/FOV offsets matched the tested game.

Earlier investigations are retained in [RUNTIME_TEST_RESULTS.md](RUNTIME_TEST_RESULTS.md), [FPS_WRITE_RESULTS.md](FPS_WRITE_RESULTS.md), and [WRITE_TEST_PLAN.md](WRITE_TEST_PLAN.md).
