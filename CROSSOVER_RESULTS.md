# CrossOver FPS / FOV result

3 November 2026, Australia/Sydney. The user authorized rapid FOV and combined tests, then explicitly authorized regular operation until they close the utility.

**Result:** FPS-cap setting 120 and FOV 80 both work in the tested environment. The user confirmed a wider view in-game and normal responsiveness.

| Test | Result |
| --- | --- |
| FOV-only, 30-second automatic test | One successful four-byte float write, 65 → 80. FPS stayed 85. No logged failures/exceptions; utility shut down cleanly. |
| Combined, 90-second automatic test | Maintained FPS 120 / FOV 80. Three successful writes: initial FPS 85 → 120, FPS reset corrected at 11:58:35, and FOV reset corrected at 11:58:43. No redundant writes while values matched. No logged failures/exceptions; clean automatic shutdown. |
| Actual view | User confirmed wider FOV in-game and responsiveness during the combined test. |
| Regular no-argument startup | `MW2.CrossOver.Unlocker.exe` loaded its packaged exact-game profile, enabled both features after validation, and read back 120 / 80. Left running with explicit user approval. |
| CI | Windows and macOS passed [run 41556616268](https://github.com/carmelo343/mw2-fps-unlocker/actions/runs/41556616268). Windows: 38 diagnostic checks, 45 experimental checks, 45 release-default checks, and a passing disposable-process native-write fixture. CrossOver release compilation: zero warnings/errors. |

Environment: Apple Silicon Mac, macOS 26.5.1, CrossOver 26.3, existing Windows 10 Steam bottle, graphics Wine and MSync enabled. Game SHA-256: `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492`. Unknown executable hashes/architectures are rejected before writes. Game installation and bottle settings were not manually edited.

The ready-to-run edition starts with 120 / 80, retains the original FPS 75–500 and FOV 60–100 sliders, and applies each feature independently. It waits for validated stable dvars, checks pointers/names/types/pages, and only writes when the requested value differs. It resolves pointers again after game relaunches. Closing the utility stops updates; existing values can remain until the game resets them or restarts.

This is a short functional compatibility test, not a long-term stability certification. Actual rendered FPS was not measured; performance may be below the configured cap. Other slider targets were not exhaustively tested. No VAC-safety guarantee is made. No injection, protection change, anti-cheat bypass or stealth behavior is included.

The original upstream utility's crash was not reproduced and its exact cause remains unknown. The fork's validated write paths work at the tested defaults; the earlier wrong-current-offset hypothesis was not supported by the observed executable.

The native CrossOver launcher was saved as `MW2.CrossOver.Unlocker` in the Steam bottle. Its target is the complete folder under this task's outputs, so keep that folder intact. Raw test logs remain local in `MW2-CrossOver-functional-test-logs.zip`.
