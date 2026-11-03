# Write-path experiment: prepared, not run against MW2

The user originally required explicit authorization at each live-memory testing stage. The work below prepares that stage; it does not establish authorization to run it.

## Completed disposable-process test

On 3 November 2026 at 11:30 Sydney time, the standalone `tests/WriteProbe` fixture passed in the existing CrossOver Steam bottle. It opens only a child process it created, accepts no external PID/address and never enumerates game processes. The child also checks its actual copy-on-write protection before reporting ready.

- Integer write/readback: 85 → 120 → 85 on `PAGE_READWRITE` memory.
- Float write/readback: 65 → 70 → 65 on a `PAGE_WRITECOPY` file mapping.
- The child independently observed each change; neighboring sentinel bytes stayed intact.
- The backing file and a separate read-only view remained unchanged.
- A `PAGE_READONLY` destination was rejected before the native write call; the runtime read-only guard also rejected a write attempt.
- The child exited and its temporary files were removed. An earlier fixture run failed before attachment because its file mapping was opened without compatible sharing; that test code was corrected, then the complete probe passed.

This establishes basic cross-process write behavior for disposable heap/file-mapped data in this bottle. MW2's memory layout, image mappings, dvar semantics and asynchronous engine behavior are not reproduced by this fixture. It does not establish game-write compatibility or account safety.

## Prepared FPS-only experiment

The experimental executable is separately named `MW2.FPS.Unlocker.Experimental.exe`. Its default is still detection-only. The diagnostic executable continues to compile without the native write import and rejects all write modes.

`--mode fps-once --fps N --profile FILE` performs at most **one four-byte FPS write attempt per utility run**, after ten seconds of stable validated FPS/FOV pointers and values. It verifies the exact executable SHA-256, architecture, expected dvar names/types, readable current values, and a committed non-executable writable destination. It refreshes the FPS record immediately before writing, checks the transferred byte count and readback, then continues reading. It never writes FOV in this mode, never repeats automatically after a game restart, and never retries a failed attempt. The FPS/FOV continuous-write checkboxes are disabled in this mode.

Build only:

```sh
./scripts/build-experimental.sh
```

Proposed live tests, **pending explicit approval**:

1. Start MW2 normally and confirm a responsive main menu. Start one experimental utility in `fps-once` mode to write the current FPS value back unchanged using `--fps current` (it was observed as 85 in earlier reads). Observe stability and the log; stop on any error.
2. If approved and the first test is stable, close the utility and run a new single-write experiment targeting 120 FPS. Confirm native readback and whether the value persists. FPS readback alone does not establish rendered frame rate; that needs a separate in-game measurement. Do not assume a one-time write will persist—the engine may reset it.
3. Only after reviewing results, consider bounded repeated FPS writes, followed by a separately authorized FOV-only test. Do not immediately enable both continuous toggles.

Test command templates for the separately packaged experimental folder, **not commands to run before approval**:

```text
"<folder>\MW2.FPS.Unlocker.Experimental.exe" --mode fps-once --fps current --profile "<profile>" --log-dir "C:\mw2-fps-once-logs"
"<folder>\MW2.FPS.Unlocker.Experimental.exe" --mode fps-once --fps 120 --profile "<profile>" --log-dir "C:\mw2-fps-once-logs"
```

The `current` keyword takes the freshly revalidated current value for the first test; it does not assume that the earlier value of 85 is still present. The workspace candidate profile was verified only for reads on executable SHA-256 `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492`; it is not labelled write-validated or shipped as a trusted public profile.

Game-memory modification can crash the game and carries VAC/account risk. A menu or offline session is not a guarantee of safety. No protection changes, injection, anti-cheat disabling, stealth or bypass is part of this plan. Authorization and an appropriate test environment must be established before game writes. No original values are automatically written back at detach; restart the game to reset state.

## Final build validation

[CI run 41553825461](https://github.com/carmelo343/mw2-fps-unlocker/actions/runs/41553825461) passed for source commit `a6b953c` on macOS and Windows. The diagnostic safety harness passed 35 checks on macOS and 38 on Windows; the experimental parser/budget harness passed 39 on macOS and 42 on Windows. The disposable native write probe also passed on Windows. The separately compiled experimental build had zero warnings/errors, and the normal diagnostic assembly again passed its no-native-write-import check. These checks do not test a live game write.
