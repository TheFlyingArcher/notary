using System;
using System.Threading.Tasks;

namespace Notary.Loading;

/// <summary>
/// Development aid for trying the loading overlay on a machine fast enough that real calls never show it. Every
/// operation run through <see cref="RunAsync(Func{Task})"/> waits <c>delay</c> before it starts. Never registered
/// outside the Development environment.
/// </summary>
public sealed class SimulatedLatencyLoadingTracker : ILoadingTracker
{
    private readonly ILoadingTracker _inner;
    private readonly TimeSpan _delay;
    private readonly TimeProvider _time;

    public SimulatedLatencyLoadingTracker(ILoadingTracker inner, TimeSpan delay, TimeProvider time)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _delay = delay;
    }

    public bool IsLoading => _inner.IsLoading;

    public event Action? Changed
    {
        add => _inner.Changed += value;
        remove => _inner.Changed -= value;
    }

    public IDisposable Begin() => _inner.Begin();

    public Task RunAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return _inner.RunAsync(async () =>
        {
            await Task.Delay(_delay, _time).ConfigureAwait(false);
            await operation().ConfigureAwait(false);
        });
    }

    public Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return _inner.RunAsync(async () =>
        {
            await Task.Delay(_delay, _time).ConfigureAwait(false);
            return await operation().ConfigureAwait(false);
        });
    }
}
