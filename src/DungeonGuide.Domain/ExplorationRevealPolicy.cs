using System;

namespace DungeonGuide;

/// <summary>Applies physical floor, circular distance, and line-of-sight rules before recording exploration.</summary>
internal static class ExplorationRevealPolicy
{
    /// <summary>
    /// Reveals only visible samples on the player's physical floor within three metres and vertical tolerance.
    /// The visibility callback is not invoked for known or out-of-range samples.
    /// </summary>
    public static bool TryReveal(Exploration exploration, NavNode node, Point player, int physicalFloor,
        Func<Point, Point, bool> visible)
    {
        if (physicalFloor < 0 || node.Floor != physicalFloor || exploration.Has(node)
            || Math.Abs(player.y - node.Position.y) > 0.9f || Point.Distance(player, node.Position) > 3)
        {
            return false;
        }

        return visible(player, node.Position) && exploration.Reveal(node);
    }
}
