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
    private readonly SessionScanController _scans;
    private readonly DungeonLocator _locator;
    private readonly DungeonActivationPolicy _activation;
    private readonly SessionLifecycle _lifecycle;
    private readonly ExplorationRecorder _exploration;
    private readonly TraversalRecorder _traversals;
    private readonly DungeonGeometrySessions _geometry;
    private readonly DoorGeometryMonitor _doorGeometry;
    private float _nextDetect;
    private bool _entrancePending;
    private readonly RoomVisitRecorder _rooms = new();
    private readonly PassageEquipmentWatch _equipment = new();
    private float _nextEquipment;
    private float _nextSave;
    private float _geometryRefresh = float.PositiveInfinity;

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
        _locator = locator;
        _activation = activation;
        _lifecycle = lifecycle;
        _exploration = exploration;
        _traversals = traversals;
        _geometry = geometry;
        _doorGeometry = new DoorGeometryMonitor(GeometryChanged);
        _geometry.Restored += () => ScanCompleted?.Invoke();
        _geometry.PermissionsChanged += () => ScanCompleted?.Invoke();
        _geometry.WorldChanged += () => WorldChanged?.Invoke();
        _geometry.NeedsScan += ScanMissing;
        _scans = new SessionScanController(host, settings, scanner, floors, log, () => Session, _geometry.StorePrepared);
        _scans.ScanCompleted += GeometryPublished;
        _scans.FullScanStarted += BeginFullScan;
    }

    private string World => GameWorld.Id?.ToString(CultureInfo.InvariantCulture);

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
        RefreshEquipmentIfDue(player);
        _doorGeometry.Tick(Time.unscaledTime);
        RefreshGeometryIfDue();
        TrackMovement(player);
        SaveIfDue();
    }

    private void Detect()
    {
        var p = Player.m_localPlayer;
        if (!p)
        {
            return;
        }

        var area = _locator.Locate(p.transform.position);

        if (area == null)
        {
            if (Session != null && (!Session.Area.IsManual || !CanRetainSession(p.transform.position)))
            {
                Close();
            }

            return;
        }

        ActivateArea(area, p);
    }

    private bool CanRetainSession(Vector3 position)
    {
        return _activation.CanRetainSession(
            Session.Area.IsManual,
            Session.Area.Bounds.Contains(position),
            Character.InInterior(position));
    }

    /// <summary>Refreshes door permissions without rebuilding unchanged floor geometry.</summary>
    public void DoorChanged()
    {
        if (Session != null)
        {
            _geometry.RefreshPermissions(Session);
        }
    }

    /// <summary>Accepts native local or replicated door-state changes only within the active dungeon.</summary>
    public void DoorObserved(Door door)
    {
        if (Session != null && door && Session.Area.Bounds.Contains(door.transform.position))
        {
            DoorChanged();
            _doorGeometry.Observe(door, Time.unscaledTime);
        }
    }

    /// <summary>Debounces automatic replacement after a change to actual sampled geometry.</summary>
    public void GeometryChanged()
    {
        if (Session != null)
        {
            _scans.InvalidateCacheWrite();
            _geometry.Invalidate(Session, true);
            _geometryRefresh = Time.unscaledTime + 1;
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
        _geometryRefresh = float.PositiveInfinity;
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
        if (!Session.HasPartialGeometry && !float.IsPositiveInfinity(_geometryRefresh))
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
        _equipment.Reset();
        _exploration.Reset();
        _doorGeometry.Reset();
        _geometryRefresh = float.PositiveInfinity;
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
            Detect();
            if (Session != null)
            {
                _entrancePending = false;
            }
        }
    }

    private void RefreshGeometryIfDue()
    {
        if (Time.unscaledTime >= _geometryRefresh)
        {
            if (Session.Scanning)
            {
                _geometryRefresh = Time.unscaledTime + 0.5f;
            }
            else
            {
                _geometryRefresh = float.PositiveInfinity;
                StartScan();
            }
        }
    }

    private void TrackMovement(Player player)
    {
        if (Session.Scan != null)
        {
            // Capture real jumps even while the published preview is waiting for navigation.
            _traversals.Tick(Session, player);
            _exploration.Track(Session.Scan, Session.Exploration, player.transform.position, player.GetEyePoint(),
                Session.Floors.Count == 0 ? -1 : FloorDetector.Nearest(Session.Floors, player.transform.position.y));
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

    private void RefreshEquipmentIfDue(Player player)
    {
        if (Time.unscaledTime >= _nextEquipment)
        {
            _nextEquipment = Time.unscaledTime + 1;
            if (_equipment.Changed(player))
            {
                DoorChanged();
            }
        }
    }

    private void ActivateArea(DungeonArea area, Player player)
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
