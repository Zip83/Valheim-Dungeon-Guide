using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>Selects overview floors and visible extents independently of UI and saved-state mutation.</summary>
internal sealed class OverviewFloorCatalog
{
    private readonly Dictionary<int, OverviewFloor> _entries = new();
    private readonly Dictionary<string, NavNode> _nodes = new();
    private int _cursor;

    /// <summary>Completed floor entries, ordered from highest to lowest; empty until enumeration finishes.</summary>
    public IReadOnlyList<OverviewFloor> Floors { get; private set; } = new List<OverviewFloor>();

    /// <summary>Reads one node per suspension point and publishes only after all nodes are classified.</summary>
    /// <param name="graph">Completed scan with assigned physical floors.</param>
    /// <param name="exploration">Saved exploration, read without adding or removing keys.</param>
    /// <param name="revealAll">Whether unknown samples may contribute floors and their extents.</param>
    /// <returns>Cooperative work steps; dispose early to cancel without publishing a partial catalog.</returns>
    public IEnumerable<byte> Build(NavGraph graph, Exploration exploration, bool revealAll)
    {
        var floors = _entries;
        _entries.Clear();
        _nodes.Clear();
        _cursor = 0;
        foreach (var node in graph.Nodes)
        {
            yield return 0;
            _nodes[node.Key] = node;
            if (node.Floor < 0 || !revealAll && !exploration.Has(node))
            {
                continue;
            }

            Include(floors, node);
        }

        Floors = floors.Values.OrderByDescending(floor => floor.Floor).ToList();
    }

    /// <summary>Extends known floor extents from additions without rescanning unchanged graph nodes.</summary>
    public IEnumerable<byte> Update(Exploration exploration)
    {
        var count = exploration.Changes.Count;
        while (_cursor < count)
        {
            yield return 0;
            var key = exploration.Changes[_cursor++].Key;
            if (_nodes.TryGetValue(key, out var node) && node.Floor >= 0 && exploration.Has(node))
            {
                Include(_entries, node);
            }
        }

        Floors = _entries.Values.OrderByDescending(floor => floor.Floor).ToList();
    }

    private static void Include(Dictionary<int, OverviewFloor> floors, NavNode node)
    {
        if (!floors.TryGetValue(node.Floor, out var floor))
        {
            floors.Add(node.Floor, new OverviewFloor(node.Floor, node.Position));
            return;
        }

        floor.Include(node.Position);
    }
}
