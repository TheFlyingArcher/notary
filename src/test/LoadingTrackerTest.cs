using Microsoft.Extensions.Time.Testing;
using Notary.Loading;

namespace Notary.Test;

public class LoadingTrackerTest
{
    private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MinimumVisible = TimeSpan.FromMilliseconds(300);

    private FakeTimeProvider _time = null!;
    private LoadingTracker _tracker = null!;
    private List<bool> _changes = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _tracker = new LoadingTracker(_time, new LoadingTrackerOptions(ShowDelay, MinimumVisible));
        _changes = [];
        _tracker.Changed += () => _changes.Add(_tracker.IsLoading);
    }

    [TearDown]
    public void TearDown()
    {
        _tracker.Dispose();
    }

    [Test]
    public void New_Tracker_IsNotLoading()
    {
        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.Empty);
    }

    [Test]
    public void DefaultOptions_UseTwoHundredAndThreeHundredMilliseconds()
    {
        Assert.That(LoadingTrackerOptions.Default.ShowDelay, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
        Assert.That(LoadingTrackerOptions.Default.MinimumVisible, Is.EqualTo(TimeSpan.FromMilliseconds(300)));
    }

    [Test]
    public void Begin_BeforeShowDelay_IsNotLoading()
    {
        using var scope = _tracker.Begin();
        _time.Advance(ShowDelay - TimeSpan.FromMilliseconds(1));

        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void Begin_AfterShowDelay_IsLoadingAndRaisesChangedOnce()
    {
        using var scope = _tracker.Begin();
        _time.Advance(ShowDelay);
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.That(_tracker.IsLoading, Is.True);
        Assert.That(_changes, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void ShortOperation_NeverShowsOrRaisesChanged()
    {
        var scope = _tracker.Begin();
        _time.Advance(TimeSpan.FromMilliseconds(100));
        scope.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.Empty);
    }

    [Test]
    public void Overlapping_Operations_KeepLoadingUntilTheLastEnds()
    {
        var first = _tracker.Begin();
        _time.Advance(TimeSpan.FromMilliseconds(100));
        var second = _tracker.Begin();
        _time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.That(_tracker.IsLoading, Is.True);

        first.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(_tracker.IsLoading, Is.True);

        second.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void Overlay_StaysForTheMinimumVisibleTime()
    {
        var scope = _tracker.Begin();
        _time.Advance(ShowDelay);
        scope.Dispose();

        _time.Advance(MinimumVisible - TimeSpan.FromMilliseconds(1));
        Assert.That(_tracker.IsLoading, Is.True);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void Overlay_VisibleLongerThanMinimum_HidesImmediately()
    {
        var scope = _tracker.Begin();
        _time.Advance(ShowDelay);
        _time.Advance(MinimumVisible);
        scope.Dispose();

        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void NewOperation_DuringMinimumVisibleWait_KeepsTheOverlayUp()
    {
        var first = _tracker.Begin();
        _time.Advance(ShowDelay);
        first.Dispose();
        _time.Advance(TimeSpan.FromMilliseconds(100));

        var second = _tracker.Begin();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.That(_tracker.IsLoading, Is.True);
        Assert.That(_changes, Is.EqualTo(new[] { true }));

        second.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void Scope_DisposedTwice_CountsOnce()
    {
        var first = _tracker.Begin();
        using var second = _tracker.Begin();
        _time.Advance(ShowDelay);

        first.Dispose();
        first.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.That(_tracker.IsLoading, Is.True);
    }

    [Test]
    public async Task RunAsync_ReturnsTheValue()
    {
        var value = await _tracker.RunAsync(() => Task.FromResult(42));

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public async Task RunAsync_CountsTheOperationWhileItRuns()
    {
        var gate = new TaskCompletionSource();

        var run = _tracker.RunAsync(() => gate.Task);
        _time.Advance(ShowDelay);
        Assert.That(_tracker.IsLoading, Is.True);

        gate.SetResult();
        await run;
        _time.Advance(MinimumVisible);

        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void RunAsync_Throws_ExceptionReachesCallerAndOverlayClears()
    {
        var gate = new TaskCompletionSource<int>();

        var run = _tracker.RunAsync(() => gate.Task);
        _time.Advance(ShowDelay);
        gate.SetException(new InvalidOperationException("boom"));

        Assert.That(async () => await run, Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        _time.Advance(MinimumVisible);
        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void RunAsync_Cancelled_ExceptionReachesCallerAndOverlayClears()
    {
        using var cts = new CancellationTokenSource();
        var run = _tracker.RunAsync(() => Task.Delay(Timeout.Infinite, cts.Token));
        _time.Advance(ShowDelay);

        cts.Cancel();

        Assert.That(async () => await run, Throws.InstanceOf<OperationCanceledException>());
        _time.Advance(MinimumVisible);
        Assert.That(_tracker.IsLoading, Is.False);
    }

    [Test]
    public void RunAsync_OperationThrowsSynchronously_StillEndsTheScope()
    {
        Assert.That(async () => await _tracker.RunAsync((Func<Task>)(() => throw new InvalidOperationException())),
            Throws.InvalidOperationException);

        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.Empty);
    }

    [Test]
    public void RunAsync_NullOperation_Throws()
    {
        Assert.That(async () => await _tracker.RunAsync((Func<Task>)null!), Throws.ArgumentNullException);
        Assert.That(async () => await _tracker.RunAsync((Func<Task<int>>)null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        Assert.That(() => new LoadingTracker(null!, LoadingTrackerOptions.Default), Throws.ArgumentNullException);
        Assert.That(() => new LoadingTracker(_time, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Dispose_StopsPendingTimers()
    {
        var scope = _tracker.Begin();
        _tracker.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.That(_tracker.IsLoading, Is.False);
        Assert.That(_changes, Is.Empty);
        scope.Dispose();
    }
}
