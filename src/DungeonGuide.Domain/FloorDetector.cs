using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>
/// Infers architectural levels from connected, broad, nearly horizontal navigation surfaces.
/// </summary>
public sealed class FloorDetector
{
    private readonly FloorLandingDetector _landingDetector;

    /// <summary>
    /// Uses a landing detector to distinguish broad floor interiors from narrow bridges and balconies.
    /// </summary>
    /// <param name="landingDetector">Detector recognizing broad interior landing geometry.</param>
    public FloorDetector(FloorLandingDetector landingDetector)
    {
        _landingDetector = landingDetector;
    }

    /// <summary>
    /// Groups sampled surfaces into levels, assigns node floor indices, and records supported landing nodes.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="spacing">Horizontal sample spacing in world metres.</param>
    /// <returns>Detected levels and broad landing nodes; an empty graph produces no levels.</returns>
    public FloorLayout Detect(NavGraph graph, float spacing)
    {
        // Count connected, near-horizontal surfaces. Isolated treads and decoration must not
        // accumulate into artificial floors simply because they share the same elevation.
        var surfaces = new List<(float height, int count, List<int> landings)>();
        var seen = new HashSet<int>();
        var minimum = Math.Max(6, (int)Math.Ceiling(16 / (spacing * spacing)));
        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            if (!graph.Nodes[i].Flat || !seen.Add(i))
            {
                continue;
            }

            var reference = graph.Nodes[i].Position.y;
            float sum = 0;
            var count = 0;
            var members = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                count++;
                members.Add(n);
                sum += graph.Nodes[n].Position.y;
                foreach (var edge in graph.Nodes[n].Edges)
                {
                    var next = graph.Nodes[edge.To];
                    if (edge.Verified && (edge.Kind == PassageKind.Walk || edge.Kind == PassageKind.Stairs) && next.Flat
                        && Math.Abs(next.Position.y - reference) < 0.35f
                        && seen.Add(edge.To))
                    {
                        queue.Enqueue(edge.To);
                    }
                }
            }

            if (count >= minimum)
            {
                var landings = _landingDetector.Find(graph, members, spacing);
                if (landings.Count >= 4)
                {
                    surfaces.Add((sum / count, count, landings));
                }
            }
        }

        var bins = surfaces.GroupBy(s => (int)Math.Round(s.height * 4))
            .Select(g => (height: g.Key / 4f, count: g.Sum(s => s.count)))
            .OrderByDescending(g => g.count)
            .ToList();
        var layout = new FloorLayout();
        var levels = layout.Levels;
        foreach (var bin in bins)
        {
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
        foreach (var node in graph.Nodes)
        {
            node.Floor = Nearest(levels, node.Position.y);
        }

        foreach (var surface in surfaces)
        {
            foreach (var id in surface.landings)
            {
                var node = graph.Nodes[id];
                if (levels.Count > 0 && Math.Abs(node.Position.y - levels[node.Floor]) <= 0.4f)
                {
                    layout.LandingNodes.Add(id);
                }
            }
        }

        return layout;
    }

    /// <summary>
    /// Returns the index of the detected elevation closest to the supplied world-space height.
    /// </summary>
    /// <param name="levels">Detected floor elevations ordered from lowest to highest.</param>
    /// <param name="y">Player or sample elevation in world metres.</param>
    /// <returns>Index of the closest detected elevation; callers must supply a non-empty list.</returns>
    public static int Nearest(IReadOnlyList<float> levels, float y)
    {
        var result = 0;
        var best = float.MaxValue;
        for (var i = 0; i < levels.Count; i++)
        {
            var d = Math.Abs(levels[i] - y);
            if (d < best)
            {
                result = i;
                best = d;
            }
        }

        return result;
    }
}
