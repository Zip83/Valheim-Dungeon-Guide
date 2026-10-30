using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    private float _scanStarted;
    private float _nextProgress;
    private bool _preparingPreview;

    /// <summary>Raised after a completed preview or full scan replaces the active session's geometry.</summary>
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
        _scanStarted = Time.realtimeSinceStartup;
        _nextProgress = _scanStarted;
        var preview = ScanPreviewArea.Create(session, _settings.RevealAll.Value);
        _scanJob = _host.StartCoroutine(Guard(BuildScan(session, preview), session));
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
        _preparingPreview = preview;
        ScanResult result = null;
        foreach (var pause in CoroutinePhase.Forward(SampleGeometry(area, completed => result = completed)))
        {
            yield return pause;
        }

        if (Session != session || (preview && result.Graph.Nodes.Count == 0))
        {
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

    private void PublishScan(DungeonSession session, ScanResult result, FloorLayout layout, bool preview)
    {
        // Each published graph is complete and no longer owned by a sampling or analysis job.
        session.Scan = result;
        session.Layout = layout;
        session.Floors = layout.Levels;
        session.GeometryRevision++;
        session.HasPartialGeometry = preview;
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

    private IEnumerator SampleGeometry(DungeonArea area, Action<ScanResult> completed)
    {
        return _scanner.Scan(
            area,
            Mathf.Clamp(_settings.Spacing.Value, 0.75f, 3),
            Mathf.Clamp(_settings.Columns.Value, 1000, 120000),
            Mathf.Clamp(_settings.Budget.Value, 1, 8192),
            Mathf.Clamp(_settings.MillisecondsPerFrame.Value, 0.25f, 4),
            completed);
    }

    private IEnumerator AnalyzeFloors(DungeonSession session, ScanResult result, Action<FloorLayout> completed)
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
                    completed(layout);
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
