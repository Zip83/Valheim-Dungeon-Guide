using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects independent incremental mask consumers and feathering across tile seams.</summary>
public sealed class IncrementalFogUpdatesTests
{
    /// <summary>Changed tile feathering matches full reconstruction, including tile seams and reloads.</summary>
    [Fact]
    public void IncrementalFogMatchesFullReconstruction()
    {
        var raster = Raster();
        var exploration = new Exploration();
        var incremental = new IncrementalFloorFog(raster);
        foreach (var step in incremental.Initialize())
        {
        }
        incremental.Observe(exploration);
        Drain(incremental, exploration);
        Assert.All(incremental.Fog.Opacity, alpha => Assert.Equal(255, alpha));
        exploration.Reveal(raster.Graph.Nodes[0]);
        incremental.Observe(exploration);
        Drain(incremental, exploration);
        var reference = new FloorFogMask(raster);
        foreach (var step in reference.Build(exploration))
        {
        }
        Assert.Equal(reference.Opacity, incremental.Fog.Opacity);
        exploration.Load(Array.Empty<string>());
        incremental.Observe(exploration);
        Drain(incremental, exploration);
        Assert.All(incremental.Fog.Opacity, alpha => Assert.Equal(255, alpha));
    }

    /// <summary>Detail and overview consume the same additions independently and ignore unrelated tiles.</summary>
    [Fact]
    public void IndependentMasksReceiveOnlyAffectedTiles()
    {
        var raster = Raster();
        var exploration = new Exploration();
        var masks = new[] { new IncrementalFloorFog(raster), new IncrementalFloorFog(raster) };
        foreach (var mask in masks)
        {
            foreach (var step in mask.Initialize())
            {
            }
            mask.Observe(exploration);
            Drain(mask, exploration);
        }

        exploration.Reveal(raster.Graph.Nodes[0]);
        foreach (var mask in masks)
        {
            mask.Observe(exploration);
            var tiles = new List<int>();
            while (mask.TryTake(out var tile))
            {
                tiles.Add(tile);
            }

            // The known footprint plus feather halo spans exactly three tile rows and columns.
            Assert.Equal(new[] { 0, 1, 2, 4, 5, 6, 8, 9, 10 }, tiles.OrderBy(tile => tile));
        }
    }

    private static FloorPlanRaster Raster()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(4, 0, 0)));
        var raster = new FloorPlanRaster(128, 0, new Point(), 16) { Graph = graph };
        for (var y = 20; y < 80; y++)
        {
            for (var x = 20; x < 80; x++)
            {
                raster.Owners[y * 128 + x] = x < 65 ? 1 : 2;
            }
        }

        return raster;
    }

    private static void Drain(IncrementalFloorFog fog, Exploration exploration)
    {
        while (fog.TryTake(out var tile))
        {
            foreach (var step in fog.Update(tile, exploration))
            {
            }
        }
    }
}
