using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonGuide;

/// <summary>
/// Recognizes broad two-dimensional floor interiors rather than long narrow surfaces.
/// </summary>
public sealed class FloorLandingDetector
{
    /// <summary>
    /// Returns surface nodes with sufficient nearby grid support to act as genuine floor landing candidates.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="surface">Node indices belonging to one connected candidate floor surface.</param>
    /// <param name="spacing">Horizontal sample spacing in world metres.</param>
    /// <param name="cancellationToken">Cancellation request checked throughout analysis of large graphs.</param>
    /// <returns>Indices of supported interior nodes within the supplied surface.</returns>
    /// <exception cref="OperationCanceledException">The scan was cancelled.</exception>
    public List<int> Find(
        NavGraph graph,
        IReadOnlyList<int> surface,
        float spacing,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cells = IndexSurface(graph, surface, spacing, cancellationToken);

        return SelectLandings(graph, surface, cells, spacing, cancellationToken);
    }

    private static HashSet<(int x, int z)> IndexSurface(
        NavGraph graph,
        IReadOnlyList<int> surface,
        float spacing,
        CancellationToken cancellationToken)
    {
        var cells = new HashSet<(int x, int z)>();
        foreach (var id in surface)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cells.Add(CellAt(graph.Nodes[id].Position, spacing));
        }

        return cells;
    }

    private static List<int> SelectLandings(
        NavGraph graph,
        IReadOnlyList<int> surface,
        HashSet<(int x, int z)> cells,
        float spacing,
        CancellationToken cancellationToken)
    {
        var result = new List<int>();
        foreach (var id in surface)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cell = CellAt(graph.Nodes[id].Position, spacing);
            if (HasBroadSupport(cells, cell, cancellationToken))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static (int x, int z) CellAt(Point position, float spacing)
    {
        return ((int)Math.Round(position.x / spacing), (int)Math.Round(position.z / spacing));
    }

    private static bool HasBroadSupport(
        HashSet<(int x, int z)> cells,
        (int x, int z) center,
        CancellationToken cancellationToken)
    {
        var supported = 0;
        // The disk has 21 cells; a three-cell-wide bridge cannot reach the required coverage.
        for (var dx = -2; dx <= 2; dx++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var dz = -2; dz <= 2; dz++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dx * dx + dz * dz <= 5 && cells.Contains((center.x + dx, center.z + dz)))
                {
                    supported++;
                    if (supported >= 16)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
