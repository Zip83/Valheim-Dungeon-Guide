using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Searches indexed landing geometry between ordinary reachable components without editing the graph.</summary>
internal sealed class GrappleRouteProbe
{
    private readonly GeometryPhysics _physics;
    private readonly GrappleLandingProbe _probe;
    private int _launch = -1;
    private int _landing = -1;

    /// <summary>Ordinary approach, one checked hook section, and ordinary departure.</summary>
    public List<int> Route { get; private set; } = new();

    /// <summary>Static surface accepted by the native hook attachment policy.</summary>
    public Vector3 Anchor
    {
        get; private set;
    }

    /// <summary>Shares collision policy and the existing per-frame physics allowance.</summary>
    public GrappleRouteProbe(GeometryPhysics physics)
    {
        _physics = physics;
        _probe = new GrappleLandingProbe(physics);
    }

    /// <summary>Identifies the forward crossing without treating its reverse as traversable.</summary>
    public bool IsPassage(int from, int to) => Route.Count > 0 && from == _launch && to == _landing;

    /// <summary>Rejects cached approach/departure passages that have been removed or lost traversal permission.</summary>
    public bool OrdinarySegmentsValid(NavGraph graph, RoutePolicy policy)
    {
        for (var i = 1; i < Route.Count; i++)
        {
            var from = Route[i - 1];
            var to = Route[i];
            if (!IsPassage(from, to) && !graph.Nodes[from].Edges.Exists(edge => edge.To == to && policy.Allows(edge)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Rechecks the selected crossing against doors, destroyed surfaces, and current landing clearance.</summary>
    public IEnumerable<object> Recheck(ScanResult scan, GrappleEquipment hook)
    {
        Vector3? anchor = null;
        foreach (var pause in _probe.Check(UnityPoints.V(scan.Graph.Nodes[_launch].Position),
            UnityPoints.V(scan.Graph.Nodes[_landing].Position), hook, result => anchor = result))
        {
            yield return pause;
        }

        if (anchor.HasValue)
        {
            Anchor = anchor.Value;
        }
        else
        {
            Route = new List<int>();
        }
    }

    /// <summary>Builds ordinary approach/departure segments and checks nearby supported hook crossings.</summary>
    public IEnumerable<object> Find(ScanResult scan, int start, Predicate<int> target, RoutePolicy policy,
        GrappleEquipment hook)
    {
        var topology = new GrappleRouteTopology();
        foreach (var pause in topology.Build(scan.Graph, start, target, policy, _physics.Work))
        {
            yield return pause;
        }

        if (!topology.HasLandings)
        {
            yield break;
        }

        foreach (var pause in FindCrossing(scan, topology, hook))
        {
            yield return pause;
        }
    }

    private IEnumerable<object> FindCrossing(ScanResult scan, GrappleRouteTopology topology, GrappleEquipment hook)
    {
        foreach (var source in topology.Sources)
        {
            foreach (var pause in SearchLandings(scan, topology, source, hook))
            {
                yield return pause;
            }

            if (Route.Count > 0)
            {
                yield break;
            }
        }
    }

    private IEnumerable<object> SearchLandings(ScanResult scan, GrappleRouteTopology topology, int source,
        GrappleEquipment hook)
    {
        var from = UnityPoints.V(scan.Graph.Nodes[source].Position);
        foreach (var column in GrappleLandingColumns.Nearby(from, hook.Range, scan.Spacing))
        {
            foreach (var pause in SearchColumn(scan, topology, source, column, hook))
            {
                yield return pause;
            }

            if (Route.Count > 0)
            {
                yield break;
            }
        }
    }

    private IEnumerable<object> SearchColumn(ScanResult scan, GrappleRouteTopology topology, int source,
        (int, int) column, GrappleEquipment hook)
    {
        while (!_physics.Work.TryStartWork())
        {
            yield return null;
            _physics.Work.BeginFrame();
        }

        if (!scan.Columns.TryGetValue(column, out var nodes))
        {
            yield break;
        }

        foreach (var destination in nodes)
        {
            if (!topology.IsLanding(destination))
            {
                continue;
            }

            foreach (var pause in CheckLanding(scan, topology, source, destination, hook))
            {
                yield return pause;
            }

            if (Route.Count > 0)
            {
                yield break;
            }
        }
    }

    private IEnumerable<object> CheckLanding(ScanResult scan, GrappleRouteTopology topology, int source,
        int destination, GrappleEquipment hook)
    {
        Vector3? anchor = null;
        foreach (var pause in _probe.Check(UnityPoints.V(scan.Graph.Nodes[source].Position),
            UnityPoints.V(scan.Graph.Nodes[destination].Position), hook, result => anchor = result))
        {
            yield return pause;
        }

        if (anchor.HasValue)
        {
            _launch = source;
            _landing = destination;
            Anchor = anchor.Value;
            Route = topology.Join(source, destination);
        }
    }
}
