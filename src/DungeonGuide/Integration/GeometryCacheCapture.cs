using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace DungeonGuide;

/// <summary>Copies graph values in bounded game-thread slices, then queues only sealed bytes for disk writes.</summary>
internal sealed class GeometryCacheCapture : IDisposable
{
    private readonly GeometryCacheRepository _repository;
    private readonly GeometryCacheWorker _worker;
    private readonly ManualLogSource _log;
    private readonly Queue<(string Key, int Version, CachedGeometry Geometry)> _queue = new();
    private readonly Dictionary<string, int> _versions = new();
    private readonly List<Task<bool>> _writes = new();
    private readonly FrameWorkBudget _budget = new(8192, 0.5);
    private (string Key, int Version, CachedGeometry Geometry) _active;
    private GeometryCacheWriter _writer;
    private IEnumerator<byte> _steps;

    /// <summary>Shares one ordered worker with load and invalidation requests.</summary>
    public GeometryCacheCapture(GeometryCacheRepository repository, GeometryCacheWorker worker, ManualLogSource log)
    {
        _repository = repository;
        _worker = worker;
        _log = log;
    }

    /// <summary>Queues a published snapshot, superseding older unfinished copies of the same layout.</summary>
    public void Enqueue(string key, CachedGeometry geometry)
    {
        Cancel(key);
        _queue.Enqueue((key, _versions[key], geometry));
    }

    /// <summary>Prevents an obsolete in-flight copy from being written after layout invalidation.</summary>
    public void Cancel(string key)
    {
        _versions[key] = _versions.TryGetValue(key, out var version) ? version + 1 : 1;
    }

    /// <summary>Advances only bounded value copies; disk I/O never runs on this caller.</summary>
    public void Tick()
    {
        ObserveWrites();
        _budget.BeginFrame();
        while (_budget.TryStartWork())
        {
            if (_steps == null && !StartNext())
            {
                return;
            }

            if (_active.Version != _versions[_active.Key])
            {
                ReleaseCopy();
                continue;
            }

            AdvanceCopy();
        }
    }

    private bool StartNext()
    {
        while (_queue.Count > 0)
        {
            _active = _queue.Dequeue();
            if (_active.Version == _versions[_active.Key])
            {
                _writer = new GeometryCacheWriter();
                _steps = _writer.Encode(_active.Key, _active.Geometry).GetEnumerator();
                return true;
            }
        }

        return false;
    }

    private void AdvanceCopy()
    {
        try
        {
            var more = _steps.MoveNext();
            if (_writer.Length > GeometryCacheFormat.MaxBytes)
            {
                throw new InvalidDataException("The dungeon geometry exceeds the cache size limit.");
            }

            if (!more)
            {
                QueueWrite();
                ReleaseCopy();
            }
        }
        catch (Exception error)
        {
            _log.LogWarning("Cannot capture dungeon geometry; the live map remains available: " + error);
            ReleaseCopy();
        }
    }

    private void QueueWrite()
    {
        var key = _active.Key;
        var bytes = _writer.Payload;
        _writes.Add(_worker.Enqueue(() => _repository.Save(key, bytes)));
    }

    private void ObserveWrites()
    {
        for (var i = _writes.Count - 1; i >= 0; i--)
        {
            var task = _writes[i];
            if (!task.IsCompleted)
            {
                continue;
            }

            if (task.IsFaulted)
            {
                _log.LogWarning("Cannot save dungeon geometry cache: " + task.Exception.GetBaseException());
            }
            else if (!task.Result)
            {
                _log.LogWarning("Preserving a newer or unreadable dungeon geometry cache; live mapping remains available.");
            }

            _writes.RemoveAt(i);
        }
    }

    private void ReleaseCopy()
    {
        _steps?.Dispose();
        _steps = null;
        _writer?.Dispose();
        _writer = null;
        _active = default;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseCopy();
        _queue.Clear();
        foreach (var task in _writes)
        {
            task.ContinueWith(failed => { _ = failed.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        _writes.Clear();
    }
}
