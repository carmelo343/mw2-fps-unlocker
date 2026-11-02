using mw2_fps_unlocker.App;
using mw2_fps_unlocker.Core;
using mw2_fps_unlocker.Diagnostics;

namespace mw2_fps_unlocker;

public partial class AppForm : Form
{
    private readonly TrackBarWithoutFocus trackBarFPS = new();
    private readonly TrackBarWithoutFocus trackBarFOV = new();
    private readonly CheckBox enableFps = new() { Text = "Enable FPS writes", AutoSize = true, Location = new(12, 140) };
    private readonly CheckBox enableFov = new() { Text = "Enable FOV writes", AutoSize = true, Location = new(12, 168) };
    private readonly CancellationTokenSource cancellation = new();
    private Task? worker;
    private PatchRequest settings = new(Guid.Empty, false, false, 300, 80);
    private Guid sessionId;
    private bool shutdownComplete;
    private bool shutdownStarted;
    private readonly Options options;
    private readonly DiagnosticLog log;

    internal AppForm(Options options, DiagnosticLog log)
    {
        this.options = options;
        this.log = log;
        InitializeComponent();
        Text = "MW2 diagnostics — " + options.Mode;
        ClientSize = new(500, 280);
        label1.Location = new(390, 250);
        trackBarFPS.Location = new(50, 37);
        trackBarFPS.Size = new(151, 40);
        trackBarFPS.Minimum = 75;
        trackBarFPS.Maximum = 500;
        trackBarFPS.Value = 300;
        trackBarFPS.TickStyle = TickStyle.None;
        trackBarFOV.Location = new(50, 80);
        trackBarFOV.Size = new(151, 40);
        trackBarFOV.Minimum = 60;
        trackBarFOV.Maximum = 100;
        trackBarFOV.Value = 80;
        trackBarFOV.TickStyle = TickStyle.None;
        enableFps.Enabled = enableFov.Enabled = false;
        trackBarFPS.ValueChanged += (_, _) => UpdateSettings();
        trackBarFOV.ValueChanged += (_, _) => UpdateSettings();
        enableFps.CheckedChanged += (_, _) => UpdateSettings();
        enableFov.CheckedChanged += (_, _) => UpdateSettings();
        var modeLabel = new Label { Text = options.Mode == RunMode.Patch
            ? "Experimental writes require a matching profile and explicit toggles."
            : "Diagnostic mode: no game-memory writes.", AutoSize = true, Location = new(12, 208) };
        var logButton = new Button { Text = "Show log path", Location = new(12, 240), AutoSize = true };
        logButton.Click += (_, _) => MessageBox.Show(log.FilePath, "Diagnostic log");
        Controls.AddRange([trackBarFPS, trackBarFOV, enableFps, enableFov, modeLabel, logButton]);
        Shown += (_, _) =>
        {
            var progress = new Progress<SessionStatus>(UpdateStatus);
            worker = Task.Run(() => new GameSession(options, log, progress,
                () => Volatile.Read(ref settings)).RunAsync(cancellation.Token));
        };
        FormClosing += OnClosing;
    }

    private void UpdateStatus(SessionStatus status)
    {
        if (IsDisposed || cancellation.IsCancellationRequested) return;
        if (sessionId != status.Session || !status.CanPatch)
        {
            sessionId = status.Session;
            enableFps.Checked = enableFov.Checked = false;
            UpdateSettings();
        }
        enableFps.Enabled = enableFov.Enabled = status.CanPatch;
        LabelStatusValue.Text = status.Text;
        LabelStatusValue.ForeColor = status.Session == Guid.Empty ? Color.RoyalBlue : Color.DarkGreen;
    }

    private void UpdateSettings()
    {
        LabelFPSValue.Text = trackBarFPS.Value.ToString();
        LabelFOVValue.Text = trackBarFOV.Value.ToString();
        var next = new PatchRequest(sessionId, enableFps.Checked, enableFov.Checked, trackBarFPS.Value, trackBarFOV.Value);
        Volatile.Write(ref settings, next);
        log.Write($"CONTROLS session={sessionId} fpsEnabled={next.Fps} fovEnabled={next.Fov} fps={next.FpsValue} fov={next.FovValue}");
    }

    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (shutdownComplete) return;
        e.Cancel = true;
        if (shutdownStarted) return;
        shutdownStarted = true;
        Enabled = false;
        cancellation.Cancel();
        if (worker != null)
        {
            try { await worker; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { log.Exception("Worker shutdown", ex); }
        }
        cancellation.Dispose();
        shutdownComplete = true;
        Close();
    }
}
