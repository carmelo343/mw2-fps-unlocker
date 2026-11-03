namespace mw2_fps_unlocker.Core;

internal enum RunMode { Detect, ReadOnly, Patch, FpsOnce }

internal sealed record Options(RunMode Mode, string? ProfilePath, string? LogDirectory, int? FpsOnceTarget = null)
{
    internal static Options Parse(string[] args)
    {
        var mode = RunMode.Detect;
        string? profile = null, logs = null;
        int? fps = null;
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
                    if (!int.TryParse(Value(), out int target) || target is < 75 or > 500)
                        throw new ArgumentException("FPS target must be an integer between 75 and 500.");
                    fps = target;
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
        if (mode == RunMode.FpsOnce && fps == null)
            throw new ArgumentException("fps-once requires an explicit --fps target.");
        if (mode != RunMode.FpsOnce && fps != null)
            throw new ArgumentException("--fps is only accepted with --mode fps-once.");
        return new(mode, profile, logs, fps);
    }
}
