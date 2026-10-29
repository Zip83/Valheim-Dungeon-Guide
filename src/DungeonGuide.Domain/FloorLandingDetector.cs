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

        var cells = new Dictionary<(int, int), int>();
        foreach (var id in surface)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var p = graph.Nodes[id].Position;
            cells[((int)Math.Round(p.x / spacing), (int)Math.Round(p.z / spacing))] = id;
        }

        var result = new List<int>();
        foreach (var id in surface)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var p = graph.Nodes[id].Position;
            var x = (int)Math.Round(p.x / spacing);
            var z = (int)Math.Round(p.z / spacing);
            var supported = 0;
            // A broad two-dimensional landing, not a long narrow bridge or a ring balcony.
            // The disk has 21 grid cells. A 3-cell-wide bridge cannot satisfy this coverage.
            for (var dx = -2; dx <= 2; dx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var dz = -2; dz <= 2; dz++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (dx * dx + dz * dz <= 5 && cells.ContainsKey((x + dx, z + dz)))
                    {
                        supported++;
                    }
                }
            }

            if (supported >= 15)
            {
                result.Add(id);
            }
        }

        return result;
    }
}
