using System;
using System.Collections.Generic;
using System.Threading;

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
    /// <param name="cancellationToken">Cancellation request checked throughout analysis of large graphs.</param>
    /// <returns>Detected levels and broad landing nodes; an empty graph produces no levels.</returns>
    /// <exception cref="OperationCanceledException">The scan was cancelled.</exception>
    public FloorLayout Detect(NavGraph graph, float spacing, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var minimum = Math.Max(6, (int)Math.Ceiling(16 / (spacing * spacing)));
        var surfaces = FloorSurfaceCollector.Collect(graph, spacing, minimum, _landingDetector, cancellationToken);
        var levels = FloorLevelSelector.Select(graph, surfaces, minimum, cancellationToken);
        var layout = new FloorLayout();
        layout.Levels.AddRange(levels);
        AssignNodeFloors(graph, layout, cancellationToken);
        RegisterLandings(graph, layout, surfaces, cancellationToken);

        return layout;
    }

    private static void AssignNodeFloors(NavGraph graph, FloorLayout layout, CancellationToken cancellationToken)
    {
        foreach (var node in graph.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            node.Floor = Nearest(layout.Levels, node.Position.y);
        }
    }

    private static void RegisterLandings(
        NavGraph graph, FloorLayout layout, List<(float height, int count, List<int> landings)> surfaces,
        CancellationToken cancellationToken)
    {
        foreach (var surface in surfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var id in surface.landings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = graph.Nodes[id];
                if (layout.Levels.Count > 0 && Math.Abs(node.Position.y - layout.Levels[node.Floor]) <= 0.4f)
                {
                    layout.LandingNodes.Add(id);
                }
            }
        }
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
