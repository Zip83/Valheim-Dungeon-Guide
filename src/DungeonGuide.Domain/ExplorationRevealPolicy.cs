using System;

namespace DungeonGuide;

/// <summary>Applies physical floor, circular distance, and line-of-sight rules before recording exploration.</summary>
internal static class ExplorationRevealPolicy
{
    /// <summary>
    /// Reveals only visible samples on the player's physical floor within the configured horizontal radius.
    /// The visibility callback is not invoked for known or out-of-range samples.
    /// </summary>
    public static bool TryReveal(Exploration exploration, NavNode node, Point player, int physicalFloor,
        Func<Point, Point, bool> visible, float radius = 25)
    {
        if (physicalFloor < 0 || node.Floor != physicalFloor || exploration.Has(node)
            || HorizontalSquared(player, node.Position) > radius * radius)
        {
            return false;
        }

        return visible(player, node.Position) && exploration.Reveal(node);
    }

    private static float HorizontalSquared(Point a, Point b) =>
        (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
}
