using System;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonGuide;

/// <summary>Owns one cancellable private disk load and prevents publication after its session has been replaced.</summary>
internal sealed class GeometryCacheRestore
{
    private readonly GeometryCacheRepository _repository;
    private readonly GeometryCacheWorker _worker;
    private CancellationTokenSource _cancellation;
    private Task<(CachedGeometry Geometry, ScanResult Scan)> _pending;
    private DungeonSession _session;

    /// <summary>Shares serialized I/O with cache writes so invalidation and replacement have a defined order.</summary>
    public GeometryCacheRestore(GeometryCacheRepository repository, GeometryCacheWorker worker)
    {
        _repository = repository;
        _worker = worker;
    }

    /// <summary>Starts private disk I/O and indexing; no caller waits on the worker.</summary>
    public void Begin(DungeonSession session)
    {
        Cancel();
        _session = session;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var key = session.GeometryCacheKey;
        _pending = _worker.Enqueue(() => Load(key, token));
    }

    private (CachedGeometry, ScanResult) Load(string key, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var geometry = _repository.Load(key, token);
        return geometry == null ? (null, null) : (geometry, CachedGeometryBinding.Index(geometry, token));
    }

    /// <summary>Reports one completed matching load, including misses/errors, without exposing stale results.</summary>
    public bool TryTake(DungeonSession current, out CachedGeometry geometry, out ScanResult scan, out Exception error)
    {
        geometry = null;
        scan = null;
        error = null;
        if (_pending == null || !_pending.IsCompleted || _session != current)
        {
            return false;
        }

        if (_pending.IsFaulted)
        {
            error = _pending.Exception.GetBaseException();
        }
        else if (!_pending.IsCanceled)
        {
            geometry = _pending.Result.Geometry;
            scan = _pending.Result.Scan;
        }

        Cancel();
        return true;
    }

    /// <summary>Cancels abandoned reads and observes eventual faults without blocking the game thread.</summary>
    public void Cancel()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        if (_pending != null)
        {
            _pending.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        _pending = null;
        _session = null;
    }
}
