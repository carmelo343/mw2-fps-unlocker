# Live FPS write test results

3 November 2026, Australia/Sydney. The user explicitly authorized two FPS-only tests: an unchanged-value write, followed by one write targeting 120 if stable. No FOV write or continuous-writing test was authorized or performed.

Environment: existing Steam Windows 10 bottle, CrossOver 26.3, macOS 26.5.1 on Apple Silicon, graphics setting Wine, MSync enabled. Bottle settings and game files were not manually changed. The experimental binary was built from source commit `a6b953c`; its executable, assembly and profile matched the reviewed package manifest before launch.

Game executable SHA-256: `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492` (x64, eight-byte pointers).

| Test | Write time (Sydney) | Exact operation | Observed outcome |
| --- | --- | --- | --- |
| Unchanged value | 11:39:04 | One four-byte integer write, 85 → 85 | API reported four bytes transferred; exact readback verified. Further reads stayed at 85 for 134 seconds before utility shutdown. User confirmed a responsive menu. |
| Target 120 | 11:42:05 | One four-byte integer write, 85 → 120 | API reported four bytes transferred; exact readback verified. Later reads showed 120 through 84 seconds after the write; the setting was back at 85 by 11:43:32 (86 seconds after the write), before game exit. User confirmed a responsive menu. |
| Game restart | 11:45:02 detection | Read-only observation after the second test | FPS was 85 and FOV was 65; FPS had already reverted before the preceding exit. The utility reattached, but its write count stayed at one for the entire second utility run. No automatic repeat occurred. |

Across both logs: **two native write attempts, two successful four-byte transfers, zero logged API failures or diagnostic exceptions, and 1712 successful native read completions**. Both write addresses matched the validated FPS field; neither write targeted FOV. All FOV samples were 65. Both experimental utility runs ended with `STOP clean shutdown`.

The agent launched the game and utilities through CrossOver and inspected logs. The user confirmed menu responsiveness and performed normal closes because the computer-control tool could not access Wine-hosted windows. At the end, the game and utility were closed, with the restarted game's last observed values back at 85 FPS / 65 FOV.

## What this establishes

For this exact executable and environment, the fork's guarded single FPS write works at the tested targets. Basic attachment, pointer/name/type validation, native writes to the observed copy-on-write data page, transfer checking and readback did not reproduce the reported crash. The setting reverted to 85 between the last 120 sample and the first 85 sample, approximately 84–86 seconds after the write. A single write therefore did not maintain the requested limit for the full session. The trigger for the reversion was not isolated; a bounded FPS-only repeat test is a useful next experiment, but has not been authorized or run.

This records the `com_maxfps` setting, **not measured rendered frame rate**. No FPS counter result was supplied. Online matches, map transitions, long-term stability, other target values, FOV writes, simultaneous FPS/FOV changes and continuous 250 ms writes remain untested. These results do not establish VAC safety.

The original unlocker's crash was not reproduced and its cause remains unknown. The successful tests argue against a general inability to write MW2 memory in CrossOver, or a wrong current-upstream FPS value offset for this executable. They do not exclude a FOV-specific operation, another unlocker build, startup timing or repeated/combined writes; none is established as the cause.

No further live write is included in the completed authorization. A later FOV-only or repeated-FPS test needs a separately reviewed experiment and explicit stage approval. The ordinary diagnostic build still has no native write import; the experimental default remains detection-only.

Raw logs are saved locally in `MW2-FPS-write-test-logs.zip`. Only this summary is intended for the public fork.
