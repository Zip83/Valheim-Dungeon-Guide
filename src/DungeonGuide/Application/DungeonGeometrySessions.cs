using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace DungeonGuide;

/// <summary>Coordinates matching-session reuse, private disk restore and incremental geometry persistence.</summary>
internal sealed class DungeonGeometrySessions : IDisposable
{
    private readonly GuideSettings _settings;
    private readonly ManualLogSource _log;
    private readonly DungeonSessionCache _memory = new();
    private readonly GeometryCacheRepository _repository;
    private readonly GeometryCacheWorker _worker = new();
    private readonly GeometryCacheRestore _restore;
    private readonly GeometryCacheCapture _capture;
    private readonly RestoredPassageValidation _validation;
    private readonly List<Task<bool>> _invalidations = new();
    private string _world;

    /// <summary>Signals that restored geometry can be presented without a new scan.</summary>
    public event Action Restored;

    /// <summary>Signals a cache miss or incomplete cache that requires automatic scanning.</summary>
    public event Action<DungeonSession> NeedsScan;

    /// <summary>Invalidates routes when current-world passage permissions need fresh verification.</summary>
    public event Action PermissionsChanged;

    /// <summary>Releases presentation caches when the connection or world changes rather than on dungeon exits.</summary>
    public event Action WorldChanged;

    /// <summary>Connects independent storage, snapshot copy, restore and recent-session retention services.</summary>
    public DungeonGeometrySessions(GuideSettings settings, GeometryCacheRepository repository, ManualLogSource log,
        RestoredPassageValidation validation)
    {
        _settings = settings;
        _repository = repository;
        _log = log;
        _validation = validation;
        _restore = new GeometryCacheRestore(repository, _worker);
        _capture = new GeometryCacheCapture(repository, _worker, log);
    }

    /// <summary>Resumes the exact retained session only when world, rooms and sampling inputs still match.</summary>
    public DungeonSession Resume(DungeonArea area, string world)
    {
        SelectWorld(world);
        var key = GeometryCacheKey.Create(area, world, _settings);
        var session = _memory.Find(key);
        if (session == null)
        {
            return null;
        }

        if (BindArea(session, area) && session.Scan != null)
        {
            _capture.Enqueue(key, CachedGeometryBinding.Capture(session));
        }

        _validation.Begin(session);
        _log.LogInfo("Dungeon geometry reused from memory: " + area.Id);
        return session;
    }

    private static bool BindArea(DungeonSession session, DungeonArea area)
    {
        if (!area.ObservedEntrance && session.Area.ObservedEntrance)
        {
            area.Entrance = session.Area.Entrance;
            area.ObservedEntrance = true;
        }

        var changed = area.ObservedEntrance && (!session.Area.ObservedEntrance
            || Point.Distance(area.Entrance.Value, session.Area.Entrance.Value) > 0.01f);
        session.Area = area;
        return changed;
    }

    /// <summary>Recognizes changed room geometry while preserving the already visible map until automatic replacement.</summary>
    public bool RefreshArea(DungeonSession session, DungeonArea area)
    {
        var key = GeometryCacheKey.Create(area, session.Data.world, _settings);
        var changed = key != session.GeometryCacheKey;
        BindArea(session, area);
        if (changed)
        {
            _memory.Remove(session.GeometryCacheKey);
            _capture.Cancel(session.GeometryCacheKey);
            _validation.Cancel();
            session.GeometryCacheKey = key;
            session.GeometryNeedsScan = true;
            session.HasPartialGeometry = true;
            session.NavigationReady = false;
            PermissionsChanged?.Invoke();
        }

        return changed;
    }

    /// <summary>Refreshes current door and destructible permissions without resampling dungeon floors.</summary>
    public void RefreshPermissions(DungeonSession session)
    {
        if (session?.Scan == null || session.HasPartialGeometry || session.GeometryNeedsScan || session.Scanning)
        {
            return;
        }

        _validation.Begin(session);
        PermissionsChanged?.Invoke();
    }

    /// <summary>Starts an asynchronous persisted-map load for a newly opened session.</summary>
    public void Open(DungeonSession session)
    {
        session.GeometryCacheKey = GeometryCacheKey.Create(session.Area, session.Data.world, _settings);
        var retained = _memory.Find(session.GeometryCacheKey);
        if (retained != null && retained != session && retained.Scan != null)
        {
            CachedGeometryBinding.Restore(session, CachedGeometryBinding.Capture(retained), retained.Scan);
            _memory.Remember(session.GeometryCacheKey, session);
            _validation.Begin(session);
            Restored?.Invoke();
            if (session.GeometryNeedsScan)
            {
                NeedsScan?.Invoke(session);
            }

            return;
        }

        _restore.Begin(session);
    }

    /// <summary>Retains and schedules a published snapshot, including preview state that still needs completion.</summary>
    public void Published(DungeonSession session)
    {
        if (session?.Scan == null || session.Scan.Graph.Nodes.Count == 0 || session.Floors.Count == 0)
        {
            return;
        }

        _memory.Remember(session.GeometryCacheKey, session);
        if (session.HasPartialGeometry)
        {
            _capture.Enqueue(session.GeometryCacheKey, CachedGeometryBinding.Capture(session));
        }

        if (!session.HasPartialGeometry)
        {
            _validation.Begin(session);
        }
    }

    /// <summary>Queues completed private-scan bytes before full-map publication without copying its graph again.</summary>
    public Task<bool> StorePrepared(DungeonSession session, byte[] payload, CancellationToken cancellation)
    {
        var key = session.GeometryCacheKey;
        _capture.Cancel(key);
        return _worker.Enqueue(() => _repository.Save(key, payload, cancellation));
    }

    /// <summary>Detaches a closing session without discarding its already published map.</summary>
    public void Leave(DungeonSession session)
    {
        _restore.Cancel();
        _validation.Cancel();
        if (session.Scan != null && session.Scan.Graph.Nodes.Count > 0 && session.Data.world == _world)
        {
            _memory.Remember(session.GeometryCacheKey, session);
        }
    }

    /// <summary>Invalidates stored geometry automatically before a replacement scan begins.</summary>
    public void Invalidate(DungeonSession session, bool mutation = false)
    {
        if (mutation)
        {
            session.GeometryDirty = true;
            session.GeometryMutationVersion++;
        }

        session.GeometryNeedsScan = true;
        session.NavigationReady = false;
        _validation.Cancel();
        if (session.Scan != null)
        {
            PermissionsChanged?.Invoke();
        }

        var key = session.GeometryCacheKey;
        _capture.Cancel(key);
        _invalidations.Add(_worker.Enqueue(() =>
        {
            _repository.Invalidate(key);
            return true;
        }));
    }

    /// <summary>Polls matching loads and advances copying even while the player is outside a dungeon.</summary>
    public void Tick(DungeonSession current, string world)
    {
        SelectWorld(world);
        ObserveInvalidations();
        CompleteRestore(current);
        _validation.Tick(current);
        _capture.Tick();
    }

    private void SelectWorld(string world)
    {
        if (_world != world)
        {
            _world = world;
            _restore.Cancel();
            _validation.Cancel();
            _memory.Clear();
            WorldChanged?.Invoke();
        }
    }

    private void CompleteRestore(DungeonSession session)
    {
        if (!_restore.TryTake(session, out var geometry, out var scan, out var error))
        {
            return;
        }

        if (error != null)
        {
            _log.LogWarning("Cannot restore dungeon geometry; scanning automatically: " + error);
        }

        if (geometry == null || scan.Graph.Nodes.Count == 0)
        {
            NeedsScan?.Invoke(session);
            return;
        }

        if (geometry.Complete || session.Scan == null)
        {
            CachedGeometryBinding.Restore(session, geometry, scan);
        }

        _validation.Begin(session);
        _memory.Remember(session.GeometryCacheKey, session);
        _log.LogInfo("Dungeon geometry restored from disk: " + session.Area.Id + ", nodes=" + scan.Graph.Nodes.Count);
        Restored?.Invoke();
        if (session.GeometryNeedsScan)
        {
            NeedsScan?.Invoke(session);
        }
    }

    private void ObserveInvalidations()
    {
        for (var i = _invalidations.Count - 1; i >= 0; i--)
        {
            var task = _invalidations[i];
            if (!task.IsCompleted)
            {
                continue;
            }

            if (task.IsFaulted)
            {
                _log.LogWarning("Cannot invalidate dungeon geometry cache: " + task.Exception.GetBaseException());
            }

            _invalidations.RemoveAt(i);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _restore.Cancel();
        _validation.Cancel();
        _capture.Dispose();
        _memory.Clear();
        foreach (var task in _invalidations)
        {
            task.ContinueWith(failed => { _ = failed.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        _invalidations.Clear();
    }
}
