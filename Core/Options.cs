namespace mw2_fps_unlocker.Core;

internal enum RunMode { Detect, ReadOnly, Patch }

internal sealed record Options(RunMode Mode, string? ProfilePath, string? LogDirectory)
{
    internal static Options Parse(string[] args)
    {
        var mode = RunMode.Detect;
        string? profile = null, logs = null;
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
                        _ => throw new ArgumentException("Mode must be detect, read-only, or patch.")
                    };
                    break;
                case "--profile": profile = Value(); break;
                case "--log-dir": logs = Value(); break;
                default: throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }
#if !ENABLE_MEMORY_WRITES
        if (mode == RunMode.Patch)
            throw new ArgumentException("This diagnostic binary has no memory-write implementation. Use detect or read-only.");
#endif
        if (mode == RunMode.Patch && profile == null)
            throw new ArgumentException("Patch mode requires an independently verified --profile file.");
        return new(mode, profile, logs);
    }
}
