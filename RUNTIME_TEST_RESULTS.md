# CrossOver read-only runtime test results

This record covers the earlier read-only session. Later authorized FPS-only writes are documented in [FPS_WRITE_RESULTS.md](FPS_WRITE_RESULTS.md).

3 November 2026, Australia/Sydney. Tests used the existing Steam bottle. The agent launched the applications through CrossOver and inspected diagnostic logs; the user confirmed the responsive game menu and performed normal closes because the computer-control tool could not access Wine-hosted windows.

## Environment and scope

- macOS 26.5.1, Apple Silicon; CrossOver 26.3, Windows 10 Steam bottle.
- Graphics setting shown as Wine; MSync was enabled. Bottle settings were left unchanged.
- Diagnostic Windows x64 self-contained build, bundled .NET 10.0.14. Native memory-write implementation absent.
- Game PE machine `0x8664`, eight-byte pointers, image size `0x952C000`.
- Game SHA-256: `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492`.
- Main-menu testing only. No online match, original-unlocker reproduction, FPS write, or FOV write test was performed. No game files were manually edited or replaced. The only new bottle files were diagnostic logs.

## Observed launch sequence

| Launch | Wine PID | Detection (Sydney) | Exit observed (Sydney) | Approx. detected lifetime |
| --- | --- | --- | --- | --- |
| 1 | 1296 | 11:12:35 | 11:15:23 | 169 seconds |
| 2 | 1836 | 11:15:51 | 11:17:53 | 122 seconds |
| 3 | 2208 | 11:18:24 | 11:20:19 | 115 seconds |


Detection ran across all three launches. Raw read-only inspection joined during launch 1 and reattached automatically for launches 2 and 3. A third diagnostic instance used a workspace-only candidate profile for read-only name/type validation on launches 2 and 3. This was a combined limited lifecycle test, not three isolated runs of each mode.

## Findings

- Process detection, module enumeration, query/read attachment, process architecture reporting, disk/live PE consistency checks, page queries, and memory reads succeeded.
- 1514 successful native read completions; 0 logged API failures or diagnostic exceptions across the three captured logs. All three diagnostic runs ended with `STOP clean shutdown`.
- Startup null dvar globals were skipped; inspection later found registered dvars without restarting the utility.
- Name/type checks confirmed the current upstream RVAs resolve to `com_maxfps` and `cg_fov` for this executable. Observed types at `+0xC` were 5 and 1; current values at `+0x10` were 85 and 65 respectively. Confirmed readings: cg_fov=65, com_maxfps=85.
- Candidate data pages were reported as `PAGE_WRITECOPY` (`0x08`). That is an observation, not proof of a Wine defect.
- The user reported responsive menus and no crash during this limited sequence. Diagnostic process lifetimes and successful reads support those observations; they do not prove long-term or gameplay stability.

## Revised crash assessment

The earlier source-only suspicion of incorrect current-upstream RVAs or a wrong `+0x10` field is not supported by this executable's observed names, types and values. There is no evidence here to justify changing these offsets. Process attachment and reads did not reproduce the reported crash.

The original unlocker still automatically writes both settings after detection, so its write path remains untested and relevant. Its installed binary identity could not be established from the registered launcher target, which was not present at its recorded path. Do not assume the installed binary matches current upstream. The exact crash operation/root cause remains unknown.

No profile is published as write-validated. Any later FPS-only or FOV-only test needs separate explicit authorization and an appropriate test environment. These results make no VAC-safety claim.

Raw logs are saved locally in `MW2-runtime-logs.zip`; they were not uploaded to the public fork. Documentation alone is committed to the feature branch.
