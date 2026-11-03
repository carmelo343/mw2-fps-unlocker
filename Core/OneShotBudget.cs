namespace mw2_fps_unlocker.Core;

// One attempt for the entire application run, including failed writes and game restarts.
internal sealed class OneShotBudget
{
    private int attempted;
    internal bool Attempted => Volatile.Read(ref attempted) != 0;
    internal bool TryConsume() => Interlocked.CompareExchange(ref attempted, 1, 0) == 0;
}
