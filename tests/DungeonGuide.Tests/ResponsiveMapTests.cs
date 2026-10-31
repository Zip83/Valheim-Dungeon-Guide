using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects display-before-routing, independent change consumers, and incremental fog updates.</summary>
public sealed class ResponsiveMapTests
{
    /// <summary>A local preview needs no navigation edges and excludes stacked geometry.</summary>
    [Fact]
    public void ClassifiesPreviewWithoutNavigation()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(1, 0.5f, 0)));
        graph.Nodes.Add(new NavNode(new Point(0, 4, 0)));
        var layout = PreviewFloorClassifier.Classify(graph, new Point(0, 0.1f, 0));
        Assert.Single(layout.Levels);
        Assert.Equal(0, graph.Nodes[1].Floor);
        Assert.Equal(-1, graph.Nodes[2].Floor);
        Assert.All(graph.Nodes, node => Assert.Empty(node.Edges));
    }

    /// <summary>A preview with no nearby standable sample cannot invent a floor.</summary>
    [Fact]
    public void RejectsUnsupportedPreviewElevation()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 20, 0)));
        Assert.Empty(PreviewFloorClassifier.Classify(graph, new Point()).Levels);
    }

    /// <summary>Consumers cannot reset the shared frame budget or charge unrelated game processing.</summary>
    [Fact]
    public void SharesActiveWorkAcrossConsumers()
    {
        var now = 0.0;
        var frame = 1;
        var budget = new FrameWorkBudget(100, 1, () => now, () => frame);
        Assert.True(budget.TryStartWork());
        now = 0.6;
        budget.Pause();
        now = 100;
        budget.BeginFrame();
        budget.Resume();
        Assert.True(budget.TryStartWork());
        now += 0.5;
        Assert.False(budget.TryStartWork());
        frame++;
        budget.BeginFrame();
        Assert.True(budget.TryStartWork());
    }

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

    /// <summary>Schema 2 retains all existing state without claiming any whole-room visit.</summary>
    [Fact]
    public void MigratesVersionTwoWithoutInferringRooms()
    {
        var data = new SavedRun { version = 2, visitedRooms = null };
        data.visited.Add("known");
        var result = new SaveMigration().Upgrade(data);
        Assert.Same(data, result);
        Assert.Equal(3, result.version);
        Assert.Equal("known", Assert.Single(result.visited));
        Assert.Empty(result.visitedRooms);
    }

    /// <summary>Stable room-local anchors survive migration without depending on current floor numbers.</summary>
    [Fact]
    public void PreservesRoomVisitAnchors()
    {
        var visit = new RoomVisit { room = "room-pose", local = new Point(2, 4, 1) };
        var data = new SavedRun();
        data.visitedRooms.Add(visit);
        Assert.Same(visit, Assert.Single(new SaveMigration().Upgrade(data).visitedRooms));
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
