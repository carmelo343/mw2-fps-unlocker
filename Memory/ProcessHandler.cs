using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using mw2_fps_unlocker.Diagnostics;

namespace mw2_fps_unlocker.Memory;

internal static class ProcessHandler
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ModuleEntry
    {
        internal uint Size, ModuleId, ProcessId, GlobalUsage, ProcessUsage;
        internal nint BaseAddress;
        internal uint BaseSize;
        internal nint Module;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string Path;
    }
    private sealed class SnapshotHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SnapshotHandle() : base(true) { }
        protected override bool ReleaseHandle() => CloseHandle(handle);
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SnapshotHandle CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Module32FirstW(SnapshotHandle snapshot, ref ModuleEntry entry);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    internal sealed record ModuleInfo(string Path, ulong BaseAddress, uint Size);

    internal static Process? Find(DiagnosticLog log)
    {
        var processes = Process.GetProcessesByName("iw4mp");
        if (processes.Length == 1) return processes[0];
        foreach (var process in processes) process.Dispose();
        if (processes.Length > 1) log.Write("Multiple iw4mp processes; waiting rather than selecting an ambiguous target.");
        return null;
    }

    internal static ModuleInfo GetModule(int pid, DiagnosticLog log)
    {
        log.Write($"BEGIN CreateToolhelp32Snapshot pid={pid}");
        using var snapshot = CreateToolhelp32Snapshot(0x08 | 0x10, (uint)pid);
        int error = Marshal.GetLastWin32Error();
        if (snapshot.IsInvalid)
        {
            log.ApiFailure("CreateToolhelp32Snapshot", error);
            throw new Win32Exception(error);
        }
        var entry = new ModuleEntry { Size = (uint)Marshal.SizeOf<ModuleEntry>() };
        log.Write("BEGIN Module32FirstW");
        bool ok = Module32FirstW(snapshot, ref entry);
        error = Marshal.GetLastWin32Error();
        if (!ok)
        {
            log.ApiFailure("Module32FirstW", error);
            throw new Win32Exception(error);
        }
        if (!string.Equals(entry.Name, "iw4mp.exe", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(System.IO.Path.GetFileName(entry.Path), "iw4mp.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Main module is not iw4mp.exe.");
        log.Write($"END module path={entry.Path} base=0x{entry.BaseAddress:X} size=0x{entry.BaseSize:X}");
        return new(entry.Path, (ulong)entry.BaseAddress, entry.BaseSize);
    }
}
