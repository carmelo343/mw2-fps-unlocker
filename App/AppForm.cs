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
    private System.Windows.Forms.Timer? testTimer;
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
        Text = "MW2 " + (options.Mode is RunMode.FpsOnce or RunMode.Patch ? "EXPERIMENTAL — " : "diagnostics — ") + options.Mode;
        ClientSize = new(500, 280);
        label1.Location = new(390, 250);
        trackBarFPS.Location = new(50, 37);
        trackBarFPS.Size = new(151, 40);
        trackBarFPS.Minimum = 75;
        trackBarFPS.Maximum = 500;
        trackBarFPS.Value = options.FpsOnceTarget ?? 120;
        trackBarFPS.TickStyle = TickStyle.None;
        trackBarFOV.Location = new(50, 80);
        trackBarFOV.Size = new(151, 40);
        trackBarFOV.Minimum = 60;
        trackBarFOV.Maximum = 100;
        trackBarFOV.Value = options.FovTarget;
        trackBarFOV.TickStyle = TickStyle.None;
        enableFps.Enabled = enableFov.Enabled = false;
        trackBarFPS.ValueChanged += (_, _) => UpdateSettings();
        trackBarFOV.ValueChanged += (_, _) => UpdateSettings();
        enableFps.CheckedChanged += (_, _) => UpdateSettings();
        enableFov.CheckedChanged += (_, _) => UpdateSettings();
        var modeLabel = new Label { Text = options.Mode == RunMode.FpsOnce
            ? $"ONE FPS write to {(options.FpsOnceUseCurrent ? "current value" : options.FpsOnceTarget.ToString())}; FOV stays read-only."
            : options.Mode == RunMode.Patch
            ? "Experimental writes require a matching profile and explicit toggles."
            : "Diagnostic mode: no game-memory writes.", AutoSize = true, Location = new(12, 208) };
        var logButton = new Button { Text = "Show log path", Location = new(12, 240), AutoSize = true };
        logButton.Click += (_, _) => MessageBox.Show(log.FilePath, "Diagnostic log");
        var licenseButton = new Button { Text = "License", Location = new(150, 240), AutoSize = true };
        licenseButton.Click += (_, _) =>
        {
            using var stream = typeof(AppForm).Assembly.GetManifestResourceStream("mw2_fps_unlocker.LICENSE")!;
            using var reader = new StreamReader(stream);
            MessageBox.Show(reader.ReadToEnd(), "MIT License");
        };
        Controls.AddRange([trackBarFPS, trackBarFOV, enableFps, enableFov, modeLabel, logButton, licenseButton]);
#if CROSSOVER_RELEASE
        Text = "MW2 FPS / FOV — CrossOver";
#endif
        UpdateSettings();
        Shown += (_, _) =>
        {
            if (options.DurationSeconds is int seconds)
            {
                testTimer = new System.Windows.Forms.Timer { Interval = seconds * 1000 };
                testTimer.Tick += (_, _) => { testTimer.Stop(); log.Write("TEST_DURATION elapsed; closing utility"); Close(); };
                testTimer.Start();
            }
            var progress = new Progress<SessionStatus>(UpdateStatus);
            worker = Task.Run(() => new GameSession(options, log, progress,
                () => Volatile.Read(ref settings)).RunAsync(cancellation.Token));
        };
        FormClosing += OnClosing;
    }

    private void UpdateStatus(SessionStatus status)
    {
        if (IsDisposed || cancellation.IsCancellationRequested) return;
        bool newlyReady = status.CanPatch && (sessionId != status.Session || !enableFps.Enabled);
        if (sessionId != status.Session || !status.CanPatch)
        {
            sessionId = status.Session;
            enableFps.Checked = enableFov.Checked = false;
            UpdateSettings();
        }
        enableFps.Enabled = enableFov.Enabled = status.CanPatch;
        if (newlyReady)
        {
            enableFps.Checked = options.AutoFps;
            enableFov.Checked = options.AutoFov;
            UpdateSettings();
        }
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
        testTimer?.Stop();
        testTimer?.Dispose();
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
