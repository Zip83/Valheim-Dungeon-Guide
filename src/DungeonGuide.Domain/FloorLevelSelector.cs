using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DungeonGuide;

/// <summary>
/// Selects distinct architectural elevations, preferring the strongest sampled surfaces.
/// </summary>
internal static class FloorLevelSelector
{
    internal static List<float> Select(
        NavGraph graph, List<(float height, int count, List<int> landings)> surfaces,
        int minimum, CancellationToken cancellationToken)
    {
        var bins = surfaces.GroupBy(s => (int)Math.Round(s.height * 4))
            .Select(g => (height: g.Key / 4f, count: g.Sum(s => s.count)))
            .OrderByDescending(g => g.count)
            .ToList();
        var levels = new List<float>();
        foreach (var bin in bins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (bin.count >= minimum && levels.All(h => Math.Abs(h - bin.height) > 2f))
            {
                levels.Add(bin.height);
            }
        }

        if (levels.Count == 0 && graph.Nodes.Count > 0)
        {
            levels.Add(graph.Nodes.OrderBy(n => n.Position.y).ElementAt(graph.Nodes.Count / 2).Position.y);
        }

        levels.Sort();
        return levels;
    }
}
