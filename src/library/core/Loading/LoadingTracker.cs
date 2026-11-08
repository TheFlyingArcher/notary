using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notary.Loading;

/// <summary>
/// Default <see cref="ILoadingTracker"/>. Register one per Blazor circuit (scoped).
/// </summary>
public sealed class LoadingTracker : ILoadingTracker, IDisposable
{
    private readonly TimeProvider _time;
    private readonly LoadingTrackerOptions _options;
    private readonly object _gate = new();

    private int _active;
    private bool _isLoading;
    private DateTimeOffset _shownAt;
    private ITimer? _timer;
    // A timer callback acts only if its version is still current, so a callback that was already running when its
    // timer was cancelled does nothing
    private long _version;
    private bool _disposed;

    public LoadingTracker(TimeProvider time, LoadingTrackerOptions options)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsLoading
    {
        get
        {
            lock (_gate)
            {
                return _isLoading;
            }
        }
    }

    public event Action? Changed;

    public IDisposable Begin()
    {
        lock (_gate)
        {
            _active++;

            if (_active == 1)
            {
                if (_isLoading)
                {
                    // The overlay is waiting out its minimum time; more work keeps it up
                    CancelTimer();
                }
                else
                {
                    ScheduleTimer(_options.ShowDelay, OnShowDelayElapsed);
                }
            }
        }

        return new Scope(this);
    }

    public async Task RunAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using (Begin())
        {
            await operation().ConfigureAwait(false);
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using (Begin())
        {
            return await operation().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            CancelTimer();
        }
    }

    private void End()
    {
        var changed = false;

        lock (_gate)
        {
            if (_active == 0)
            {
                return;
            }

            _active--;

            if (_active > 0)
            {
                return;
            }

            if (!_isLoading)
            {
                // Finished before the show delay, so the overlay never appears
                CancelTimer();
                return;
            }

            var remaining = _options.MinimumVisible - (_time.GetUtcNow() - _shownAt);

            if (remaining <= TimeSpan.Zero)
            {
                _isLoading = false;
                changed = true;
            }
            else
            {
                ScheduleTimer(remaining, OnMinimumVisibleElapsed);
            }
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    private void OnShowDelayElapsed(long version)
    {
        lock (_gate)
        {
            if (version != _version || _active == 0 || _isLoading)
            {
                return;
            }

            _isLoading = true;
            _shownAt = _time.GetUtcNow();
        }

        Changed?.Invoke();
    }

    private void OnMinimumVisibleElapsed(long version)
    {
        lock (_gate)
        {
            if (version != _version || _active > 0 || !_isLoading)
            {
                return;
            }

            _isLoading = false;
        }

        Changed?.Invoke();
    }

    // Callers hold _gate
    private void ScheduleTimer(TimeSpan delay, Action<long> callback)
    {
        CancelTimer();

        if (_disposed)
        {
            return;
        }

        var version = _version;
        _timer = _time.CreateTimer(_ => callback(version), null, delay, Timeout.InfiniteTimeSpan);
    }

    // Callers hold _gate
    private void CancelTimer()
    {
        _version++;
        _timer?.Dispose();
        _timer = null;
    }

    private sealed class Scope(LoadingTracker owner) : IDisposable
    {
        private int _ended;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 0)
            {
                owner.End();
            }
        }
    }
}
