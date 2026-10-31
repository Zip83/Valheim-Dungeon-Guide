using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>Groups neighboring stair samples once during cooperative floor-raster construction.</summary>
internal sealed class StairMarkerGrouping
{
    private readonly NavGraph _graph;
    private readonly FloorPlanRaster _raster;
    private readonly Dictionary<int, List<int>> _neighbors = new();
    private readonly HashSet<int> _visited = new();

    /// <summary>Uses only the private raster's feature index and the matching published graph.</summary>
    public StairMarkerGrouping(NavGraph graph, FloorPlanRaster raster)
    {
        _graph = graph;
        _raster = raster;
    }

    /// <summary>Yields between candidates and connections without modifying navigation or merging floors.</summary>
    public IEnumerable<byte> Build()
    {
        foreach (var step in IndexCandidates())
        {
            yield return step;
        }

        foreach (var step in ConnectCandidates())
        {
            yield return step;
        }

        foreach (var step in CollectGroups())
        {
            yield return step;
        }
    }

    private IEnumerable<byte> CollectGroups()
    {
        foreach (var id in _raster.Features)
        {
            yield return 0;
            if (_neighbors.ContainsKey(id) && _visited.Add(id))
            {
                foreach (var step in CollectGroup(id))
                {
                    yield return step;
                }
            }
        }
    }

    private IEnumerable<byte> IndexCandidates()
    {
        foreach (var id in _raster.Features)
        {
            var directions = StairMarkerPolicy.Directions(_graph, id);
            if (_graph.Nodes[id].Floor == _raster.Floor && (directions.Up || directions.Down))
            {
                _neighbors.Add(id, new List<int>());
            }

            yield return 0;
        }
    }

    private IEnumerable<byte> ConnectCandidates()
    {
        foreach (var id in _neighbors.Keys)
        {
            foreach (var edge in _graph.Nodes[id].Edges.ToArray())
            {
                if (edge.Verified && (edge.Kind == PassageKind.Walk || edge.Kind == PassageKind.Stairs)
                    && _neighbors.ContainsKey(edge.To) && !_neighbors[id].Contains(edge.To))
                {
                    _neighbors[id].Add(edge.To);
                    _neighbors[edge.To].Add(id);
                }

                yield return 0;
            }
        }
    }

    private IEnumerable<byte> CollectGroup(int seed)
    {
        var group = new StairMarkerGroup();
        var pending = new Queue<int>();
        pending.Enqueue(seed);
        var sumX = 0d;
        var sumZ = 0d;
        while (pending.Count > 0)
        {
            var id = pending.Dequeue();
            group.Nodes.Add(id);
            sumX += _graph.Nodes[id].Position.x;
            sumZ += _graph.Nodes[id].Position.z;
            foreach (var neighbor in _neighbors[id])
            {
                if (_visited.Add(neighbor))
                {
                    pending.Enqueue(neighbor);
                }

                yield return 0;
            }
        }

        group.Center = new Point((float)(sumX / group.Nodes.Count), 0, (float)(sumZ / group.Nodes.Count));
        _raster.StairGroups.Add(group);
    }
}
