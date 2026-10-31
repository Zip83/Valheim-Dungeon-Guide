using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Builds directed ordinary-route prefixes and goal-reaching suffixes for a single grapple fallback.</summary>
internal sealed class GrappleRouteTopology
{
    private readonly Dictionary<int, int> _fromStart = new();
    private readonly Dictionary<int, int> _towardGoal = new();

    /// <summary>Requested goal nodes used to prioritize promising launch positions.</summary>
    public List<int> Goals { get; } = new();

    /// <summary>Launch nodes connected to the player by currently permitted ordinary passages.</summary>
    public IEnumerable<int> Sources => _fromStart.Keys;

    /// <summary>Whether a node belongs to the currently permitted ordinary approach component.</summary>
    public bool IsSource(int node) => _fromStart.ContainsKey(node);

    /// <summary>Whether a landing has an ordinary route to a requested goal outside the reachable component.</summary>
    public bool IsLanding(int node) => !_fromStart.ContainsKey(node) && _towardGoal.ContainsKey(node);

    /// <summary>Nodes with a permitted ordinary suffix to the goal, excluding the reachable launch component.</summary>
    public IEnumerable<int> Landings
    {
        get
        {
            foreach (var node in _towardGoal.Keys)
            {
                if (IsLanding(node))
                {
                    yield return node;
                }
            }
        }
    }

    /// <summary>Whether at least one requested goal lies outside the ordinarily reachable component.</summary>
    public bool HasLandings
    {
        get; private set;
    }

    /// <summary>Builds connectivity cooperatively without changing the published graph or inferring reverse edges.</summary>
    public IEnumerable<object> Build(NavGraph graph, int start, Predicate<int> target, RoutePolicy policy,
        FrameWorkBudget work)
    {
        var reverse = new Dictionary<int, List<int>>();
        foreach (var pause in IndexIncoming(graph, policy, reverse, work))
        {
            yield return pause;
        }

        foreach (var pause in Traverse(new[] { start }, _fromStart, n => Outgoing(graph, n, policy), work))
        {
            yield return pause;
        }

        var goals = Goals;
        foreach (var pause in CollectGoals(graph.Nodes.Count, target, goals, work))
        {
            yield return pause;
        }

        if (!HasLandings)
        {
            yield break;
        }

        foreach (var pause in Traverse(goals, _towardGoal,
            n => reverse.TryGetValue(n, out var incoming) ? incoming : Array.Empty<int>(), work))
        {
            yield return pause;
        }
    }

    /// <summary>Joins ordinary segments with one explicit directed grapple; invalid endpoints yield no route.</summary>
    public List<int> Join(int launch, int landing)
    {
        var route = new List<int>();
        if (!_fromStart.ContainsKey(launch) || !IsLanding(landing))
        {
            return route;
        }

        AppendChain(route, _fromStart, launch);
        route.Reverse();
        AppendChain(route, _towardGoal, landing);
        return route;
    }

    private IEnumerable<object> CollectGoals(int count, Predicate<int> target, List<int> goals,
        FrameWorkBudget work)
    {
        for (var i = 0; i < count; i++)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            if (target(i))
            {
                goals.Add(i);
                HasLandings |= !_fromStart.ContainsKey(i);
            }
        }
    }

    private static void AppendChain(List<int> route, Dictionary<int, int> parents, int node)
    {
        while (node >= 0)
        {
            route.Add(node);
            node = parents[node];
        }
    }

    private static IEnumerable<int> Outgoing(NavGraph graph, int node, RoutePolicy policy)
    {
        foreach (var edge in graph.Nodes[node].Edges)
        {
            if (policy.Allows(edge))
            {
                yield return edge.To;
            }
        }
    }

    private static IEnumerable<object> IndexIncoming(NavGraph graph, RoutePolicy policy,
        Dictionary<int, List<int>> reverse, FrameWorkBudget work)
    {
        for (var source = 0; source < graph.Nodes.Count; source++)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            foreach (var edge in graph.Nodes[source].Edges)
            {
                while (!work.TryStartWork())
                {
                    yield return null;
                    work.BeginFrame();
                }

                if (policy.Allows(edge))
                {
                    if (!reverse.TryGetValue(edge.To, out var incoming))
                    {
                        incoming = new List<int>();
                        reverse.Add(edge.To, incoming);
                    }

                    incoming.Add(source);
                }
            }
        }
    }

    private static IEnumerable<object> Traverse(IEnumerable<int> roots,
        Dictionary<int, int> parents, Func<int, IEnumerable<int>> adjacent, FrameWorkBudget work)
    {
        var queue = new Queue<int>();
        foreach (var root in roots)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            parents[root] = -1;
            queue.Enqueue(root);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var next in adjacent(node))
            {
                while (!work.TryStartWork())
                {
                    yield return null;
                    work.BeginFrame();
                }

                if (!parents.ContainsKey(next))
                {
                    parents.Add(next, node);
                    queue.Enqueue(next);
                }
            }
        }
    }
}
