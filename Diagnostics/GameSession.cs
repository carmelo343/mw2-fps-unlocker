using System.Diagnostics;
using System.Text;
using mw2_fps_unlocker.Core;
using mw2_fps_unlocker.Memory;

namespace mw2_fps_unlocker.Diagnostics;

internal sealed record PatchRequest(Guid Session, bool Fps, bool Fov, int FpsValue, int FovValue);
internal sealed record SessionStatus(Guid Session, string Text, bool CanPatch = false);

internal sealed class GameSession(Options options, DiagnosticLog log, IProgress<SessionStatus> status,
    Func<PatchRequest> request)
{
    private readonly OneShotBudget oneShot = new();
    private sealed record DvarSnapshot(ulong Address, byte[] Value);

    internal async Task RunAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                using Process? process = ProcessHandler.Find(log);
                if (process == null)
                {
                    status.Report(new(Guid.Empty, "Waiting for iw4mp.exe"));
                    await Task.Delay(2000, cancellation);
                    continue;
                }
                Guid id = Guid.NewGuid();
                log.Write($"DETECTED pid={process.Id}; mode={options.Mode}; no automatic writes");
                status.Report(new(id, "Detected — " + options.Mode));
                try
                {
                    if (options.Mode == RunMode.Detect)
                        await WaitForExit(process, cancellation);
                    else
                        await Inspect(process, id, cancellation);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Exception("Session stopped; no further memory operations until process exits", ex);
                    status.Report(new(id, "Inspection stopped — see log"));
                    await WaitForExit(process, cancellation);
                }
                log.Write($"DETACHED pid={process.Id}");
                status.Report(new(Guid.Empty, "Game exited; waiting"));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                log.Exception("Process polling", ex);
                status.Report(new(Guid.Empty, "Discovery error — see log"));
                await Task.Delay(2000, cancellation);
            }
        }
    }

    private static async Task WaitForExit(Process process, CancellationToken cancellation)
    {
        while (!process.HasExited) await Task.Delay(1000, cancellation);
    }

    private async Task Inspect(Process process, Guid id, CancellationToken cancellation)
    {
        // Retry only module discovery while the executable loader is starting.
        ProcessHandler.ModuleInfo? module = null;
        for (int attempt = 0; attempt < 10 && !process.HasExited; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            try { module = ProcessHandler.GetModule(process.Id, log); break; }
            catch (System.ComponentModel.Win32Exception ex)
            {
                log.Exception($"Module startup attempt={attempt + 1}/10", ex);
                await Task.Delay(1000, cancellation);
            }
        }
        if (module == null) throw new IOException("Main module unavailable after startup retries.");
        log.Write("BEGIN disk PE validation and SHA256 (read-only)");
        GameImage image = GameImage.Load(module.Path);
        log.Write($"IMAGE sha256={image.Sha256} machine=0x{image.Machine:X} pointerSize={image.PointerSize} size=0x{image.ImageSize:X} timestamp=0x{image.Timestamp:X}");
        if (module.Size != image.ImageSize) throw new InvalidDataException("Module and disk image sizes differ.");
        GameProfile? profile = options.ProfilePath == null ? null : GameProfile.Load(options.ProfilePath);
        profile?.Match(image);
        if (process.HasExited) return;
        using var memory = new MemoryHandler(process.Id, image.PointerSize, options.Mode is RunMode.Patch or RunMode.FpsOnce, log);
        memory.ValidateArchitecture(image.Machine);
        ValidateLiveImage(memory, module.BaseAddress, image);
        status.Report(new(id, "Validated — " + options.Mode, options.Mode == RunMode.Patch && profile != null));
        DvarSnapshot? previousFps = null, previousFov = null;
        var stable = new Stopwatch();
        while (!process.HasExited)
        {
            cancellation.ThrowIfCancellationRequested();
            if (profile == null)
            {
                // Historical upstream candidates, NOT trusted layout information.
                if (image.PointerSize == 8)
                {
                    ProbeCandidate(memory, module, 0x1D260A8, "com_maxfps");
                    ProbeCandidate(memory, module, 0x809C78, "cg_fov");
                }
                else log.Write("x86 image: no verified candidate RVAs; header inspection only.");
            }
            else
            {
                PatchRequest current = request();
                var fps = InspectDvar(memory, module.BaseAddress, profile.Fps, "com_maxfps", false,
                    current.Session == id && current.Fps, current.FpsValue);
                var fov = InspectDvar(memory, module.BaseAddress, profile.Fov, "cg_fov", true,
                    current.Session == id && current.Fov, current.FovValue);
                if (options.Mode == RunMode.FpsOnce && !oneShot.Attempted)
                {
                    bool unchanged = fps != null && fov != null && previousFps != null && previousFov != null &&
                        fps.Address == previousFps.Address && fov.Address == previousFov.Address &&
                        fps.Value.SequenceEqual(previousFps.Value) && fov.Value.SequenceEqual(previousFov.Value);
                    if (!unchanged) stable.Restart();
                    previousFps = fps;
                    previousFov = fov;
                    if (unchanged && stable.Elapsed >= TimeSpan.FromSeconds(10))
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (oneShot.TryConsume())
                        {
                            log.Write($"FPS_ONCE attempt=1 old={BitConverter.ToInt32(fps!.Value)} target={(options.FpsOnceUseCurrent ? "current (unchanged)" : options.FpsOnceTarget.ToString())} fovBefore={BitConverter.ToSingle(fov!.Value)}; no FOV write; no repeat on restart");
                            // Fresh name/type/pointer/value validation immediately before the one write.
                            var fresh = InspectDvar(memory, module.BaseAddress, profile.Fps, "com_maxfps", false, false, 0);
                            if (fresh == null || fresh.Address != fps.Address || !fresh.Value.SequenceEqual(fps.Value))
                                throw new InvalidDataException("FPS changed before the single-write attempt.");
                            int target = options.FpsOnceUseCurrent ? BitConverter.ToInt32(fresh.Value) : options.FpsOnceTarget!.Value;
                            log.Write($"FPS_ONCE resolvedTarget={target} baseline={BitConverter.ToInt32(fresh.Value)} sameValue={options.FpsOnceUseCurrent}");
                            memory.WriteValue(fresh.Address, BitConverter.GetBytes(target));
                            var fovAfter = InspectDvar(memory, module.BaseAddress, profile.Fov, "cg_fov", true, false, 0);
                            log.Write($"FPS_ONCE completed; fovAfter={(fovAfter == null ? "UNAVAILABLE" : BitConverter.ToSingle(fovAfter.Value).ToString())}; subsequent cycles read-only");
                            status.Report(new(id, "FPS single write completed — observing"));
                        }
                    }
                }
            }
            await Task.Delay(options.Mode == RunMode.Patch ? 250 : 2000, cancellation);
        }
    }

    private static byte[] Require(byte[]? bytes) => bytes ?? throw new IOException("Required memory read failed.");

    private void ValidateLiveImage(MemoryHandler memory, ulong baseAddress, GameImage image)
    {
        byte[] dos = Require(memory.Read(baseAddress, 64));
        if (dos[0] != 'M' || dos[1] != 'Z') throw new InvalidDataException("Invalid live DOS header.");
        int peOffset = BitConverter.ToInt32(dos, 0x3C);
        if (peOffset < 64 || peOffset > image.ImageSize - 88) throw new InvalidDataException("Invalid live PE offset.");
        byte[] pe = Require(memory.Read(MemoryPolicy.Add(baseAddress, (ulong)peOffset, image.PointerSize), 88));
        if (BitConverter.ToUInt32(pe) != 0x4550 || BitConverter.ToUInt16(pe, 4) != image.Machine ||
            BitConverter.ToUInt32(pe, 8) != image.Timestamp ||
            BitConverter.ToUInt16(pe, 24) != (image.PointerSize == 8 ? 0x20B : 0x10B) ||
            BitConverter.ToInt32(pe, 24 + 56) != image.ImageSize)
            throw new InvalidDataException("Live PE identity differs from disk image.");
        log.Write("VALIDATED live PE signature, architecture, timestamp and SizeOfImage");
    }

    private void ProbeCandidate(MemoryHandler memory, ProcessHandler.ModuleInfo module, ulong rva, string name)
    {
        if (rva > module.Size || (ulong)memory.PointerSize > module.Size - rva)
        {
            log.Write($"CANDIDATE {name} rva=0x{rva:X} outside image; skipped");
            return;
        }
        ulong address = MemoryPolicy.Add(module.BaseAddress, rva, memory.PointerSize);
        ulong? pointer = memory.ReadPointer(address);
        log.Write($"CANDIDATE {name} rva=0x{rva:X} pointer={(pointer.HasValue ? $"0x{pointer:X}" : "READ_FAILED")} UNVERIFIED");
        if (!pointer.HasValue || !MemoryPolicy.ValidAddress(pointer.Value, 32, memory.PointerSize)) return;
        byte[]? record = memory.Read(pointer.Value, 32);
        log.Write($"CANDIDATE {name} first32={(record == null ? "READ_FAILED" : Convert.ToHexString(record))}; raw data only, layout unverified");
    }

    private DvarSnapshot? InspectDvar(MemoryHandler memory, ulong moduleBase, DvarSpec spec, string name,
        bool isFloat, bool enabled, int desired)
    {
        ulong? pointer = memory.ReadPointer(MemoryPolicy.Add(moduleBase, spec.Rva, memory.PointerSize));
        if (pointer == null) throw new IOException($"{name} global read failed.");
        if (pointer == 0) { log.Write($"{name} not registered yet; skipped"); return null; }
        ulong dvar = pointer.Value;
        if (dvar % (ulong)memory.PointerSize != 0) throw new InvalidDataException("Unaligned dvar pointer.");
        ulong? namePointer = memory.ReadPointer(MemoryPolicy.Add(dvar, (ulong)spec.NamePointerOffset, memory.PointerSize));
        if (namePointer == null) throw new IOException("Dvar name pointer read failed.");
        byte[] actualName = Require(memory.Read(namePointer.Value, name.Length + 1));
        if (!actualName.SequenceEqual(Encoding.ASCII.GetBytes(name + '\0')))
            throw new InvalidDataException($"{name} name validation failed.");
        byte[] type = Require(memory.Read(MemoryPolicy.Add(dvar, (ulong)spec.TypeOffset, memory.PointerSize), 1));
        if (type[0] != spec.Type) throw new InvalidDataException($"{name} type validation failed.");
        ulong valueAddress = MemoryPolicy.Add(dvar, (ulong)spec.ValueOffset, memory.PointerSize);
        byte[] value = Require(memory.Read(valueAddress, 4));
        double number = isFloat ? BitConverter.ToSingle(value) : BitConverter.ToInt32(value);
        if (!double.IsFinite(number) || number < 1 || number > (isFloat ? 180 : 1000))
            throw new InvalidDataException($"{name} implausible current value={number}.");
        log.Write($"DVAR {name} address=0x{valueAddress:X} current={number} enabled={enabled}");
        var snapshot = new DvarSnapshot(valueAddress, value);
        if (!enabled || options.Mode != RunMode.Patch) return snapshot;
        if (desired < (isFloat ? 60 : 75) || desired > (isFloat ? 100 : 500))
            throw new InvalidDataException("Requested slider value is out of range.");
        // Resolve and validate each cycle; never cache pointers across launches.
        if (memory.ReadPointer(MemoryPolicy.Add(moduleBase, spec.Rva, memory.PointerSize)) != dvar)
            throw new InvalidDataException("Dvar pointer changed during validation.");
        memory.WriteValue(valueAddress, isFloat ? BitConverter.GetBytes((float)desired) : BitConverter.GetBytes(desired));
        return snapshot;
    }
}
