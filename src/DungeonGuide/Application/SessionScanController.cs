using System;
using System.Collections;
using System.Collections.Generic;
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
    private Coroutine _scanJob;
    private FloorAnalysisJob _floorAnalysis;

    /// <summary>Raised after complete geometry and floor assignments replace the active session's scan.</summary>
    public event Action ScanCompleted;

    /// <summary>Connects sampling, floor analysis, and the active-session publication guard.</summary>
    /// <param name="host">Game-thread coroutine owner.</param>
    /// <param name="settings">Sampling resolution and per-frame work limits.</param>
    /// <param name="scanner">Cooperative Unity geometry scanner.</param>
    /// <param name="floors">Pure floor-analysis policy.</param>
    /// <param name="log">Sink for scan completion and failure diagnostics.</param>
    /// <param name="current">Returns the session currently owned by the runtime.</param>
    public SessionScanController(MonoBehaviour host, GuideSettings settings, GeometryScanner scanner,
        FloorDetector floors, ManualLogSource log, Func<DungeonSession> current)
    {
        _host = host;
        _settings = settings;
        _scanner = scanner;
        _floors = floors;
        _log = log;
        _current = current;
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

        session.Scanning = true;
        _scanJob = _host.StartCoroutine(Guard(BuildScan(session), session));
    }

    /// <summary>Cancels private floor analysis before the runtime saves and closes its session.</summary>
    public void CancelAnalysis()
    {
        _floorAnalysis?.Dispose();
        _floorAnalysis = null;
    }

    /// <summary>Stops the sampling coroutine after the runtime has saved the active session.</summary>
    public void Stop()
    {
        if (_scanJob != null)
        {
            _host.StopCoroutine(_scanJob);
        }

        _scanJob = null;
    }

    private IEnumerator BuildScan(DungeonSession session)
    {
        ScanResult result = null;
        foreach (var pause in CoroutinePhase.Forward(SampleGeometry(session, completed => result = completed)))
        {
            yield return pause;
        }

        foreach (var pause in CoroutinePhase.Forward(AnalyzeFloors(session, result)))
        {
            yield return pause;
        }
    }

    private void PublishScan(DungeonSession session, ScanResult result, FloorLayout layout)
    {
        // Publication and all game-facing callbacks stay on the game thread, after worker completion.
        session.Scan = result;
        session.Layout = layout;
        session.Floors = layout.Levels;
        session.GeometryRevision++;
        session.Scanning = false;
        _log.LogInfo("Scan complete: " + session.Area.Id + ", nodes=" + result.Graph.Nodes.Count
            + ", floors=" + session.Floors.Count + ", incomplete=" + result.Truncated);
        _log.LogInfo("Floor navigation landings=" + layout.LandingNodes.Count);
        ScanCompleted?.Invoke();
    }

    private IEnumerator Guard(IEnumerator task, DungeonSession session)
    {
        try
        {
            while (MoveNextSafely(task))
            {
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

    private IEnumerator SampleGeometry(DungeonSession session, Action<ScanResult> completed)
    {
        return _scanner.Scan(
            session.Area,
            Mathf.Clamp(_settings.Spacing.Value, 0.75f, 3),
            Mathf.Clamp(_settings.Columns.Value, 1000, 120000),
            Mathf.Clamp(_settings.Budget.Value, 1, 128),
            Mathf.Clamp(_settings.MillisecondsPerFrame.Value, 0.25f, 4),
            completed);
    }

    private IEnumerator AnalyzeFloors(DungeonSession session, ScanResult result)
    {
        // The new graph is private to this scan. Existing maps/navigation retain their previous graph.
        using (var analysis = new FloorAnalysisJob(_floors, result.Graph, result.Spacing))
        {
            _floorAnalysis = analysis;
            try
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

                if (Session == session)
                {
                    PublishScan(session, result, layout);
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

    private bool MoveNextSafely(IEnumerator task)
    {
        try
        {
            return task.MoveNext();
        }
        catch (Exception e)
        {
            _log.LogError("Geometry scan failed: " + e);
            return false;
        }
    }
}
