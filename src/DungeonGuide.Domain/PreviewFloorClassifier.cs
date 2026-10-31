using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Assigns a conservative local display layer without consulting navigation edges.</summary>
internal static class PreviewFloorClassifier
{
    /// <summary>Publishes one provisional layer near the player; other elevations remain unclassified.</summary>
    public static FloorLayout Classify(NavGraph graph, Point player)
    {
        var nearest = graph.Nearest(player, 5);
        var layout = new FloorLayout();
        if (nearest < 0)
        {
            return layout;
        }

        var height = graph.Nodes[nearest].Position.y;
        layout.Levels.Add(height);
        foreach (var node in graph.Nodes)
        {
            node.Floor = Math.Abs(node.Position.y - height) <= 1.2f ? 0 : -1;
        }

        return layout;
    }
}
