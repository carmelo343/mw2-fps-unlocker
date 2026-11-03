# CrossOver investigation and diagnostic build

Status: **detection and read-only inspection have now been exercised in CrossOver; FPS/FOV modification remains untested.** The delivered executable cannot write game memory. Do not substitute the upstream executable during these tests.

## What the source establishes

Baseline: upstream `ce77a4d` (use `git rev-parse upstream/main` for the full checked-out revision). The exact abbreviated revision is recorded in `git log`; the relevant implementation change is [`a6522d1`](https://github.com/andersblomqvist/mw2-fps-unlocker/commit/a6522d10e989a19e8abfbef6a679275fd5d67fac).

- Detection constructs a `MemoryHandler` using `Process.Handle` and `MainModule.BaseAddress`.
- A `BackgroundWorker` immediately calls `SetFPS()` and then `SetFOV()`, repeating every 250 ms. Scrolling also writes from the UI thread. There is no read-only period.
- It reads eight-byte pointers at module-relative RVAs `0x1D260A8` and `0x809C78`, then writes four bytes at each nonzero pointer plus `0x10`.
- Read failures become null/zero. Write return values, transferred byte counts and Win32 errors are discarded. A nonzero pointer is the only target check.
- There is no executable hash, target architecture, dvar name/type, page protection or structure-layout check. Workers are not cancelled on form close, process wrappers are not disposed, and the background worker reads GUI controls directly.
- The recent 64-bit migration changed the old x86 pointer/value scheme (`+0xC`) to `+0x10` without evidence of a validated layout in the repository. An older installed unlocker may instead contain obsolete absolute x86 addresses. Record the actual unlocker version/hash when comparing results.

**Initial source-based hypothesis, not a proven root cause (see runtime update below):** one of these automatic writes targets an outdated RVA or the wrong field in the 64-bit dvar structure. Pointer expansion and alignment can shift fields by more than four bytes; `+0x10` could refer to metadata rather than the value. We must examine this executable's layout before choosing another offset. A read-access fault later inside `iw4mp` can be a delayed consequence of earlier corruption. The supplied symptom alone cannot distinguish FPS from FOV, or exclude a Wine issue.

### Runtime evidence, 3 November 2026

Manual CrossOver launches plus diagnostic logs established that process discovery, module enumeration, `OpenProcess` with access `0x410`, `IsWow64Process2`, `VirtualQueryEx` and `ReadProcessMemory` work in this bottle. The user confirmed a responsive MW2 main menu. The observed executable is x64 with SHA-256 `926F8A47959F2B24420898D07F599883E0866050D42D5289F94EEF546A4A3492`.

A workspace-only candidate profile was used exclusively with the diagnostic binary's read-only mode. It validated the expected `com_maxfps` and `cg_fov` names at the current upstream RVAs, type bytes 5 and 1 at offset `+0xC`, and current values 85 and 65 at `+0x10`. This weakens the initial incorrect-offset/layout hypothesis for this exact executable. It does not establish write safety, prove the identity of the user's original unlocker, or reproduce its crash. No replacement offset is justified by these observations.

The queried candidate data pages had protection `0x08` (`PAGE_WRITECOPY`). This is an observed property, not evidence of a Wine defect. Separate FPS/FOV write tests remain unauthorized and were not run. See `TEST_RESULTS.md` and `RUNTIME_TEST_RESULTS.md` for the measured sequence and its limits.

The P/Invoke address and SIZE_T parameters in current upstream are already pointer-sized; blindly converting more integers to 64-bit does not establish a fix. [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory) reports failures through its return value, byte count and last error. The inspector now checks all three. [Wine's current implementation](https://github.com/wine-mirror/wine/blob/master/dlls/kernelbase/memory.c) implements read/query/write APIs; their presence does not prove behavior in the installed CrossOver version. The new inspector does not depend on patching executable pages or changing protection.

## Build from macOS or Windows

The project is an MIT-licensed C# WinForms application, targeting .NET 10 and Windows x64. MIT notices are retained. Its unused Microsoft.VisualBasic package reference was removed; there are no third-party runtime package dependencies. NuGet access is still needed for Microsoft Windows targeting/runtime packs.

Use the SDK pinned in `global.json` (10.0.403). A system-wide SDK installation is unnecessary; a workspace-local SDK works. Cross-compilation on macOS is enabled with `EnableWindowsTargeting=true`, as described by [Microsoft](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1100). Running WinForms still requires Windows or a compatibility layer.

macOS/Linux:

```sh
dotnet run --project tests/Diagnostics.Tests.csproj -c Release
./scripts/build.sh
# Optional workspace-local SDK:
# DOTNET=/absolute/path/to/dotnet ./scripts/build.sh
```

Windows PowerShell:

```powershell
dotnet run --project tests/Diagnostics.Tests.csproj -c Release
./scripts/build.ps1
```

The result is `artifacts/diagnostic-win-x64/MW2.FPS.Unlocker.exe` **plus every sibling file and subdirectory**. Keep the full publish directory together. It bundles the Windows x64 .NET runtime, including WinForms; no .NET installation inside the bottle is required by design. Whether that bundled runtime launches in this bottle remains an actual test. This is a folder deployment rather than a single-file bundle, to keep native loading and diagnostics straightforward. It is not trimmed or ReadyToRun compiled. Scripts pin the toolchain/settings; bit-for-bit repeatability across future runtime-pack feeds is not claimed.

`verify_readonly.py` checks the untrimmed application assembly for absent native write/protection/injection symbols and present read/query symbols. The diagnostic `OpenProcess` mask is `0x410` (`PROCESS_QUERY_INFORMATION | PROCESS_VM_READ`); it never requests write rights. The compile-time gate removes `WriteProcessMemory` from this assembly. The runtime guard also rejects writes. The symbol scan is a regression check, not a general-purpose security audit of the bundled .NET runtime.

## Run the first two stages in the existing Steam bottle

Do not copy anything into the game directory or replace `iw4mp.exe`. Unzip the diagnostic folder somewhere separate, for example a folder under Downloads. Close the old unlocker first. These steps are manual because running tests against the game requires your interaction. The initial implementation did not open a live game process; the later user-authorized read-only tests are recorded below and in the validation record.

1. Open CrossOver, select the existing **Steam** bottle, choose **Run Command**, and browse to the extracted `MW2.FPS.Unlocker.exe`. Do not create or change the bottle.
2. Append `--mode detect` to the command. The default with no arguments is also detect-only. Launch the utility before MW2. Confirm the GUI opens; then launch MW2 normally through Steam in the same bottle. Keep it running for several minutes, close MW2 normally, and launch it again at least three times with the utility left open. Detection only enumerates processes/checks exit state; it does not enumerate game modules or call our memory APIs. Both write toggles remain disabled.
3. Close the diagnostic utility. Launch it again using `--mode read-only`. Start MW2 and repeat the same sequence. This opens a query/read handle, validates module and disk/live PE architecture, hashes the executable, and reads candidate pointers/32-byte records. Candidate RVAs are explicitly unverified and never become write targets. The x86 case inspects PE headers only unless a separately researched exact profile is supplied.
4. Use **Show log path** in the utility. Logs normally live in `%LOCALAPPDATA%\MW2FPSUnlocker\logs` within the bottle. The actual Wine mapping can vary; use the displayed path. You can append `--log-dir "C:\mw2-diagnostic-logs"` to choose a known writable bottle directory. The directory is created by the utility. Logs are UTC ISO timestamps and flush at each event; each run has a separate file, with rotation at 10 MiB and one preceding segment.

Exact Windows command examples (entered in CrossOver's Run Command, with your real extracted path):

```text
"C:\path\MW2-diagnostics\MW2.FPS.Unlocker.exe" --mode detect --log-dir "C:\mw2-diagnostic-logs"
"C:\path\MW2-diagnostics\MW2.FPS.Unlocker.exe" --mode read-only --log-dir "C:\mw2-diagnostic-logs"
```

If the executable is on a macOS path, CrossOver's Browse control maps it into the bottle. Keep the arguments after the closing quote.

For an optional shell launch, first confirm this installed CrossOver provides its `wine` wrapper and accepts `--bottle` by running its `--help`. The manual Run Command path above does not depend on wrapper syntax.

## Evidence to return

Send the **detect** log and **read-only** log (and `.previous` segment if present), including startup through the first failure or clean exit. Also send:

- Exact CrossOver version, macOS version, bottle architecture, and whether each of three repeated launches stayed stable. CrossOver 26.3 was found installed on the development Mac; runtime detection/read-only testing was subsequently performed; memory writes were not tested.
- The old unlocker's file version and, if possible, its SHA-256. Read-only logs record the game SHA-256, pointer size, machine type, PE timestamp and image size automatically.
- For any crash: the complete Wine Debugger text including faulting instruction, registers, backtrace, module list, and the time of the crash. Use CrossOver's logging controls for the relevant Run Command if available; preserve the generated report.

Logs include local executable paths and memory addresses. Review these before posting publicly. No memory dump of the full game is made.

Interpretation:

| Observation | What it tells us |
| --- | --- |
| GUI cannot launch | Investigate WinForms/bundled .NET initialization separately; memory code has not started. |
| Detect-only stable; read-only fails before `END OpenProcess` | Investigate attachment/rights/API behavior with the last operation and Wine trace. |
| Open succeeds but query/read fails | Log contains address, page information, byte count and Win32 error; no write has occurred. |
| Both modes stable | Automatic writes become a stronger hypothesis; this still does not identify a particular original write. |
| `BEGIN` without matching `END` | Candidate operation for correlation with crash time; not proof of causality, especially if the game crashed asynchronously. |
| Candidate pointer is null or unreadable | Startup, stale RVA or layout mismatch is possible. The diagnostic does not guess another write address. |

If a required inspection operation fails, the session stops memory operations and waits for game exit. Module-discovery failures during startup retry at most ten times. Unknown/unsupported APIs fail gracefully; unavailable `IsWow64Process2` falls back to independently checked disk/live PE metadata. Multiple `iw4mp` processes are treated as ambiguous. Every new process gets a new handle and freshly resolved pointers. Closing the GUI cancels and awaits polling before disposing resources.

## Experimental FPS/FOV path — not part of the delivered binary

The source preserves the 75–500 FPS and 60–100 FOV controls and a 250 ms update loop behind `EnableMemoryWrites=true`. It does not ship a game profile, because no layout has yet been validated for live writes. A profile must identify an exact game SHA-256, pointer width, both dvar global RVAs, and each dvar's name-pointer/type/value offsets and expected type byte. Integers in JSON are decimal. See `Core/GameProfile.cs` for the schema; there are deliberately no copy-and-run speculative offsets.

For later maintainer validation only:

```sh
dotnet build mw2-fps-unlocker.csproj -c Release -p:EnableMemoryWrites=true
```

That opt-in build is separately labelled in its startup log and title (`Patch` when explicitly selected), and needs `--mode patch --profile FILE`. Hash/architecture mismatch prevents patching. Each enabled write checks the expected `com_maxfps`/`cg_fov` name, profile type, plausible current value, pointer alignment and stability, and a committed writable **non-executable** page. Transfers and readback are checked; failure stops that session. No protection changes, injection, bypasses or stealth features are present. A hash plus plausible fields cannot prove a profile correct, and a pointer can change after validation; these checks do not make multiplayer patching risk-free. Toggling off stops subsequent cycles; an already-started cycle may finish. Original values are not automatically restored on detach, because writing through a potentially stale pointer would add risk; restart the game to reset values.

**Do not run patch mode against the live game yet.** FPS-only, then FOV-only tests require independently verified offsets, explicit authorization for that stage, and an appropriate test environment. Both toggles begin unchecked and are disarmed on exit/relaunch or an inspection error. No claim of VAC safety is made for read-only tools either. [Valve's VAC guidance](https://help.steampowered.com/en/faqs/view/571A-97DA-70E9-FF74) does not endorse this utility; the upstream absence-of-reported-bans statement is not evidence of safety.

No supported configuration alternative for both targets on this exact current Steam executable has been established. Use in-game settings where available. This fork does not edit configs, executables, bottle settings or game files, and does not unlock protected dvars through configuration or bypasses.

## Validation record

See `TEST_RESULTS.md` for completed checks and the explicit pending game-test matrix. GitHub Actions runs the portable checks on macOS and Windows; the Windows run additionally reads an allocation owned by its own test process and confirms the diagnostic write guard leaves it unchanged. This fixture never selects a game process. Compilation of the experimental branch is checked but is not a game-write test.
