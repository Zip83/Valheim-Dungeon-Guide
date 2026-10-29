using System;
using System.Collections;
using System.Globalization;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Coordinates dungeon detection, incremental scan execution, exploration recording, and session lifetime.
/// </summary>
internal sealed class GuideRuntime : IDisposable
{
    private readonly MonoBehaviour _host;
    private readonly GuideSettings _settings;
    private readonly DungeonLocator _locator;
    private readonly GeometryScanner _scanner;
    private readonly FloorDetector _floors;
    private readonly SessionLifecycle _lifecycle;
    private readonly ExplorationRecorder _exploration;
    private readonly TraversalRecorder _traversals;
    private readonly ManualLogSource _log;
    private readonly UserNotice _notice;
    private Coroutine _scanJob;
    private float _nextDetect;
    private float _nextTrack;
    private float _nextSave;
    private float _doorRescan = float.PositiveInfinity;

    /// <summary>
    /// Currently active dungeon session, or null when no usable dungeon is active.
    /// </summary>
    public DungeonSession Session
    {
        get; private set;
    }

    /// <summary>
    /// Raised after a dungeon session is opened.
    /// </summary>
    public event Action Entered;

    /// <summary>
    /// Raised when the active dungeon session is closed.
    /// </summary>
    public event Action Left;

    /// <summary>
    /// Raised when scanned geometry and floor assignments have been replaced.
    /// </summary>
    public event Action ScanCompleted;

    /// <summary>
    /// Connects session lifecycle, bounded scanning, floor detection, and exploration/traversal observation.
    /// </summary>
    public GuideRuntime(
        MonoBehaviour host,
        GuideSettings settings,
        DungeonLocator locator,
        GeometryScanner scanner,
        FloorDetector floors,
        SessionLifecycle lifecycle,
        ExplorationRecorder exploration,
        TraversalRecorder traversals,
        ManualLogSource log,
        UserNotice notice)
    {
        _host = host;
        _settings = settings;
        _locator = locator;
        _scanner = scanner;
        _floors = floors;
        _lifecycle = lifecycle;
        _exploration = exploration;
        _traversals = traversals;
        _log = log;
        _notice = notice;
    }

    private string World => ZNet.instance ? ZNet.instance.GetWorldUID().ToString(CultureInfo.InvariantCulture) : null;

    /// <summary>
    /// Detects dungeon transitions and schedules exploration, traversal observation, and periodic saves.
    /// </summary>
    public void Tick()
    {
        var player = Player.m_localPlayer;
        if (!player || World == null)
        {
            Close();
            return;
        }

        if (Session != null && Session.Data.world != World)
        {
            Close();
        }

        if (Time.unscaledTime >= _nextDetect)
        {
            _nextDetect = Time.unscaledTime + 3;
            Detect(false);
        }

        if (Session == null)
        {
            return;
        }

        if (Time.unscaledTime >= _doorRescan)
        {
            if (Session.Scanning)
            {
                _doorRescan = Time.unscaledTime + 0.5f;
            }
            else
            {
                _doorRescan = float.PositiveInfinity;
                Rescan();
            }
        }

        if (Session.Scan != null)
        {
            if (Time.unscaledTime >= _nextTrack)
            {
                _nextTrack = Time.unscaledTime + 0.35f;
                _exploration.Track(Session.Scan, Session.Exploration, player.transform.position);
            }

            // Recording takeoff/landing must run every frame; proof revalidation is throttled separately.
            _traversals.Tick(Session, player);
        }

        if (Time.unscaledTime >= _nextSave)
        {
            _nextSave = Time.unscaledTime + 15;
            _lifecycle.Save(Session);
        }
    }

    private void Detect(bool manual)
    {
        var p = Player.m_localPlayer;
        if (!p)
        {
            return;
        }

        var area = _locator.Locate(p.transform.position);
        if (area == null && manual)
        {
            area = _locator.Manual(
                p.transform.position,
                Mathf.Clamp(_settings.Radius.Value, 10, 150),
                Mathf.Clamp(_settings.Height.Value, 10, 300));
        }

        if (area == null)
        {
            if (Session != null && !Session.Area.Bounds.Contains(p.transform.position))
            {
                Close();
            }

            return;
        }

        if (Session?.Area.Id == area.Id)
        {
            if (manual)
            {
                StartScan();
            }

            return;
        }

        Close();
        Session = _lifecycle.Open(area, World, UnityPoints.P(p.transform.position));
        Entered?.Invoke();
        StartScan();
    }

    /// <summary>
    /// Schedules a replacement scan without assuming the player position is the dungeon entrance.
    /// </summary>
    public void Rescan()
    {
        if (Session == null)
        {
            Detect(true);
        }
        else
        {
            StartScan();
        }
    }

    /// <summary>
    /// Debounces a geometry rescan after a relevant door or destructible change.
    /// </summary>
    public void DoorChanged()
    {
        if (Session != null)
        {
            _doorRescan = Time.unscaledTime + 1;
        }
    }

    private void StartScan()
    {
        if (Session == null || Session.Scanning)
        {
            return;
        }

        var session = Session;
        session.Scanning = true;
        var task = _scanner.Scan(
            session.Area,
            Mathf.Clamp(_settings.Spacing.Value, 0.75f, 3),
            Mathf.Clamp(_settings.Columns.Value, 1000, 120000),
            Mathf.Clamp(_settings.Budget.Value, 1, 128),
            result =>
        {
            if (Session != session)
            {
                return;
            }

            session.Scan = result;
            session.Layout = _floors.Detect(result.Graph, result.Spacing);
            session.Floors = session.Layout.Levels;
            session.GeometryRevision++;
            session.Scanning = false;
            _log.LogInfo("Scan complete: " + session.Area.Id + ", nodes=" + result.Graph.Nodes.Count + ", floors="
                        + session.Floors.Count
                        + ", incomplete="
                        + result.Truncated);
            _log.LogInfo("Floor navigation landings=" + session.Layout.LandingNodes.Count);
            ScanCompleted?.Invoke();
        });
        _scanJob = _host.StartCoroutine(Guard(task, session));
    }

    private IEnumerator Guard(IEnumerator task, DungeonSession session)
    {
        while (true)
        {
            bool more;
            try
            {
                more = task.MoveNext();
            }
            catch (Exception e)
            {
                _log.LogError("Geometry scan failed: " + e);
                session.Scanning = false;
                yield break;
            }

            if (!more)
            {
                break;
            }

            yield return task.Current;
        }

        _scanJob = null;
    }

    private void Close()
    {
        if (Session == null)
        {
            return;
        }

        _lifecycle.Save(Session);
        if (_scanJob != null)
        {
            _host.StopCoroutine(_scanJob);
        }

        _scanJob = null;
        Session = null;
        _traversals.Reset();
        _doorRescan = float.PositiveInfinity;
        Left?.Invoke();
    }

    /// <inheritdoc/>
    public void Dispose() => Close();
}
