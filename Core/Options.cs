namespace mw2_fps_unlocker.Core;

internal enum RunMode { Detect, ReadOnly, Patch, FpsOnce }

internal sealed record Options(RunMode Mode, string? ProfilePath, string? LogDirectory, int? FpsOnceTarget = null, bool FpsOnceUseCurrent = false, int FovTarget = 80, bool AutoFps = false, bool AutoFov = false, int? DurationSeconds = null)
{
    internal static Options Parse(string[] args)
    {
#if CROSSOVER_RELEASE
        if (args.Length == 0)
            return new(RunMode.Patch, Path.Combine(AppContext.BaseDirectory, "mw2-profile.json"), null, 120, false, 80, true, true);
#endif
        var mode = RunMode.Detect;
        string? profile = null, logs = null;
        int? fps = null;
        bool useCurrent = false;
        int fov = 80;
        bool fovProvided = false, autoFps = false, autoFov = false;
        int? duration = null;
        for (int i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new ArgumentException($"Missing value for {args[i - 1]}");
            switch (args[i])
            {
                case "--mode":
                    mode = Value() switch
                    {
                        "detect" => RunMode.Detect,
                        "read-only" => RunMode.ReadOnly,
                        "patch" => RunMode.Patch,
                        "fps-once" => RunMode.FpsOnce,
                        _ => throw new ArgumentException("Mode must be detect, read-only, patch, or fps-once.")
                    };
                    break;
                case "--fps":
                    string targetText = Value();
                    useCurrent = targetText == "current";
                    if (useCurrent) fps = null;
                    else
                    {
                        if (!int.TryParse(targetText, out int target) || target is < 75 or > 500)
                            throw new ArgumentException("FPS target must be 'current' or an integer between 75 and 500.");
                        fps = target;
                    }
                    break;
                case "--fov":
                    if (!int.TryParse(Value(), out fov) || fov is < 60 or > 100)
                        throw new ArgumentException("FOV target must be between 60 and 100.");
                    fovProvided = true;
                    break;
                case "--enable-fps": autoFps = true; break;
                case "--enable-fov": autoFov = true; break;
                case "--duration":
                    if (!int.TryParse(Value(), out int seconds) || seconds is < 5 or > 3600)
                        throw new ArgumentException("Test duration must be 5 to 3600 seconds.");
                    duration = seconds;
                    break;
                case "--profile": profile = Value(); break;
                case "--log-dir": logs = Value(); break;
                default: throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }
#if !ENABLE_MEMORY_WRITES
        if (mode is RunMode.Patch or RunMode.FpsOnce)
            throw new ArgumentException("This diagnostic binary has no memory-write implementation. Use detect or read-only.");
#endif
        if (mode is (RunMode.Patch or RunMode.FpsOnce) && profile == null)
            throw new ArgumentException("Patch mode requires an independently verified --profile file.");
        if (mode == RunMode.FpsOnce && fps == null && !useCurrent)
            throw new ArgumentException("fps-once requires an explicit --fps target.");
        if (mode != RunMode.FpsOnce && mode != RunMode.Patch && (fps != null || useCurrent))
            throw new ArgumentException("--fps requires patch or fps-once mode.");
        if (mode != RunMode.Patch && (fovProvided || autoFps || autoFov || duration != null))
            throw new ArgumentException("FOV, auto-enable and duration arguments require patch mode.");
        if (mode == RunMode.Patch && useCurrent)
            throw new ArgumentException("Patch mode needs a numeric FPS target.");
        return new(mode, profile, logs, fps, useCurrent, fov, autoFps, autoFov, duration);
    }
}
