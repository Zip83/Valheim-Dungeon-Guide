using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Maps persistent entry elevations onto the current physical floor layout.</summary>
internal static class RoomVisitLayerPolicy
{
    /// <summary>Rejects unknown preview elevations instead of assigning an unseen upper layer to the local floor.</summary>
    public static int Resolve(IReadOnlyList<float> levels, float height, bool partial)
    {
        if (levels.Count == 0)
        {
            return -1;
        }

        var floor = FloorDetector.Nearest(levels, height);
        return partial && Math.Abs(levels[floor] - height) > 1.2f ? -1 : floor;
    }
}
