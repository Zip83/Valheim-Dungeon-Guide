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
    private readonly DungeonGeometrySessions _geometry;
    private float _nextDetect;
    private bool _entrancePending;
    private readonly RoomVisitRecorder _rooms = new();
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

    /// <summary>Signals actual world/connection changes so retained presentation resources can be released.</summary>
    public event Action WorldChanged;

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
        UserNotice notice,
        DungeonGeometrySessions geometry)
    {
        _settings = settings;
        _locator = locator;
        _activation = activation;
        _lifecycle = lifecycle;
        _exploration = exploration;
        _traversals = traversals;
        _geometry = geometry;
        _geometry.Restored += () => ScanCompleted?.Invoke();
        _geometry.PermissionsChanged += () => ScanCompleted?.Invoke();
        _geometry.WorldChanged += () => WorldChanged?.Invoke();
        _geometry.NeedsScan += ScanMissing;
        _scans = new SessionScanController(host, settings, scanner, floors, log, () => Session, _geometry.StorePrepared);
        _scans.ScanCompleted += GeometryPublished;
        _scans.FullScanStarted += BeginFullScan;
    }

    private string World => ZNet.instance ? ZNet.instance.GetWorldUID().ToString(CultureInfo.InvariantCulture) : null;

    /// <summary>
    /// Detects dungeon transitions and schedules exploration, traversal observation, and periodic saves.
    /// </summary>
    public void Tick()
    {
        var player = Player.m_localPlayer;
        var world = World;
        _geometry.Tick(Session, world);
        if (!player || world == null)
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

        _rooms.Track(Session, player.transform.position);
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

    /// <summary>Refreshes door permissions without rebuilding unchanged floor geometry.</summary>
    public void DoorChanged()
    {
        if (Session != null)
        {
            _geometry.RefreshPermissions(Session);
        }
    }

    /// <summary>Debounces automatic replacement after a change to actual sampled geometry.</summary>
    public void GeometryChanged()
    {
        if (Session != null)
        {
            _scans.InvalidateCacheWrite();
            _geometry.Invalidate(Session, true);
            _doorRescan = Time.unscaledTime + 1;
        }
    }

    private void StartScan()
    {
        if (Session == null || Session.Scanning)
        {
            return;
        }

        if (_geometry.RefreshArea(Session, Session.Area))
        {
            _geometry.Open(Session);
            return;
        }

        if (!Session.GeometryNeedsScan)
        {
            _geometry.Invalidate(Session);
        }

        _scans.Start(Session);
    }

    private void BeginFullScan()
    {
        _geometry.RefreshArea(Session, Session.Area);
        _doorRescan = float.PositiveInfinity;
    }

    private void ScanMissing(DungeonSession session)
    {
        if (Session == session)
        {
            StartScan();
        }
    }

    private void GeometryPublished()
    {
        if (!Session.HasPartialGeometry && !float.IsPositiveInfinity(_doorRescan))
        {
            Session.GeometryNeedsScan = true;
        }

        _geometry.Published(Session);
        ScanCompleted?.Invoke();
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
        Session.Scanning = false;
        _geometry.Leave(Session);

        Session = null;
        _traversals.Reset();
        _rooms.Reset();
        _exploration.Reset();
        _doorRescan = float.PositiveInfinity;
        Left?.Invoke();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Close();
        _geometry.Dispose();
    }

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

    /// <summary>Starts short discovery retries while the native doorway transfer loads its dungeon rooms.</summary>
    public void EntranceObserved()
    {
        _entrancePending = true;
        _nextDetect = 0;
    }

    private void DetectIfDue()
    {
        if (Time.unscaledTime >= _nextDetect)
        {
            _nextDetect = Time.unscaledTime + (_entrancePending ? 0.25f : 3);
            Detect(false);
            if (Session != null)
            {
                _entrancePending = false;
            }
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
            _exploration.Track(Session.Scan, Session.Exploration, player.transform.position, player.GetEyePoint(),
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
            if (_geometry.RefreshArea(Session, area))
            {
                _scans.CancelAnalysis();
                _scans.Stop();
                Session.Scanning = false;
                _geometry.Open(Session);
            }
            else if (manual)
            {
                StartScan();
            }

            return;
        }

        Close();
        Session = _geometry.Resume(area, World);
        if (Session != null)
        {
            Entered?.Invoke();
            ScanCompleted?.Invoke();
            if (Session.GeometryNeedsScan)
            {
                _geometry.Open(Session);
            }

            return;
        }

        Session = _lifecycle.Open(area, World, UnityPoints.P(player.transform.position));
        Entered?.Invoke();
        _geometry.Open(Session);
    }
}
