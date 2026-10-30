using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects nonblocking polling, cancellable shutdown, floor-analysis results, and failure propagation.
/// </summary>
public sealed class FloorAnalysisJobTests
{
    /// <summary>
    /// A worker that has not finished cannot make a completion poll wait for it.
    /// </summary>
    [Fact]
    public async Task PendingPollDoesNotWaitAsync()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var job = new FloorAnalysisJob(token =>
        {
            entered.SetResult(true);
            try
            {
                if (!release.Wait(TimeSpan.FromSeconds(15)))
                {
                    throw new TimeoutException("Test worker was not released.");
                }

                return new FloorLayout();
            }
            finally
            {
                exited.SetResult(true);
            }
        });

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var poll = Task.Run(() => job.TryComplete(out _));

            Assert.False(await poll.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.False(exited.Task.IsCompleted);
        }
        finally
        {
            release.Set();
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Closing a scan requests cancellation and returns while the worker is still unwinding.
    /// </summary>
    [Fact]
    public async Task DisposeDoesNotJoinWorkerAsync()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var job = new FloorAnalysisJob(token =>
        {
            using var registration = token.Register(() => cancelled.TrySetResult(true));
            entered.SetResult(true);
            try
            {
                if (!release.Wait(TimeSpan.FromSeconds(15)))
                {
                    throw new TimeoutException("Test worker was not released.");
                }

                token.ThrowIfCancellationRequested();
                return new FloorLayout();
            }
            finally
            {
                exited.SetResult(true);
            }
        });

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Run(job.Dispose).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.False(exited.Task.IsCompleted);
            Assert.False(job.TryComplete(out _));
            job.Dispose();
        }
        finally
        {
            release.Set();
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Moving analysis to a worker preserves floor elevations, landing nodes, and per-node floor assignments.
    /// </summary>
    [Fact]
    public async Task PreservesFloorDetectionResultsAsync()
    {
        var expectedGraph = Floors();
        var workerGraph = Floors();
        var detector = new FloorDetector(new FloorLandingDetector());
        var expected = detector.Detect(expectedGraph, 1.25f);
        using var job = new FloorAnalysisJob(detector, workerGraph, 1.25f);
        var actual = await CompleteAsync(job);

        Assert.Equal(expected.Levels, actual.Levels);
        Assert.Equal(expected.LandingNodes.OrderBy(n => n), actual.LandingNodes.OrderBy(n => n));
        Assert.Equal(expectedGraph.Nodes.Select(n => n.Floor), workerGraph.Nodes.Select(n => n.Floor));
    }

    /// <summary>
    /// Worker failures are surfaced to the caller rather than leaving a scan pending forever.
    /// </summary>
    [Fact]
    public async Task PropagatesWorkerFailureAsync()
    {
        using var job = new FloorAnalysisJob(token => throw new InvalidOperationException("Floor analysis failed."));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => { await CompleteAsync(job); });

        Assert.Equal("Floor analysis failed.", error.Message);
    }

    /// <summary>
    /// Cancelled floor analysis stops before changing a previously assigned floor index.
    /// </summary>
    [Fact]
    public void CancelledDetectorDoesNotMutateGraph()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)) { Floor = 42 });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new FloorDetector(new FloorLandingDetector()).Detect(graph, 1.25f, cancellation.Token));
        Assert.Equal(42, graph.Nodes[0].Floor);
    }

    /// <summary>
    /// Landing analysis observes cancellation before traversing surface samples.
    /// </summary>
    [Fact]
    public void CancelledLandingAnalysisStops()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new FloorLandingDetector().Find(new NavGraph(), new List<int>(), 1.25f, cancellation.Token));
    }

    private static async Task<FloorLayout> CompleteAsync(FloorAnalysisJob job)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        FloorLayout layout;
        while (!job.TryComplete(out layout))
        {
            await Task.Delay(1, timeout.Token);
        }

        return layout;
    }

    private static NavGraph Floors()
    {
        var graph = new NavGraph();
        TestSurfaces.Add(graph, 0, 8, 8, 0);
        TestSurfaces.Add(graph, 4, 16, 16, 0, true);
        TestSurfaces.Add(graph, 12, 8, 8, 0);
        return graph;
    }
}
