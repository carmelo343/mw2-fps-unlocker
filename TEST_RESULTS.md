# Validation record

Date: 3 November 2026 (Australia/Sydney).

Environment observed: macOS arm64 host; CrossOver 26.3 present; SDK 10.0.403 installed within this task workspace. Game installation and bottle settings were not changed. Runtime tests subsequently created only diagnostic log files in the bottle.

| Check | Result |
| --- | --- |
| Git and authenticated `gh` | Passed (account `carmelo343`; network/keychain require normal host access) |
| Fork, clone, origin/upstream, feature branch | Completed |
| Complete upstream C# source/history, resources, project/dependencies, MIT review | Completed |
| Diagnostic Windows-targeted compilation on macOS | Passed, zero warnings/errors |
| Portable policy/PE/profile/parser/log checks | 30 passed; Windows native fixture skipped locally |
| Managed diagnostic symbol scan | Passed; no native memory-write, protection-change or injection import |
| Self-contained Windows x64 publish | Passed; self-contained folder with Windows x64 .NET/WinForms runtime |
| Experimental path compilation | Passed, zero warnings/errors; no game writes performed |
| Windows owned-memory fixture / CI | Passed: 33 checks on Windows; both macOS and Windows CI jobs succeeded |
| GUI in CrossOver | WinForms startup and UI callbacks succeeded under bundled .NET 10.0.14; Wine window capture was unavailable to automation |
| Detection and lifecycle | Live process detection and normal-exit/relaunch observed in logs; see runtime record for sequence |
| Read-only inspection and repeated game launches | Live attachment, PE/architecture validation, candidate reads and dvar name/type checks succeeded; see runtime record |
| FPS-only single writes | Passed: 85 → 85 and 85 → 120, one write per utility run, no reported crash. 120 persisted in samples for 84 seconds, then reverted to 85 by 86 seconds. No repeat after game restart. |
| Repeated FPS / FOV-only / combined writes | Not tested; further stage approval required |
| Exact crash operation and root cause | Not established. Reads succeed; current upstream offsets/name/type/value layout match the observed executable. Original write behavior was not run. |

CI evidence: [successful run 41547424745](https://github.com/carmelo343/mw2-fps-unlocker/actions/runs/41547424745), tested commit `1ed09a3`. An earlier Windows run caught a test reader file-sharing error; that fixture was corrected and the full workflow passed. The native fixture reads its own allocated memory and verifies that a diagnostic write attempt is rejected without changing it. It never opens a game process.

Build checks do not establish runtime compatibility. The diagnostic artifact intentionally cannot modify game memory. The experimental code is scaffolding for a later verified profile, not a compatibility claim.

Later user-authorized live evidence is documented in [RUNTIME_TEST_RESULTS.md](RUNTIME_TEST_RESULTS.md). These main-menu tests do not establish online-match stability, VAC safety, or memory-write compatibility.

The later write-path preparation is recorded in [WRITE_TEST_PLAN.md](WRITE_TEST_PLAN.md): the disposable child-process write probe passed in CrossOver and Windows CI. Updated diagnostic checks passed 35/38 (macOS/Windows), and experimental parser/budget checks passed 39/42. The later authorized FPS-only single-write results are recorded in [FPS_WRITE_RESULTS.md](FPS_WRITE_RESULTS.md); further write stages remain untested.

The FPS write session completed two successful four-byte writes and 1,712 successful reads with zero logged API failures/exceptions. Actual rendered frame rate was not measured. The game and experimental utilities were closed cleanly.

Latest result: [CROSSOVER_RESULTS.md](CROSSOVER_RESULTS.md). FOV-only and combined FPS/FOV tests passed, both reset corrections were observed, wider FOV was confirmed in-game, and the regular no-argument build started successfully. Earlier untested-stage statements above describe prior checkpoints.
