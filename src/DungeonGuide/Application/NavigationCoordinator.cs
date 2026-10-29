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
    private float _destinationHeight;

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
    public NavigationCoordinator(RoutePlanner planner, GraphLocator locator, FloorNavigationPolicy floorPolicy)
    {
        _planner = planner;
        _locator = locator;
        _floorPolicy = floorPolicy;
    }

    /// <summary>
    /// Clears the requested destination and current route.
    /// </summary>
    public void Stop()
    {
        Goal = NavigationGoal.None;
        Chest = null;
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
    }

    /// <summary>
    /// Selects a chest record as the persistent navigation goal.
    /// </summary>
    public void ToChest(ChestRecord chest)
    {
        InvalidateRoute();
        Goal = NavigationGoal.Chest;
        Chest = chest;
    }

    /// <summary>
    /// Requests the known entrance, with a labeled scan-origin fallback when unavailable.
    /// </summary>
    public void Entrance()
    {
        InvalidateRoute();
        Goal = NavigationGoal.Entrance;
        Chest = null;
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
    }

    /// <summary>
    /// Clears cached route geometry while retaining the selected goal.
    /// </summary>
    public void InvalidateRoute()
    {
        Route.Clear();
        Status = NavigationStatus.None;
    }

    /// <summary>
    /// Resolves the active goal against current geometry and route policy without silently changing destination
    /// elevation.
    /// </summary>
    public void Recalculate(DungeonSession session, MapViewState view, Vector3 player)
    {
        Route.Clear();
        Status = NavigationStatus.None;
        if (Goal == NavigationGoal.None || session.Scan == null)
        {
            return;
        }

        var scan = session.Scan;
        var start = _locator.Snap(scan, player);
        if (start < 0)
        {
            Status = NavigationStatus.OffGraph;
            return;
        }

        if (Goal == NavigationGoal.Floor)
        {
            DestinationFloor = _floorPolicy.Resolve(session.Layout, _destinationHeight);
            if (DestinationFloor < 0)
            {
                Status = NavigationStatus.NoRoute;
                return;
            }

            // Being assigned to a floor by nearest height is insufficient: stairs and
            // balconies can share that label long before the real landing is reached.
            if (_floorPolicy.IsDestination(scan.Graph, session.Layout, start, DestinationFloor)
                && System.Math.Abs(player.y - _destinationHeight) <= 0.75f
                && Player.m_localPlayer.IsOnGround())
            {
                Stop();
                return;
            }
        }

        var destination = -1;
        if (Goal == NavigationGoal.Chest)
        {
            destination = _locator.Snap(scan, UnityPoints.V(Chest.position), 4.5f);
        }

        if (Goal == NavigationGoal.Entrance)
        {
            destination = _locator.Snap(scan, UnityPoints.V(session.Area.Entrance ?? session.Data.start), 4.5f);
        }

        var policy = new RoutePolicy
        {
            Breakables = view.AllowBreakables,
            Jumps = view.AllowJumps
        };
        Route = _planner.Find(
            scan.Graph,
            start,
            n => Goal == NavigationGoal.Explore
                ? !session.Exploration.Has(scan.Graph.Nodes[n])
                : Goal == NavigationGoal.Floor
                ? _floorPolicy.IsDestination(scan.Graph, session.Layout, n, DestinationFloor)
                : n == destination,
            policy);
        if (Route.Count == 0)
        {
            Status = Goal == NavigationGoal.Explore ? NavigationStatus.ExplorationExhausted : NavigationStatus.NoRoute;
        }
    }
}
