using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using mw2_fps_unlocker.Core;
using mw2_fps_unlocker.Diagnostics;

namespace mw2_fps_unlocker.Memory;

internal sealed class MemoryHandler : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        internal nint BaseAddress, AllocationBase;
        internal uint AllocationProtect;
        internal ushort PartitionId;
        internal nuint RegionSize;
        internal uint State, Protect, Type;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(SafeProcessHandle process, nint address,
        [Out] byte[] buffer, nuint size, out nuint read);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQueryEx(SafeProcessHandle process, nint address,
        out MemoryBasicInformation info, nuint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process2(SafeProcessHandle process, out ushort machine, out ushort native);
#if ENABLE_MEMORY_WRITES
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(SafeProcessHandle process, nint address,
        byte[] buffer, nuint size, out nuint written);
#endif

    private readonly SafeProcessHandle handle;
    private readonly DiagnosticLog log;
    private readonly bool writable;
    internal int PointerSize { get; }

    internal MemoryHandler(int pid, int pointerSize, bool requestWrites, DiagnosticLog log)
    {
        if (IntPtr.Size != 8) throw new PlatformNotSupportedException("The inspector must run as x64.");
        this.log = log;
        PointerSize = pointerSize;
#if ENABLE_MEMORY_WRITES
        writable = requestWrites;
#else
        if (requestWrites) throw new InvalidOperationException("Writes are absent from this diagnostic build.");
        writable = false;
#endif
        uint access = 0x0400 | 0x0010; // QUERY_INFORMATION | VM_READ
        if (writable) access |= 0x0020 | 0x0008; // VM_WRITE | VM_OPERATION, experimental only
        log.Write($"BEGIN OpenProcess pid={pid} access=0x{access:X}");
        handle = OpenProcess(access, false, pid);
        int error = Marshal.GetLastWin32Error();
        if (handle.IsInvalid)
        {
            handle.Dispose();
            log.ApiFailure("OpenProcess", error);
            throw new System.ComponentModel.Win32Exception(error);
        }
        log.Write("END OpenProcess success");
    }

    internal void ValidateArchitecture(ushort expectedMachine)
    {
        try
        {
            log.Write("BEGIN IsWow64Process2");
            bool ok = IsWow64Process2(handle, out ushort process, out ushort native);
            int error = Marshal.GetLastWin32Error();
            if (!ok)
            {
                log.ApiFailure("IsWow64Process2 (using validated disk + live PE headers)", error);
                return;
            }
            ushort actual = process == 0 ? native : process;
            log.Write($"END IsWow64Process2 process=0x{process:X} native=0x{native:X}");
            if (actual != expectedMachine) throw new InvalidDataException("Process architecture differs from executable PE header.");
        }
        catch (EntryPointNotFoundException ex)
        {
            log.Exception("IsWow64Process2 unavailable; using disk + live PE validation", ex);
        }
    }

    private bool ValidateRange(ulong address, int length, bool write)
    {
        if (!MemoryPolicy.ValidAddress(address, (ulong)Math.Max(length, 0), PointerSize))
        {
            log.Write($"REJECT address=0x{address:X} length={length} pointerSize={PointerSize}");
            return false;
        }
        log.Write($"BEGIN VirtualQueryEx address=0x{address:X} length={length} write={write}");
        nuint result = VirtualQueryEx(handle, (nint)address, out var info, (nuint)Marshal.SizeOf<MemoryBasicInformation>());
        int error = Marshal.GetLastWin32Error();
        if (result != (nuint)Marshal.SizeOf<MemoryBasicInformation>())
        {
            log.ApiFailure($"VirtualQueryEx returned={result}", error);
            return false;
        }
        bool valid = MemoryPolicy.RegionAllows(address, (ulong)length, (ulong)info.BaseAddress,
            (ulong)info.RegionSize, info.State, info.Protect, write);
        log.Write($"END VirtualQueryEx base=0x{info.BaseAddress:X} size=0x{info.RegionSize:X} state=0x{info.State:X} protect=0x{info.Protect:X} allowed={valid}");
        return valid;
    }

    internal byte[]? Read(ulong address, int length)
    {
        if (length is <= 0 or > 4096 || !ValidateRange(address, length, false)) return null;
        var bytes = new byte[length];
        log.Write($"BEGIN ReadProcessMemory address=0x{address:X} length={length}");
        bool ok = ReadProcessMemory(handle, (nint)address, bytes, (nuint)length, out nuint read);
        int error = Marshal.GetLastWin32Error();
        if (!ok || read != (nuint)length)
        {
            log.ApiFailure($"ReadProcessMemory address=0x{address:X} transferred={read}/{length} success={ok}", error);
            return null;
        }
        log.Write($"END ReadProcessMemory success transferred={read}");
        return bytes;
    }

    internal ulong? ReadPointer(ulong address)
    {
        var bytes = Read(address, PointerSize);
        return bytes == null ? null : MemoryPolicy.DecodePointer(bytes, PointerSize);
    }

    internal void WriteValue(ulong address, byte[] bytes)
    {
        // Runtime guard complements compile-time removal of the native import.
        if (!writable) throw new InvalidOperationException("Memory writes are disabled.");
#if ENABLE_MEMORY_WRITES
        if (bytes.Length != 4 || !ValidateRange(address, bytes.Length, true))
            throw new InvalidDataException("Write target is not a committed non-executable writable region.");
        log.Write($"BEGIN WriteProcessMemory address=0x{address:X} bytes={Convert.ToHexString(bytes)}");
        bool ok = WriteProcessMemory(handle, (nint)address, bytes, (nuint)bytes.Length, out nuint written);
        int error = Marshal.GetLastWin32Error();
        if (!ok || written != (nuint)bytes.Length)
        {
            log.ApiFailure($"WriteProcessMemory transferred={written}/{bytes.Length} success={ok}", error);
            throw new System.ComponentModel.Win32Exception(error, "Write failed or was partial; patching stopped.");
        }
        log.Write($"END WriteProcessMemory success transferred={written}");
        var actual = Read(address, bytes.Length);
        if (actual == null || !actual.SequenceEqual(bytes)) throw new IOException("Write readback mismatch; patching stopped.");
#else
        throw new InvalidOperationException("No memory-write implementation in this binary.");
#endif
    }

    public void Dispose() => handle.Dispose();
}
