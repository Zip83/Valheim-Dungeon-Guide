namespace DungeonGuide;

/// <summary>Resolves explicit map placement only onto visible sampled floor interiors.</summary>
internal static class WaypointMapPolicy
{
    /// <summary>Returns the physical owning sample, rejecting hidden cells, walls, voids and other floors.</summary>
    public static NavNode Resolve(FloorPlanRaster raster, float u, float v, Exploration exploration, bool reveal)
    {
        if (raster?.Graph == null || !(u >= 0 && u < 1 && v >= 0 && v < 1))
        {
            return null;
        }

        var index = (int)(v * raster.Size) * raster.Size + (int)(u * raster.Size);
        var owner = raster.Owners[index] - 1;
        if (raster.Boundary[index] || owner < 0 || owner >= raster.Graph.Nodes.Count)
        {
            return null;
        }

        var node = raster.Graph.Nodes[owner];
        return node.Floor == raster.Floor && (reveal || exploration.Has(node)) ? node : null;
    }
}
