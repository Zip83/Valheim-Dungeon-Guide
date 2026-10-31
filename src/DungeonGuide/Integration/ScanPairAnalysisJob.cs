using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonGuide;

/// <summary>Runs cancellable pure endpoint preselection while sampled geometry remains privately owned.</summary>
internal sealed class ScanPairAnalysisJob : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task<PassagePairCatalog> _task;
    private bool _disposed;

    /// <summary>Starts work without accessing Unity or publishing the input graph.</summary>
    public ScanPairAnalysisJob(NavGraph graph, IReadOnlyDictionary<(int, int), List<int>> columns)
    {
        var token = _cancellation.Token;
        _task = Task.Run(() => PassagePairCatalog.Build(graph, columns, token), token);
        _task.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>Polls without waiting and propagates completed failures to the scan guard.</summary>
    public bool TryComplete(out PassagePairCatalog catalog)
    {
        catalog = null;
        if (_disposed || !_task.IsCompleted)
        {
            return false;
        }

        catalog = _task.GetAwaiter().GetResult();
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cancellation.Cancel();
            _task.ContinueWith(_ => _cancellation.Dispose(), TaskScheduler.Default);
        }
    }
}
