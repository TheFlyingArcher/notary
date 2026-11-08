using Microsoft.Extensions.Time.Testing;
using Notary.Loading;

namespace Notary.Test;

public class SimulatedLatencyLoadingTrackerTest
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(1500);

    private FakeTimeProvider _time = null!;
    private LoadingTracker _inner = null!;
    private SimulatedLatencyLoadingTracker _tracker = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _inner = new LoadingTracker(_time, LoadingTrackerOptions.Default);
        _tracker = new SimulatedLatencyLoadingTracker(_inner, Delay, _time);
    }

    [TearDown]
    public void TearDown()
    {
        _inner.Dispose();
    }

    [Test]
    public async Task RunAsync_DoesNotStartTheOperationUntilTheDelayHasPassed()
    {
        var started = false;

        var run = _tracker.RunAsync(() =>
        {
            started = true;
            return Task.CompletedTask;
        });

        _time.Advance(Delay - TimeSpan.FromMilliseconds(1));
        Assert.That(started, Is.False);
        Assert.That(run.IsCompleted, Is.False);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        await run;
        Assert.That(started, Is.True);
    }

    [Test]
    public async Task RunAsync_Generic_ReturnsTheValueAfterTheDelay()
    {
        var run = _tracker.RunAsync(() => Task.FromResult(7));
        Assert.That(run.IsCompleted, Is.False);

        _time.Advance(Delay);

        Assert.That(await run, Is.EqualTo(7));
    }

    [Test]
    public void RunAsync_ThrowingOperation_ReachesCallerAndEndsTheOperation()
    {
        var run = _tracker.RunAsync(() => Task.FromException<int>(new InvalidOperationException("boom")));
        _time.Advance(Delay);

        Assert.That(async () => await run, Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public async Task RunAsync_ShowsTheOverlayWhileItWaits()
    {
        var run = _tracker.RunAsync(() => Task.CompletedTask);

        _time.Advance(LoadingTrackerOptions.Default.ShowDelay);
        Assert.That(_tracker.IsLoading, Is.True);

        _time.Advance(Delay);
        await run;
        _time.Advance(LoadingTrackerOptions.Default.MinimumVisible);
        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void Changed_IsForwardedToTheInnerTracker()
    {
        var changes = new List<bool>();
        void OnChanged() => changes.Add(_tracker.IsLoading);

        _tracker.Changed += OnChanged;
        var scope = _tracker.Begin();
        _time.Advance(LoadingTrackerOptions.Default.ShowDelay);
        Assert.That(changes, Is.EqualTo(new[] { true }));

        _tracker.Changed -= OnChanged;
        scope.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(changes, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void Begin_IsNotDelayed()
    {
        using var scope = _tracker.Begin();
        _time.Advance(LoadingTrackerOptions.Default.ShowDelay);

        Assert.That(_tracker.IsLoading, Is.True);
    }

    [Test]
    public void NullArguments_Throw()
    {
        Assert.That(() => new SimulatedLatencyLoadingTracker(null!, Delay, _time), Throws.ArgumentNullException);
        Assert.That(() => new SimulatedLatencyLoadingTracker(_inner, Delay, null!), Throws.ArgumentNullException);
        Assert.That(() => _tracker.RunAsync((Func<Task>)null!), Throws.ArgumentNullException);
        Assert.That(() => _tracker.RunAsync((Func<Task<int>>)null!), Throws.ArgumentNullException);
    }
}
