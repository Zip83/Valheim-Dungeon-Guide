using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Maintains the selected goal and recalculates routes without silently changing requested floor elevation.
/// </summary>
internal sealed class NavigationCoordinator
{
    private readonly RoutePlanner _planner;
    private readonly GraphLocator _locator;
    private readonly FloorNavigationPolicy _floorPolicy;
    private readonly GrappleFallbackSearch _grapple;
    private float _destinationHeight;

    /// <summary>Selected personal location when return-location navigation is active.</summary>
    public WaypointRecord Waypoint
    {
        get; private set;
    }

    /// <summary>
    /// Kind of navigation destination currently requested by the player.
    /// </summary>
    public NavigationGoal Goal
    {
        get; private set;
    }

    /// <summary>
    /// Latest route outcome, including off-graph and inaccessible destinations.
    /// </summary>
    public NavigationStatus Status
    {
        get; private set;
    }

    /// <summary>
    /// Selected chest destination when chest navigation is active.
    /// </summary>
    public ChestRecord Chest
    {
        get; private set;
    }

    /// <summary>
    /// Current floor index resolved for the remembered physical elevation.
    /// </summary>
    public int DestinationFloor
    {
        get; private set;
    }

    /// <summary>
    /// Ordered graph-node indices for the latest validated route.
    /// </summary>
    public List<int> Route { get; private set; } = new List<int>();

    /// <summary>
    /// Connects graph routing, position snapping, and physical-floor destination policy.
    /// </summary>
    public NavigationCoordinator(RoutePlanner planner, GraphLocator locator, FloorNavigationPolicy floorPolicy,
        GrappleFallbackSearch grapple)
    {
        _planner = planner;
        _locator = locator;
        _floorPolicy = floorPolicy;
        _grapple = grapple;
    }

    /// <summary>
    /// Clears the requested destination and current route.
    /// </summary>
    public void Stop()
    {
        _grapple.Reset();
        Goal = NavigationGoal.None;
        Chest = null;
        Waypoint = null;
        Route.Clear();
        Status = NavigationStatus.None;
    }

    /// <summary>
    /// Requests a reachable unexplored floor sample.
    /// </summary>
    public void Explore()
    {
        InvalidateRoute();
        Goal = NavigationGoal.Explore;
        Chest = null;
        Waypoint = null;
    }

    /// <summary>
    /// Selects a chest record as the persistent navigation goal.
    /// </summary>
    public void ToChest(ChestRecord chest)
    {
        InvalidateRoute();
        Goal = NavigationGoal.Chest;
        Chest = chest;
        Waypoint = null;
    }

    /// <summary>Requests a saved physical location, retaining it while navigation is preparing.</summary>
    public void ToWaypoint(WaypointRecord waypoint)
    {
        InvalidateRoute();
        Goal = NavigationGoal.Waypoint;
        Chest = null;
        Waypoint = waypoint;
    }

    /// <summary>
    /// Requests the known entrance, with a labeled scan-origin fallback when unavailable.
    /// </summary>
    public void Entrance()
    {
        InvalidateRoute();
        Goal = NavigationGoal.Entrance;
        Chest = null;
        Waypoint = null;
    }

    /// <summary>
    /// Remembers an architectural floor and its physical elevation for higher/lower navigation.
    /// </summary>
    public void Floor(int floor, float height)
    {
        InvalidateRoute();
        Goal = NavigationGoal.Floor;
        DestinationFloor = floor;
        _destinationHeight = height;
        Chest = null;
        Waypoint = null;
    }

    /// <summary>
    /// Clears cached route geometry while retaining the selected goal.
    /// </summary>
    public void InvalidateRoute()
    {
        _grapple.Reset();
        Route.Clear();
        Status = NavigationStatus.None;
    }

    /// <summary>
    /// Resolves the active goal against current geometry and route policy without silently changing destination
    /// elevation.
    /// </summary>
    public void Recalculate(DungeonSession session, Vector3 player)
    {
        Route = new List<int>();
        Status = NavigationStatus.None;
        if (Goal == NavigationGoal.None || session.Scan == null || !session.NavigationReady)
        {
            return;
        }

        var start = _locator.Snap(session.Scan, player);
        if (start < 0)
        {
            Status = NavigationStatus.OffGraph;
            return;
        }

        if (PrepareFloorDestination(session, start, player))
        {
            FindRoute(session, start);
        }
    }

    private bool PrepareFloorDestination(DungeonSession session, int start, Vector3 player)
    {
        if (Goal != NavigationGoal.Floor)
        {
            return true;
        }

        DestinationFloor = _floorPolicy.Resolve(session.Layout, _destinationHeight);
        if (DestinationFloor < 0)
        {
            Status = NavigationStatus.NoRoute;
            return false;
        }

        // Being assigned to a floor by nearest height is insufficient: stairs and
        // balconies can share that label long before the real landing is reached.
        if (_floorPolicy.IsDestination(session.Scan.Graph, session.Layout, start, DestinationFloor)
            && System.Math.Abs(player.y - _destinationHeight) <= 0.75f
            && Player.m_localPlayer.IsOnGround())
        {
            Stop();
            return false;
        }

        return true;
    }

    private int ResolvePointDestination(DungeonSession session)
    {
        var destination = -1;
        if (Goal == NavigationGoal.Chest)
        {
            destination = _locator.Snap(session.Scan, UnityPoints.V(Chest.position), 4.5f);
        }

        if (Goal == NavigationGoal.Waypoint)
        {
            var floor = FloorDetector.Nearest(session.Floors, Waypoint.position.y);
            destination = _locator.Snap(session.Scan, UnityPoints.V(Waypoint.position), 4.5f, floor);
        }

        if (Goal == NavigationGoal.Entrance)
        {
            destination = _locator.Snap(session.Scan, UnityPoints.V(session.Area.Entrance ?? session.Data.start), 4.5f);
        }

        return destination;
    }

    private void FindRoute(DungeonSession session, int start)
    {
        var scan = session.Scan;
        var destination = ResolvePointDestination(session);
        var policy = new RoutePolicy();
        Route = _planner.Find(
            scan.Graph,
            start,
            n => IsDestination(session, n, destination),
            policy);
        if (Route.Count > 0)
        {
            _grapple.Reset();
            return;
        }

        _grapple.Request(scan, start, n => IsDestination(session, n, destination), policy, Player.m_localPlayer);
        Route = _grapple.Route;
        if (Route.Count == 0)
        {
            Status = Goal == NavigationGoal.Explore ? NavigationStatus.ExplorationExhausted : NavigationStatus.NoRoute;
        }
    }

    /// <summary>Whether an asynchronous hook search is still checking supported landings.</summary>
    public bool SearchingGrapple => _grapple.Pending;

    /// <summary>Whether the active route explicitly requires the hook fallback.</summary>
    public bool RequiresGrapple => _grapple.Route.Count > 0 && Route == _grapple.Route;

    /// <summary>Checked attachment surface for the active hook section.</summary>
    public Vector3 GrappleAnchor => _grapple.Anchor;

    /// <summary>Distinguishes the directed hook section from ordinary approach and departure segments.</summary>
    public bool IsGrapplePassage(int from, int to) => RequiresGrapple && _grapple.IsPassage(from, to);

    /// <summary>Advances fallback physics without waiting for the once-per-second ordinary route refresh.</summary>
    public void AdvanceFallback(DungeonSession session, Player player)
    {
        var wasGrapple = RequiresGrapple;
        _grapple.Advance(session, player);
        if (wasGrapple && _grapple.Route.Count == 0)
        {
            Route = new List<int>();
            Status = NavigationStatus.NoRoute;
        }

        if (_grapple.Route.Count > 0)
        {
            Route = _grapple.Route;
            Status = NavigationStatus.None;
        }
    }

    private bool IsDestination(DungeonSession session, int node, int pointDestination)
    {
        switch (Goal)
        {
            case NavigationGoal.Explore:
                return !session.Exploration.Has(session.Scan.Graph.Nodes[node]);
            case NavigationGoal.Floor:
                return _floorPolicy.IsDestination(session.Scan.Graph, session.Layout, node, DestinationFloor);
            default:
                return node == pointDestination;
        }
    }
}
