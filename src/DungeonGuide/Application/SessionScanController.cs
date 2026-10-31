using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns one cancellable scan pipeline and publishes only completed geometry on the game thread.</summary>
internal sealed class SessionScanController
{
    private readonly MonoBehaviour _host;
    private readonly GuideSettings _settings;
    private readonly GeometryScanner _scanner;
    private readonly FloorDetector _floors;
    private readonly ManualLogSource _log;
    private readonly Func<DungeonSession> _current;
    private readonly Func<DungeonSession, byte[], CancellationToken, Task<bool>> _persist;
    private CancellationTokenSource _cacheWrite;
    private int _sampleVersion;
    private Coroutine _scanJob;
    private FloorAnalysisJob _floorAnalysis;
    private float _scanStarted;
    private float _nextProgress;
    private bool _preparingPreview;

    /// <summary>Raised after a completed preview or full scan replaces the active session's geometry.</summary>
    public event Action ScanCompleted;

    /// <summary>Signals that a full replacement starts sampling the latest world state.</summary>
    public event Action FullScanStarted;

    /// <summary>Connects sampling, floor analysis, and the active-session publication guard.</summary>
    /// <param name="host">Game-thread coroutine owner.</param>
    /// <param name="settings">Sampling resolution and per-frame work limits.</param>
    /// <param name="scanner">Cooperative Unity geometry scanner.</param>
    /// <param name="floors">Pure floor-analysis policy.</param>
    /// <param name="log">Sink for scan completion and failure diagnostics.</param>
    /// <param name="current">Returns the session currently owned by the runtime.</param>
    /// <param name="persist">Queues sealed optional cache bytes for publication without blocking the game thread.</param>
    public SessionScanController(MonoBehaviour host, GuideSettings settings, GeometryScanner scanner,
        FloorDetector floors, ManualLogSource log, Func<DungeonSession> current,
        Func<DungeonSession, byte[], CancellationToken, Task<bool>> persist)
    {
        _host = host;
        _settings = settings;
        _scanner = scanner;
        _floors = floors;
        _log = log;
        _current = current;
        _persist = persist;
    }

    private DungeonSession Session => _current();

    /// <summary>Starts a replacement scan while retaining the previously published map.</summary>
    /// <param name="session">Active session, or null when no scan can be started.</param>
    public void Start(DungeonSession session)
    {
        if (session == null || session.Scanning)
        {
            return;
        }

        foreach (var room in session.Area.Rooms)
        {
            _log.LogInfo("Dungeon room bounds: size=" + room.size + ", center=" + room.center);
        }

        _cacheWrite?.Dispose();
        _cacheWrite = new CancellationTokenSource();
        session.Scanning = true;
        _scanStarted = Time.realtimeSinceStartup;
        _nextProgress = _scanStarted;
        var preview = ScanPreviewArea.Create(session);
        _scanJob = _host.StartCoroutine(Guard(BuildScan(session, preview), session));
    }

    /// <summary>Cancels private floor analysis before the runtime saves and closes its session.</summary>
    public void CancelAnalysis()
    {
        InvalidateCacheWrite();
        _cacheWrite?.Dispose();
        _cacheWrite = null;
        _scanner.CancelAnalysis();
        _floorAnalysis?.Dispose();
        _floorAnalysis = null;
    }

    /// <summary>Revokes cache publication if geometry changed after this private scan had already sampled it.</summary>
    public void InvalidateCacheWrite() => _cacheWrite?.Cancel();

    /// <summary>Stops the sampling coroutine after the runtime has saved the active session.</summary>
    public void Stop()
    {
        if (_scanJob != null)
        {
            _host.StopCoroutine(_scanJob);
        }

        _scanJob = null;
    }

    private IEnumerator BuildScan(DungeonSession session, DungeonArea preview)
    {
        if (preview != null)
        {
            foreach (var pause in CoroutinePhase.Forward(BuildPass(session, preview, true)))
            {
                yield return pause;
            }
        }

        if (Session != session)
        {
            yield break;
        }

        foreach (var pause in CoroutinePhase.Forward(BuildPass(session, session.Area, false)))
        {
            yield return pause;
        }
    }

    private IEnumerator BuildPass(DungeonSession session, DungeonArea area, bool preview)
    {
        BeginPass(session, preview);
        ScanResult result = null;
        foreach (var pause in CoroutinePhase.Forward(SampleGeometry(area, completed => result = completed, preview)))
        {
            yield return pause;
        }

        if (Session != session || (preview && result.Graph.Nodes.Count == 0))
        {
            yield break;
        }

        if (preview)
        {
            PublishPreview(session, result);
            yield break;
        }

        FloorLayout layout = null;
        foreach (var pause in CoroutinePhase.Forward(AnalyzeFloors(session, result, completed => layout = completed)))
        {
            yield return pause;
        }

        if (Session == session && layout != null)
        {
            PublishScan(session, result, layout, preview);
        }
    }

    private void BeginPass(DungeonSession session, bool preview)
    {
        if (!preview)
        {
            _cacheWrite?.Dispose();
            _cacheWrite = new CancellationTokenSource();
            _sampleVersion = session.GeometryMutationVersion;
            FullScanStarted?.Invoke();
        }

        _preparingPreview = preview;
    }

    private void PublishPreview(DungeonSession session, ScanResult result)
    {
        var player = Player.m_localPlayer;
        var layout = PreviewFloorClassifier.Classify(result.Graph,
            player ? UnityPoints.P(player.transform.position) : session.Data.start);
        if (layout.Levels.Count > 0)
        {
            PublishScan(session, result, layout, true);
        }
    }

    private void PublishScan(DungeonSession session, ScanResult result, FloorLayout layout, bool preview)
    {
        // Each published snapshot is exclusively owned and no longer changed by a scan or worker.
        session.OverviewExploration.Load(Array.Empty<string>());
        session.VisitedRoomCoverage.Load(Array.Empty<string>());
        session.Scan = result;
        session.Layout = layout;
        session.Floors = layout.Levels;
        session.GeometryRevision++;
        session.HasPartialGeometry = preview;
        session.GeometryDirty = !preview && _sampleVersion != session.GeometryMutationVersion;
        session.GeometryNeedsScan = preview || session.GeometryDirty;
        session.NavigationReady = !preview;
        session.Scanning = preview;
        _log.LogInfo((preview ? "Scan preview ready: " : "Scan complete: ") + session.Area.Id
            + ", nodes=" + result.Graph.Nodes.Count + ", floors=" + session.Floors.Count
            + ", incomplete=" + result.Truncated
            + ", elapsedSeconds=" + (Time.realtimeSinceStartup - _scanStarted).ToString("F1", CultureInfo.InvariantCulture));
        _log.LogInfo("Floor navigation landings=" + layout.LandingNodes.Count);
        ScanCompleted?.Invoke();
    }

    private IEnumerator Guard(IEnumerator task, DungeonSession session)
    {
        try
        {
            while (MoveNextSafely(task))
            {
                ReportProgress();
                yield return task.Current;
            }
        }
        finally
        {
            (task as IDisposable)?.Dispose();
            if (Session == session)
            {
                session.Scanning = false;
                _scanJob = null;
            }
        }
    }

    private IEnumerator SampleGeometry(DungeonArea area, Action<ScanResult> completed, bool preview)
    {
        return _scanner.Scan(
            area,
            Mathf.Clamp(_settings.Spacing.Value, 0.75f, 3),
            Mathf.Clamp(_settings.Columns.Value, 1000, 120000),
            Mathf.Clamp(_settings.Budget.Value, 1, 8192),
            Mathf.Clamp(_settings.MillisecondsPerFrame.Value, 0.25f, 4),
            completed, preview);
    }

    private IEnumerator AnalyzeFloors(DungeonSession session, ScanResult result, Action<FloorLayout> completed)
    {
        // The new graph is private to this scan. Existing maps/navigation retain their previous graph.
        var detector = _floors;
        var preparation = new GeometryCachePreparation(session.GeometryCacheKey, result.Spacing, result.Truncated,
            session.Area.ObservedEntrance ? session.Area.Entrance : null);
        using (var analysis = new FloorAnalysisJob(token => preparation.Analyze(detector, result.Graph, token)))
        {
            _floorAnalysis = analysis;
            try
            {
                FloorLayout layout = null;
                foreach (var pause in CoroutinePhase.Forward(AwaitAnalysis(analysis, session, value => layout = value)))
                {
                    yield return pause;
                }

                if (Session == session && layout != null)
                {
                    foreach (var pause in CoroutinePhase.Forward(PersistPrepared(session, preparation)))
                    {
                        yield return pause;
                    }

                    if (Session == session)
                    {
                        completed(layout);
                    }
                }
            }
            finally
            {
                if (_floorAnalysis == analysis)
                {
                    _floorAnalysis = null;
                }
            }
        }
    }

    private IEnumerator AwaitAnalysis(FloorAnalysisJob analysis, DungeonSession session, Action<FloorLayout> completed)
    {
        FloorLayout layout;
        while (!analysis.TryComplete(out layout))
        {
            yield return null;
            if (Session != session)
            {
                yield break;
            }
        }

        completed(layout);
    }

    private IEnumerator PersistPrepared(DungeonSession session, GeometryCachePreparation preparation)
    {
        if (preparation.Error != null)
        {
            _log.LogWarning("Cannot prepare persistent geometry; keeping the live map: " + preparation.Error);
            yield break;
        }

        var token = _cacheWrite?.Token ?? CancellationToken.None;
        if (preparation.Payload == null || token.IsCancellationRequested)
        {
            yield break;
        }

        var task = _persist(session, preparation.Payload, token);
        task.ContinueWith(failed => { _ = failed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        while (!task.IsCompleted)
        {
            yield return null;
            if (Session != session)
            {
                yield break;
            }
        }

        if (task.IsFaulted && !(task.Exception.GetBaseException() is OperationCanceledException))
        {
            _log.LogWarning("Cannot persist completed geometry; keeping the live map: " + task.Exception.GetBaseException());
        }
        else if (!task.IsFaulted && !task.IsCanceled && !task.Result)
        {
            _log.LogWarning("Preserving a newer geometry cache; the live map remains available.");
        }
    }

    private bool MoveNextSafely(IEnumerator task)
    {
        try
        {
            _scanner.BeginSlice();
            return task.MoveNext();
        }
        catch (Exception e)
        {
            _log.LogError("Geometry scan failed: " + e);
            return false;
        }
        finally
        {
            _scanner.EndSlice();
        }
    }

    private void ReportProgress()
    {
        var now = Time.realtimeSinceStartup;
        if (now < _nextProgress)
        {
            return;
        }

        _nextProgress = now + 5;
        var phase = _floorAnalysis == null ? _scanner.Phase.ToString() : nameof(FloorAnalysisJob);
        _log.LogInfo("Scan progress: preview=" + _preparingPreview + ", phase=" + phase
            + ", columns=" + _scanner.ProcessedColumns + ", nodes=" + _scanner.SampledNodes
            + ", steps=" + _scanner.WorkSteps
            + ", elapsedSeconds=" + (now - _scanStarted).ToString("F1", CultureInfo.InvariantCulture));
    }
}
