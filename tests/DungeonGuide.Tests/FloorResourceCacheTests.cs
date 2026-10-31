using System;
using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects resident image reuse and cancellation at geometry/session boundaries.</summary>
public sealed class FloorResourceCacheTests
{
    /// <summary>Entering a prepared neighbor keeps its completed raster instead of constructing a replacement.</summary>
    [Fact]
    public void RetainsReadyNeighborWhenPlayerChangesFloor()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point()) { Floor = 1 });
        using var cache = new FloorResourceCache<FloorPlanCache>();
        var neighbor = new FloorPlanCache();
        neighbor.Tick(graph, 1, 1, 32, new Point(), 8, new FrameWorkBudget(10000, 1000, () => 0));
        var ready = neighbor.Ready;
        Assert.NotNull(ready);
        cache.Add(graph, 1, neighbor);
        cache.Retain(graph, new[] { 1, 2, 0 }, neighbor);

        Assert.True(cache.TryGet(graph, 1, out var retained));
        Assert.Same(neighbor, retained);
        Assert.Same(ready, retained.Ready);
    }

    /// <summary>Floors falling outside the moving window release their owned work exactly once.</summary>
    [Fact]
    public void EvictsOnlyFloorsOutsideWorkingSet()
    {
        var graph = new NavGraph();
        using var cache = new FloorResourceCache<Resource>();
        var outside = new Resource();
        var current = new Resource();
        cache.Add(graph, 0, outside);
        cache.Add(graph, 1, current);
        cache.Retain(graph, new[] { 1, 2 }, current);
        cache.Retain(graph, new[] { 1, 2 }, current);

        Assert.Equal(1, outside.Disposals);
        Assert.Equal(0, current.Disposals);
        Assert.Equal(1, cache.Count);
    }

    /// <summary>A new graph cancels old prefetch work while keeping the displayed image until replacement.</summary>
    [Fact]
    public void KeepsOldDisplayOnlyUntilNewSnapshotIsPublished()
    {
        var oldGraph = new NavGraph();
        var newGraph = new NavGraph();
        using var cache = new FloorResourceCache<Resource>();
        var displayed = new Resource();
        var pending = new Resource();
        var replacement = new Resource();
        cache.Add(oldGraph, 0, displayed);
        cache.Add(oldGraph, 1, pending);
        cache.Retain(newGraph, new[] { 0, 1 }, displayed);
        Assert.Equal(1, pending.Disposals);
        Assert.Equal(0, displayed.Disposals);
        Assert.False(cache.TryGet(newGraph, 0, out _));
        cache.Add(newGraph, 0, replacement);
        cache.Retain(newGraph, new[] { 0, 1 }, replacement);

        Assert.Equal(1, displayed.Disposals);
        Assert.Equal(0, replacement.Disposals);
        Assert.Equal(1, cache.Count);
    }

    /// <summary>Manual browsing adds one floor without evicting the player's physical neighbors.</summary>
    [Fact]
    public void BoundsManualBrowseWorkingSetAndProtectedDisplay()
    {
        var graph = new NavGraph();
        using var cache = new FloorResourceCache<Resource>();
        for (var floor = 0; floor < 10; floor++)
        {
            cache.Add(graph, floor, new Resource());
        }

        Assert.True(cache.TryGet(graph, 8, out var oldDisplay));
        cache.Retain(graph, new[] { 9, 4, 5, 3 }, oldDisplay);
        Assert.Equal(5, cache.Count);
        Assert.True(cache.TryGet(graph, 9, out var replacement));
        cache.Retain(graph, new[] { 9, 4, 5, 3 }, replacement);

        Assert.Equal(4, cache.Count);
        Assert.Equal(1, oldDisplay.Disposals);
        foreach (var floor in new[] { 3, 4, 5 })
        {
            Assert.True(cache.TryGet(graph, floor, out _));
        }
    }

    /// <summary>Session cleanup releases both current and temporarily protected older resources.</summary>
    [Fact]
    public void SessionCleanupReleasesAllResourcesOnce()
    {
        var cache = new FloorResourceCache<Resource>();
        var resource = new Resource();
        var replacement = new Resource();
        var newGraph = new NavGraph();
        cache.Add(new NavGraph(), 0, resource);
        cache.Add(newGraph, 0, replacement);
        cache.Retain(newGraph, new[] { 0 }, resource);
        cache.Dispose();
        cache.Dispose();

        Assert.Equal(1, resource.Disposals);
        Assert.Equal(1, replacement.Disposals);
        Assert.Equal(0, cache.Count);
    }

    /// <summary>Browsing distant floors and returning never retires resources from the active dungeon snapshot.</summary>
    [Fact]
    public void CompletePreloadOrderRetainsEveryPreparedFloorAcrossBrowsing()
    {
        var graph = new NavGraph();
        var resources = new List<Resource>();
        using var cache = new FloorResourceCache<Resource>();
        for (var floor = 0; floor < 10; floor++)
        {
            var resource = new Resource();
            resources.Add(resource);
            cache.Add(graph, floor, resource);
        }

        var targets = new List<int>();
        foreach (var selected in new[] { 0, 9, 4, 1, 8, 0 })
        {
            FloorPreloadPolicy.Fill(targets, 0, selected, 10);
            cache.Retain(graph, targets, resources[selected]);
            Assert.Equal(10, cache.Count);
            for (var floor = 0; floor < 10; floor++)
            {
                Assert.True(cache.TryGet(graph, floor, out var retained));
                Assert.Same(resources[floor], retained);
                Assert.Equal(0, retained.Disposals);
            }
        }
    }

    private sealed class Resource : IDisposable
    {
        internal int Disposals;

        /// <inheritdoc />
        public void Dispose() => Disposals++;
    }
}
