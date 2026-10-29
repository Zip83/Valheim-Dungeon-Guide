using System;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonGuide;

/// <summary>
/// Runs pure floor analysis on a private graph without synchronously waiting on the game thread.
/// </summary>
internal sealed class FloorAnalysisJob : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task<FloorLayout> _task;
    private bool _disposed;

    /// <summary>
    /// Starts cancellable analysis of a graph that must remain unpublished until this job completes.
    /// </summary>
    /// <param name="detector">Stateless floor and landing analysis policy.</param>
    /// <param name="graph">Exclusively owned graph that no other thread may read or modify during analysis.</param>
    /// <param name="spacing">Horizontal sample spacing in world metres.</param>
    public FloorAnalysisJob(FloorDetector detector, NavGraph graph, float spacing)
        : this(token => detector.Detect(graph, spacing, token))
    {
    }

    internal FloorAnalysisJob(Func<CancellationToken, FloorLayout> analyze)
    {
        var token = _cancellation.Token;
        _task = Task.Run(() => analyze(token), token);
        _task.ContinueWith(
            task => { _ = task.Exception; },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Polls without waiting; completed failures are propagated to the game-thread scan error handler.
    /// </summary>
    /// <param name="layout">Completed floor layout, or null when no result may be published.</param>
    /// <returns>True only when a successful, non-disposed job has finished.</returns>
    public bool TryComplete(out FloorLayout layout)
    {
        layout = null;
        if (_disposed || !_task.IsCompleted)
        {
            return false;
        }

        // Completion was checked first, so retrieving the outcome cannot wait for worker execution.
        layout = _task.GetAwaiter().GetResult();
        return true;
    }

    /// <summary>
    /// Requests cooperative cancellation without joining the worker or waiting for it to finish.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();
    }
}
