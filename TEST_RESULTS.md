# Validation record

Date: 3 November 2026 (Australia/Sydney).

Environment observed: macOS arm64 host; CrossOver 26.3 present; SDK 10.0.403 installed within this task workspace. Game installation and bottle contents were not changed.

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
| GUI in CrossOver | Not tested; user stage 1 |
| Detect-only stability and repeated game launches | Not tested; user stage 2 |
| Read-only inspection and repeated game launches | Not tested; user stage 3 |
| FPS-only / FOV-only / combined patch stability | Not tested; blocked pending exact profile validation and explicit stage authorization |
| Exact crash operation and root cause | Not yet established; automatic unchecked writes are the leading source-based hypothesis |

CI evidence: [successful run 41547424745](https://github.com/carmelo343/mw2-fps-unlocker/actions/runs/41547424745), tested commit `1ed09a3`. An earlier Windows run caught a test reader file-sharing error; that fixture was corrected and the full workflow passed. The native fixture reads its own allocated memory and verifies that a diagnostic write attempt is rejected without changing it. It never opens a game process.

Build checks do not establish runtime compatibility. The diagnostic artifact intentionally cannot modify game memory. The experimental code is scaffolding for a later verified profile, not a compatibility claim.
