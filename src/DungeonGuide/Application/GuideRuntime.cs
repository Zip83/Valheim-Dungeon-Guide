using System;
using System.Collections;
using System.Globalization;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Owns dungeon session transitions and schedules observations while delegating scan execution.
/// </summary>
internal sealed class GuideRuntime : IDisposable
{
    private readonly GuideSettings _settings;
    private readonly SessionScanController _scans;
    private readonly DungeonLocator _locator;
    private readonly DungeonActivationPolicy _activation;
    private readonly SessionLifecycle _lifecycle;
    private readonly ExplorationRecorder _exploration;
    private readonly TraversalRecorder _traversals;
    private float _nextDetect;
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
        DungeonActivationPolicy activation,
        GeometryScanner scanner,
        FloorDetector floors,
        SessionLifecycle lifecycle,
        ExplorationRecorder exploration,
        TraversalRecorder traversals,
        ManualLogSource log,
        UserNotice notice)
    {
        _settings = settings;
        _locator = locator;
        _activation = activation;
        _lifecycle = lifecycle;
        _exploration = exploration;
        _traversals = traversals;
        _scans = new SessionScanController(host, settings, scanner, floors, log, () => Session);
        _scans.ScanCompleted += () => ScanCompleted?.Invoke();
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

        if (!RetainSession(player.transform.position))
        {
            return;
        }

        DetectIfDue();
        if (Session == null)
        {
            return;
        }

        RescanIfDue();
        TrackMovement(player);
        SaveIfDue();
    }

    private void Detect(bool manual)
    {
        var p = Player.m_localPlayer;
        if (!p)
        {
            return;
        }

        var area = LocateArea(p, manual);

        if (area == null)
        {
            if (Session != null && (!Session.Area.IsManual || !CanRetainSession(p.transform.position)))
            {
                Close();
            }

            return;
        }

        ActivateArea(area, p, manual);
    }

    /// <summary>
    /// Schedules a replacement scan without assuming the player position is the dungeon entrance.
    /// </summary>
    public void Rescan()
    {
        if (Session?.Scanning == true)
        {
            return;
        }

        var player = Player.m_localPlayer;
        if (Session != null && player && CanRetainSession(player.transform.position))
        {
            StartScan();
            return;
        }

        Detect(true);
    }

    private bool CanRetainSession(Vector3 position)
    {
        return _activation.CanRetainSession(
            Session.Area.IsManual,
            Session.Area.Bounds.Contains(position),
            Character.InInterior(position),
            _settings.AllowOutdoorMapping.Value);
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
        _scans.Start(Session);
    }

    private void Close()
    {
        if (Session == null)
        {
            return;
        }

        _scans.CancelAnalysis();
        _lifecycle.Save(Session);
        _scans.Stop();

        Session = null;
        _traversals.Reset();
        _exploration.Reset();
        _doorRescan = float.PositiveInfinity;
        Left?.Invoke();
    }

    /// <inheritdoc/>
    public void Dispose() => Close();

    private bool RetainSession(Vector3 position)
    {
        var inInterior = Character.InInterior(position);
        if (Session != null && (Session.Data.world != World || !CanRetainSession(position)))
        {
            Close();
        }

        if (!inInterior && Session == null)
        {
            return false;
        }

        return true;
    }

    private void DetectIfDue()
    {
        if (Time.unscaledTime >= _nextDetect)
        {
            _nextDetect = Time.unscaledTime + 3;
            Detect(false);
        }
    }

    private void RescanIfDue()
    {
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
    }

    private void TrackMovement(Player player)
    {
        if (Session.Scan != null)
        {
            _exploration.Track(Session.Scan, Session.Exploration, player.transform.position,
                Session.Floors.Count == 0 ? -1 : FloorDetector.Nearest(Session.Floors, player.transform.position.y));

            // Recording takeoff/landing must run every frame; proof revalidation is throttled separately.
            if (Session.NavigationReady)
            {
                _traversals.Tick(Session, player);
            }
        }
    }

    private void SaveIfDue()
    {
        if (Time.unscaledTime >= _nextSave)
        {
            _nextSave = Time.unscaledTime + 15;
            _lifecycle.Save(Session);
        }
    }

    private DungeonArea LocateArea(Player player, bool manual)
    {
        var area = _locator.Locate(player.transform.position);
        if (area == null && manual && _activation.CanScanManually(
            Character.InInterior(player.transform.position),
            _settings.AllowOutdoorMapping.Value))
        {
            area = _locator.Manual(
                player.transform.position,
                Mathf.Clamp(_settings.Radius.Value, 10, 150),
                Mathf.Clamp(_settings.Height.Value, 10, 300));
        }

        return area;
    }

    private void ActivateArea(DungeonArea area, Player player, bool manual)
    {
        if (Session?.Area.Id == area.Id)
        {
            if (manual)
            {
                StartScan();
            }

            return;
        }

        Close();
        Session = _lifecycle.Open(area, World, UnityPoints.P(player.transform.position));
        Entered?.Invoke();
        StartScan();
    }
}
