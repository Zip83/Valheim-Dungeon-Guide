using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide.Tests;

internal static class TestSurfaces
{
    internal static List<int> Add(NavGraph graph, float height, int width, int depth, float offsetX, bool ring = false)
    {
        var ids = new Dictionary<(int, int), int>();
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                if (ring && x >= 2 && z >= 2 && x < width - 2 && z < depth - 2)
                {
                    continue;
                }

                ids[(x, z)] = graph.Nodes.Count;
                graph.Nodes.Add(new NavNode(new Point(offsetX + x * 1.25f, height, z * 1.25f)));
            }
        }

        foreach (var cell in ids)
        {
            if (ids.TryGetValue((cell.Key.Item1 + 1, cell.Key.Item2), out var right))
            {
                graph.Connect(cell.Value, right);
            }

            if (ids.TryGetValue((cell.Key.Item1, cell.Key.Item2 + 1), out var forward))
            {
                graph.Connect(cell.Value, forward);
            }
        }

        return ids.Values.ToList();
    }
}
