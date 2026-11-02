using System.Runtime.InteropServices;
using mw2_fps_unlocker.Core;
using mw2_fps_unlocker.Diagnostics;

namespace mw2_fps_unlocker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        DiagnosticLog? log = null;
        try
        {
            Options options = Options.Parse(args);
            log = new DiagnosticLog(options.LogDirectory);
            log.Write($"START mode={options.Mode} runtime={RuntimeInformation.FrameworkDescription} processArch={RuntimeInformation.ProcessArchitecture} os={RuntimeInformation.OSDescription} pointerSize={IntPtr.Size}");
#if ENABLE_MEMORY_WRITES
            log.Write("BUILD experimental write support compiled in; both toggles start OFF");
#else
            log.Write("BUILD diagnostic-only; native write API absent");
#endif
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.Run(new AppForm(options, log));
            log.Write("STOP clean shutdown");
        }
        catch (Exception ex)
        {
            log?.Exception("Fatal", ex);
            MessageBox.Show($"{ex.Message}\n\nLog: {log?.FilePath ?? "not created"}\n\nArguments: --mode detect|read-only [--log-dir DIRECTORY]\nExperimental builds only: --mode patch --profile FILE",
                "MW2 diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { log?.Dispose(); }
    }
}
