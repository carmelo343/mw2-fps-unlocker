using System.ComponentModel;
using System.Diagnostics;

namespace mw2_fps_unlocker.Diagnostics;

internal sealed class DiagnosticLog : IDisposable
{
    private readonly object gate = new();
    private StreamWriter writer;
    internal string FilePath { get; }

    internal DiagnosticLog(string? directory = null)
    {
        directory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MW2FPSUnlocker", "logs");
        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, $"diagnostic-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
        writer = Open();
    }

    private StreamWriter Open() => new(new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

    internal void Write(string message)
    {
        lock (gate)
        {
            // Bound disk use during long unattended sessions; keep the preceding segment.
            if (writer.BaseStream.Length > 10 * 1024 * 1024)
            {
                writer.Dispose();
                File.Move(FilePath, FilePath + ".previous", true);
                writer = Open();
            }
            writer.WriteLine($"{DateTimeOffset.UtcNow:O} t={Environment.CurrentManagedThreadId} {message}");
            Debug.WriteLine(message);
        }
    }

    internal void ApiFailure(string operation, int error) =>
        Write($"FAIL {operation} win32={error} ({new Win32Exception(error).Message})");
    internal void Exception(string operation, Exception ex) => Write($"EXCEPTION {operation}: {ex}");
    public void Dispose() { lock (gate) writer.Dispose(); }
}
