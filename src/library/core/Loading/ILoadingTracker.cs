using System;
using System.Threading.Tasks;

namespace Notary.Loading;

/// <summary>
/// Counts the operations in flight so the UI can show a busy overlay. <see cref="IsLoading"/> is true only once work has
/// been running for the show delay, and stays true for the minimum visible time, so quick calls never flash it.
/// </summary>
public interface ILoadingTracker
{
    /// <summary>
    /// True while the overlay should be visible
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Raised when <see cref="IsLoading"/> changes. It can be raised from any thread.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Marks an operation as started. Dispose the result when it ends; disposing more than once is harmless.
    /// </summary>
    IDisposable Begin();

    /// <summary>
    /// Runs <paramref name="operation"/> as a tracked operation. Any exception reaches the caller.
    /// </summary>
    Task RunAsync(Func<Task> operation);

    /// <summary>
    /// Runs <paramref name="operation"/> as a tracked operation and returns its result. Any exception reaches the caller.
    /// </summary>
    Task<T> RunAsync<T>(Func<Task<T>> operation);
}
