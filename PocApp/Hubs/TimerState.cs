namespace PocApp.Hubs;

/// <summary>
/// Stopwatch semantics: ElapsedSeconds counts wall time spent in the "running"
/// state and freezes while paused. Pressing Stop captures the current elapsed
/// value; pressing Start resumes from that exact value (no time skipped over).
///
/// Singleton — all hub connections and the BackgroundService share this instance.
/// Trivially thread-safe via the lock; for horizontal scaling you'd put this
/// state in Redis behind a Redis-backed SignalR group.
/// </summary>
public class TimerState
{
    private readonly object _lock = new();
    private long _elapsedAtPauseSec;
    private DateTime? _resumedAt = DateTime.UtcNow;

    public bool IsRunning
    {
        get { lock (_lock) return _resumedAt.HasValue; }
    }

    public long ElapsedSeconds
    {
        get
        {
            lock (_lock)
            {
                if (_resumedAt is { } at)
                    return _elapsedAtPauseSec + (long)(DateTime.UtcNow - at).TotalSeconds;
                return _elapsedAtPauseSec;
            }
        }
    }

    /// <summary>Pause. Returns true if the state actually changed.</summary>
    public bool TryPause()
    {
        lock (_lock)
        {
            if (_resumedAt is null) return false;
            _elapsedAtPauseSec += (long)(DateTime.UtcNow - _resumedAt.Value).TotalSeconds;
            _resumedAt = null;
            return true;
        }
    }

    /// <summary>Resume. Returns true if the state actually changed.</summary>
    public bool TryResume()
    {
        lock (_lock)
        {
            if (_resumedAt is not null) return false;
            _resumedAt = DateTime.UtcNow;
            return true;
        }
    }
}
