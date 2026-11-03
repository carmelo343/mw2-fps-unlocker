using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text.Json;
using mw2_fps_unlocker.Diagnostics;
using mw2_fps_unlocker.Memory;

internal sealed record Ready(int Pid, string Nonce, ulong Rw, ulong CopyOnWrite, ulong ReadOnly);
internal sealed record Observed(string Nonce, byte[] Rw, byte[] CopyOnWrite);

internal static class Program
{
    private const int ValueOffset = 16;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQuery(nint address, out MemoryHandler.MemoryBasicInformation info, nuint size);

    private static int Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--fixture-child") return Child(args[1], args[2]);
        if (args.Length != 2 || args[0] != "--log-dir") return 2;
        Directory.CreateDirectory(args[1]);
        using var log = new DiagnosticLog(args[1]);
        string directory = Path.Combine(Path.GetTempPath(), "MW2-Disposable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string nonce = Guid.NewGuid().ToString("N");
        Process? child = null;
        int result = 1;
        try
        {
            log.Write("START disposable cross-process write probe; no process enumeration or caller-supplied PID/address");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("--fixture-child");
            start.ArgumentList.Add(directory);
            start.ArgumentList.Add(nonce);
            child = Process.Start(start) ?? throw new IOException("Could not start owned fixture child.");
            log.Write($"OWNED_CHILD pid={child.Id}");
            WaitForFile(Path.Combine(directory, "ready.json"), child, 20000);
            Ready ready = JsonSerializer.Deserialize<Ready>(File.ReadAllText(Path.Combine(directory, "ready.json")))!;
            if (ready.Pid != child.Id || ready.Nonce != nonce || child.HasExited)
                throw new InvalidDataException("Owned child identity mismatch.");
            using var memory = new MemoryHandler(child.Id, 8, true, log);
            byte[] rwBefore = memory.Read(ready.Rw, 32) ?? throw new IOException("RW baseline read failed.");
            byte[] cowBefore = memory.Read(ready.CopyOnWrite, 32) ?? throw new IOException("COW baseline read failed.");
            Check(BitConverter.ToInt32(rwBefore, ValueOffset) == 85, "RW baseline=85", log);
            Check(BitConverter.ToSingle(cowBefore, ValueOffset) == 65, "COW baseline=65", log);
            int observation = 0;
            void Verify(byte[] expectedRw, byte[] expectedCow)
            {
                int number = ++observation;
                File.WriteAllText(Path.Combine(directory, $"observe-{number}"), nonce);
                string answer = Path.Combine(directory, $"observed-{number}.json");
                WaitForFile(answer, child, 10000);
                Observed observed = JsonSerializer.Deserialize<Observed>(File.ReadAllText(answer))!;
                Check(observed.Nonce == nonce && observed.Rw.SequenceEqual(expectedRw) && observed.CopyOnWrite.SequenceEqual(expectedCow),
                    $"child observation {number}: target value and neighboring bytes correct", log);
            }
            byte[] rwChanged = (byte[])rwBefore.Clone();
            BitConverter.GetBytes(120).CopyTo(rwChanged, ValueOffset);
            memory.WriteValue(ready.Rw + ValueOffset, BitConverter.GetBytes(120));
            Verify(rwChanged, cowBefore);
            memory.WriteValue(ready.Rw + ValueOffset, BitConverter.GetBytes(85));
            Verify(rwBefore, cowBefore);
            byte[] cowChanged = (byte[])cowBefore.Clone();
            BitConverter.GetBytes(70f).CopyTo(cowChanged, ValueOffset);
            memory.WriteValue(ready.CopyOnWrite + ValueOffset, BitConverter.GetBytes(70f));
            Verify(rwBefore, cowChanged);
            Check(BitConverter.ToSingle(ReadSharedFile(Path.Combine(directory, "backing.bin")), ValueOffset) == 65,
                "COW backing file unchanged", log);
            Check(BitConverter.ToSingle(memory.Read(ready.ReadOnly + ValueOffset, 4)!, 0) == 65,
                "separate read-only view unchanged", log);
            memory.WriteValue(ready.CopyOnWrite + ValueOffset, BitConverter.GetBytes(65f));
            Verify(rwBefore, cowBefore);
            bool rejected = false;
            try { memory.WriteValue(ready.ReadOnly + ValueOffset, BitConverter.GetBytes(70f)); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "read-only page write rejected before native write call", log);
            using var readonlyHandle = new MemoryHandler(child.Id, 8, false, log);
            rejected = false;
            try { readonlyHandle.WriteValue(ready.Rw + ValueOffset, BitConverter.GetBytes(999)); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "runtime read-only guard rejects writes even in write-capable fixture", log);
            result = 0;
        }
        catch (Exception ex) { log.Exception("Disposable write probe", ex); }
        finally
        {
            File.WriteAllText(Path.Combine(directory, "exit"), nonce);
            if (child != null)
            {
                if (!child.WaitForExit(5000)) { log.Write("Owned child exceeded exit timeout; terminating only that child."); child.Kill(true); child.WaitForExit(5000); }
                if (child.ExitCode != 0) { log.Write($"Owned child failed exit={child.ExitCode}"); result = 1; }
                child.Dispose();
            }
            if (File.Exists(Path.Combine(directory, "child-error.txt"))) log.Write("CHILD_ERROR " + File.ReadAllText(Path.Combine(directory, "child-error.txt")));
            try { Directory.Delete(directory, true); }
            catch (Exception ex) { log.Exception("Disposable directory cleanup", ex); result = 1; }
            log.Write($"RESULT {(result == 0 ? "PASS" : "FAIL")} disposable-only probe; game process never opened");
        }
        return result;
    }

    private static void Check(bool condition, string message, DiagnosticLog log)
    {
        if (!condition) throw new InvalidDataException(message);
        log.Write("PASS " + message);
    }

    private static void WaitForFile(string path, Process child, int timeout)
    {
        var watch = Stopwatch.StartNew();
        while (!File.Exists(path))
        {
            if (child.HasExited || watch.ElapsedMilliseconds > timeout) throw new TimeoutException("Owned fixture did not respond: " + Path.GetFileName(path));
            Thread.Sleep(25);
        }
    }

    private static byte[] ReadSharedFile(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var buffer = new MemoryStream();
        file.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void Publish(string path, object value)
    {
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value, Json));
        File.Move(path + ".tmp", path);
    }

    private static int Child(string directory, string nonce)
    {
        if (nonce.Length != 32 || !Path.GetFileName(directory).StartsWith("MW2-Disposable-", StringComparison.Ordinal)) return 2;
        nint rw = Marshal.AllocHGlobal(32);
        try
        {
            byte[] rwBytes = Enumerable.Repeat((byte)0xA5, 32).ToArray();
            BitConverter.GetBytes(85).CopyTo(rwBytes, ValueOffset);
            Marshal.Copy(rwBytes, 0, rw, rwBytes.Length);
            byte[] fileBytes = Enumerable.Repeat((byte)0x5A, 4096).ToArray();
            BitConverter.GetBytes(65f).CopyTo(fileBytes, ValueOffset);
            string backing = Path.Combine(directory, "backing.bin");
            File.WriteAllBytes(backing, fileBytes);
            using var backingStream = new FileStream(backing, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var mapping = MemoryMappedFile.CreateFromFile(backingStream, null, 4096, MemoryMappedFileAccess.CopyOnWrite, HandleInheritability.None, true);
            using var cow = mapping.CreateViewAccessor(0, 4096, MemoryMappedFileAccess.CopyOnWrite);
            using var readStream = new FileStream(backing, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var readMapping = MemoryMappedFile.CreateFromFile(readStream, null, 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var read = readMapping.CreateViewAccessor(0, 4096, MemoryMappedFileAccess.Read);
            nint cowAddress = cow.SafeMemoryMappedViewHandle.DangerousGetHandle();
            nint readAddress = read.SafeMemoryMappedViewHandle.DangerousGetHandle();
            // Check the actual OS page type before touching the COW view in the child.
            if (VirtualQuery(cowAddress, out var cowInfo, (nuint)Marshal.SizeOf<MemoryHandler.MemoryBasicInformation>()) == 0 ||
                (cowInfo.Protect & 0xFF) != 0x08) throw new InvalidDataException("Fixture did not receive PAGE_WRITECOPY.");
            if (VirtualQuery(readAddress, out var readInfo, (nuint)Marshal.SizeOf<MemoryHandler.MemoryBasicInformation>()) == 0 ||
                (readInfo.Protect & 0xFF) != 0x02) throw new InvalidDataException("Fixture did not receive PAGE_READONLY.");
            Publish(Path.Combine(directory, "ready.json"), new Ready(Environment.ProcessId, nonce, (ulong)rw, (ulong)cowAddress, (ulong)readAddress));
            var lifetime = Stopwatch.StartNew();
            int number = 1;
            while (lifetime.Elapsed < TimeSpan.FromSeconds(60))
            {
                if (File.Exists(Path.Combine(directory, "exit"))) return 0;
                if (File.Exists(Path.Combine(directory, $"observe-{number}")))
                {
                    byte[] currentRw = new byte[32], currentCow = new byte[32];
                    Marshal.Copy(rw, currentRw, 0, 32);
                    Marshal.Copy(cowAddress, currentCow, 0, 32);
                    Publish(Path.Combine(directory, $"observed-{number}.json"), new Observed(nonce, currentRw, currentCow));
                    number++;
                }
                Thread.Sleep(25);
            }
            return 3;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "child-error.txt"), ex.ToString()); return 1; }
        finally { Marshal.FreeHGlobal(rw); }
    }
}
