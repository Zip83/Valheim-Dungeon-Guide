using System;

namespace DungeonGuide;

/// <summary>Selects visible landing representatives without hiding a partially explored staircase.</summary>
internal static class StairMarkerSelection
{
    private const float LandingHeightTolerance = 0.15f;

    /// <summary>Returns lower upward and upper downward samples, preferring central near-level candidates; -1 means absent.</summary>
    public static (int Up, int Down) Select(NavGraph graph, StairMarkerGroup group, Exploration exploration, bool reveal)
    {
        var up = -1;
        var down = -1;
        foreach (var id in group.Nodes)
        {
            if (!reveal && !exploration.Has(graph.Nodes[id]))
            {
                continue;
            }

            var directions = StairMarkerPolicy.Directions(graph, id);
            if (directions.Up && PrefersLanding(graph, id, up, group.Center, lowest: true))
            {
                up = id;
            }

            if (directions.Down && PrefersLanding(graph, id, down, group.Center, lowest: false))
            {
                down = id;
            }
        }

        return (up, down);
    }

    private static bool PrefersLanding(NavGraph graph, int candidate, int current, Point center, bool lowest)
    {
        if (current < 0)
        {
            return true;
        }

        var point = graph.Nodes[candidate].Position;
        var previous = graph.Nodes[current].Position;
        if (Math.Abs(point.y - previous.y) > LandingHeightTolerance)
        {
            return lowest ? point.y < previous.y : point.y > previous.y;
        }

        return DistanceSquared(point, center) < DistanceSquared(previous, center);
    }

    private static float DistanceSquared(Point point, Point center)
    {
        var x = point.x - center.x;
        var z = point.z - center.z;
        return x * x + z * z;
    }
}
