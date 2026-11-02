using System.Runtime.InteropServices;
using mw2_fps_unlocker.Core;
using mw2_fps_unlocker.Memory;
using mw2_fps_unlocker.Diagnostics;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    checks++;
    Console.WriteLine("PASS " + name);
}
void Reject(Action action, string name)
{
    try { action(); }
    catch (Exception ex) when (ex is ArgumentException or InvalidDataException or OverflowException or BadImageFormatException)
    { Check(true, name); return; }
    throw new Exception("FAILED: expected rejection: " + name);
}

Check(Options.Parse([]).Mode == RunMode.Detect, "default mode only detects");
Check(Options.Parse(["--mode", "read-only"]).Mode == RunMode.ReadOnly, "read-only mode parsed");
Reject(() => Options.Parse(["--mode", "patch", "--profile", "unused"]), "diagnostic build rejects patch mode");
Reject(() => Options.Parse(["--mode"]), "missing mode rejected");
Reject(() => Options.Parse(["--enable-fps"]), "unknown flags rejected");
Check(!MemoryPolicy.ValidAddress(0, 4, 8), "null pointer rejected");
Check(!MemoryPolicy.ValidAddress(0xFFFF, 4, 8), "low pointer rejected");
Check(!MemoryPolicy.ValidAddress(0xFFFFFFFF, 8, 4), "x86 range overflow rejected");
Check(MemoryPolicy.ValidAddress(0xF1234567, 4, 4), "unsigned x86 pointer retained");
Check(!MemoryPolicy.ValidAddress(0x800000000000, 4, 8), "non-user x64 pointer rejected");
Reject(() => MemoryPolicy.Add(ulong.MaxValue, 16, 8), "pointer addition overflow rejected");
Check(MemoryPolicy.DecodePointer(BitConverter.GetBytes(0xF1234567u), 4) == 0xF1234567, "x86 pointer decoded without sign extension");
Check(MemoryPolicy.DecodePointer(BitConverter.GetBytes(0x123456789ABCul), 8) == 0x123456789ABC, "x64 pointer not truncated");
Reject(() => MemoryPolicy.DecodePointer(new byte[4], 8), "pointer width mismatch rejected");
Check(MemoryPolicy.RegionAllows(0x10000, 4, 0x10000, 4096, 0x1000, 0x04, true), "committed RW data writable");
Check(!MemoryPolicy.RegionAllows(0x10000, 4, 0x10000, 4096, 0x1000, 0x40, true), "executable page never patched");
Check(!MemoryPolicy.RegionAllows(0x10000, 4, 0x10000, 4096, 0x1000, 0x104, false), "guard page never read");
Check(!MemoryPolicy.RegionAllows(0x10FFE, 4, 0x10000, 4096, 0x1000, 0x04, false), "cross-region read rejected");
Check(!MemoryPolicy.RegionAllows(0x10000, 4, 0x10000, 4096, 0x2000, 0x04, false), "uncommitted memory rejected");
Check(!MemoryPolicy.RegionAllows(0x10000, 4, 0x10000, 4096, 0x1000, 0x01, false), "no-access memory rejected");
Check(Marshal.SizeOf<MemoryHandler.MemoryBasicInformation>() == 48, "x64 MEMORY_BASIC_INFORMATION ABI size");
var image = GameImage.Load(typeof(Options).Assembly.Location);
Check(image.Sha256.Length == 64 && image.ImageSize > 0, "real PE identity parsed");
var profile = new GameProfile(image.Sha256, image.PointerSize, new(128, 0, 8, 5, 16), new(256, 0, 8, 1, 16));
profile.Match(image);
Check(true, "exact hash and architecture profile accepted");
Reject(() => (profile with { Sha256 = new string('0', 64) }).Match(image), "wrong executable hash rejected");
Reject(() => (profile with { PointerSize = image.PointerSize == 4 ? 8 : 4 }).Match(image), "wrong target architecture rejected");
Reject(() => (profile with { Fps = profile.Fps with { Rva = (ulong)image.ImageSize } }).Match(image), "out-of-module RVA rejected");
Reject(() => (profile with { Fps = profile.Fps with { ValueOffset = 4 } }).Validate(), "overlapping dvar layout rejected");
Reject(() => (profile with { Fov = profile.Fps }).Validate(), "duplicate global rejected");
string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
Directory.CreateDirectory(temp);
try
{
    string invalid = Path.Combine(temp, "invalid.exe");
    File.WriteAllBytes(invalid, new byte[64]);
    Reject(() => GameImage.Load(invalid), "invalid PE rejected");
    using var log = new DiagnosticLog(temp);
    log.Write("portable fixture");
    Check(File.ReadAllText(log.FilePath).Contains("portable fixture"), "diagnostic log flushed immediately");
    if (OperatingSystem.IsWindows())
    {
        nint block = Marshal.AllocHGlobal(32);
        try
        {
            Marshal.WriteInt32(block, 123456);
            using var memory = new MemoryHandler(Environment.ProcessId, 8, false, log);
            byte[]? read = memory.Read((ulong)block, 4);
            Check(read != null && BitConverter.ToInt32(read) == 123456, "native read of owned fixture memory");
            Check(memory.Read(0, 4) == null, "native wrapper rejects null address");
            bool blocked = false;
            try { memory.WriteValue((ulong)block, BitConverter.GetBytes(654321)); }
            catch (InvalidOperationException) { blocked = true; }
            Check(blocked && Marshal.ReadInt32(block) == 123456, "diagnostic write guard preserves fixture memory");
        }
        finally { Marshal.FreeHGlobal(block); }
    }
    else Console.WriteLine("SKIP Windows native API fixture (requires Windows or Wine; no game process used)");
}
finally { Directory.Delete(temp, true); }
Console.WriteLine($"{checks} checks passed");
