namespace BL.Helpers;

using System.Threading;

/// <summary>
/// A non-blocking exclusion mechanism.
/// Used to skip an operation if a previous execution is still in progress.
/// </summary>
internal class AsyncMutex
{
    private int _isLocked = 0;

    /// <summary>
    /// Checks if the mutex is locked. 
    /// If not locked (0) -> Sets to 1 and returns false (meaning: "It was free, I took it, proceed").
    /// If locked (1) -> Returns true (meaning: "It is busy, skip execution").
    /// </summary>
    public bool CheckAndSetInProgress()
    {
        // CompareExchange(ref location, value, comparand)
        // If _isLocked is 0 (free), replace with 1 (busy) and return original (0).
        // If _isLocked is 1 (busy), leave as 1 and return original (1).
        return Interlocked.CompareExchange(ref _isLocked, 1, 0) == 1;
    }

    /// <summary>
    /// Releases the lock (sets back to 0).
    /// </summary>
    public void UnsetInProgress()
    {
        Interlocked.Exchange(ref _isLocked, 0);
    }
}
