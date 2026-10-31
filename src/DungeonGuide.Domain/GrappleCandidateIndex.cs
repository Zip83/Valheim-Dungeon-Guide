using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>Indexes goal-reaching landings and prioritizes nearby launches without scheduling physics.</summary>
internal sealed class GrappleCandidateIndex
{
    private readonly SortedSet<(float Distance, int Node)> _sources = new();
    private Point _landingMin;
    private Point _landingMax;
    private Point _goalMin;
    private Point _goalMax;

    /// <summary>Landing-only columns exclude every sample that cannot reach the current goal.</summary>
    public Dictionary<(int, int), List<int>> Columns { get; } = new();

    /// <summary>Launches closest to the requested goal are checked before distant approach samples.</summary>
    public IEnumerable<int> Sources => _sources.Select(source => source.Node);

    /// <summary>Builds the request-owned spatial index and priority ordering under the shared work budget.</summary>
    public IEnumerable<object> Build(NavGraph graph, GrappleRouteTopology topology, float spacing,
        float range, RoutePolicy policy, FrameWorkBudget work)
    {
        _landingMin = _goalMin = new Point(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        _landingMax = _goalMax = new Point(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (var pause in ReadGoalBounds(graph, topology, work))
        {
            yield return pause;
        }

        foreach (var pause in IndexLandings(graph, topology, spacing, policy, work))
        {
            yield return pause;
        }

        foreach (var pause in RankLaunches(graph, topology, spacing, range, policy, work))
        {
            yield return pause;
        }
    }

    private IEnumerable<object> ReadGoalBounds(NavGraph graph, GrappleRouteTopology topology, FrameWorkBudget work)
    {
        foreach (var node in topology.Goals)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            Expand(graph.Nodes[node].Position, ref _goalMin, ref _goalMax);
        }
    }

    private IEnumerable<object> IndexLandings(NavGraph graph, GrappleRouteTopology topology, float spacing,
        RoutePolicy policy, FrameWorkBudget work)
    {
        Predicate<int> member = topology.IsLanding;
        foreach (var node in topology.Landings)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            if (GrappleLaunchBoundary.Contains(graph, node, spacing, policy, member))
            {
                IndexLanding(graph.Nodes[node].Position, node, spacing);
            }
        }
    }

    private IEnumerable<object> RankLaunches(NavGraph graph, GrappleRouteTopology topology, float spacing,
        float range, RoutePolicy policy, FrameWorkBudget work)
    {
        Predicate<int> member = topology.IsSource;
        foreach (var node in topology.Sources)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            var position = graph.Nodes[node].Position;
            if (InRange(position, range)
                && GrappleLaunchBoundary.Contains(graph, node, spacing, policy, member))
            {
                _sources.Add((DistanceSquared(position, _goalMin, _goalMax), node));
            }
        }
    }

    /// <summary>Rejects launches outside the complete landing envelope before scanning nearby columns.</summary>
    private bool InRange(Point source, float range) => DistanceSquared(source, _landingMin, _landingMax) <= range * range;

    private void IndexLanding(Point position, int node, float spacing)
    {
        var column = ((int)Math.Round(position.x / spacing), (int)Math.Round(position.z / spacing));
        if (!Columns.TryGetValue(column, out var nodes))
        {
            nodes = new List<int>();
            Columns.Add(column, nodes);
        }

        nodes.Add(node);
        Expand(position, ref _landingMin, ref _landingMax);
    }

    private static void Expand(Point point, ref Point min, ref Point max)
    {
        min = new Point(Math.Min(min.x, point.x), Math.Min(min.y, point.y), Math.Min(min.z, point.z));
        max = new Point(Math.Max(max.x, point.x), Math.Max(max.y, point.y), Math.Max(max.z, point.z));
    }

    private static float DistanceSquared(Point point, Point min, Point max)
    {
        var x = Math.Max(0, Math.Max(min.x - point.x, point.x - max.x));
        var y = Math.Max(0, Math.Max(min.y - point.y, point.y - max.y));
        var z = Math.Max(0, Math.Max(min.z - point.z, point.z - max.z));
        return x * x + y * y + z * z;
    }
}
