using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Ensures vertical navigation reaches real floors rather than ring balconies.
/// </summary>
public sealed class FloorNavigationTests
{
    /// <summary>
    /// Long narrow balconies cannot become floors solely because of total area.
    /// </summary>
    [Fact]
    public void RejectsRingBalconiesAsFloors()
    {
        var graph = Fortress();
        var layout = Detect(graph);
        Assert.Equal(new[] { 0f, 12f }, layout.Levels);
    }

    /// <summary>
    /// A bridge assigned the nearest floor label is not an arrival landing.
    /// </summary>
    [Theory]
    [InlineData(4, 0)]
    [InlineData(8, 1)]
    public void BridgeCannotCompleteFloorNavigation(float bridgeHeight, int labeledFloor)
    {
        var graph = Fortress();
        var layout = Detect(graph);
        var bridges = graph.Nodes.Select((node, index) => (node, index)).Where(p => p.node.Position.y == bridgeHeight);
        Assert.All(
            bridges,
            p =>
        {
            Assert.Equal(labeledFloor, p.node.Floor);
            Assert.DoesNotContain(p.index, layout.LandingNodes);
            Assert.False(new FloorNavigationPolicy().IsDestination(graph, layout, p.index, labeledFloor));
        });
    }

    /// <summary>
    /// Both navigation directions must cross intermediate bridges to reach a real landing.
    /// </summary>
    [Theory]
    [InlineData(0, 1, 12)]
    [InlineData(12, 0, 0)]
    public void ReachesActualDestinationFloor(float startHeight, int destination, float expectedHeight)
    {
        var graph = Fortress();
        var layout = Detect(graph);
        var start = graph.Nodes.FindIndex(n => n.Position.y == startHeight);
        var policy = new FloorNavigationPolicy();
        var route = new RoutePlanner().Find(
            graph,
            start,
            n => policy.IsDestination(graph, layout, n, destination),
            new RoutePolicy());
        Assert.NotEmpty(route);
        Assert.Equal(expectedHeight, graph.Nodes[route.Last()].Position.y);
        Assert.Contains(route.Last(), layout.LandingNodes);
    }

    /// <summary>
    /// Adjacent floor selection uses physical height and respects dungeon limits.
    /// </summary>
    [Theory]
    [InlineData(8, 1, 1)]
    [InlineData(4, -1, 0)]
    [InlineData(12, 1, -1)]
    [InlineData(0, -1, -1)]
    public void ChoosesAdjacentPhysicalFloor(float height, int direction, int expected)
    {
        Assert.Equal(expected, new FloorNavigationPolicy().Next(Detect(Fortress()), height, direction));
    }

    /// <summary>
    /// Rescanning cannot silently substitute a different requested elevation.
    /// </summary>
    [Fact]
    public void RejectsMissingRequestedHeight()
    {
        Assert.Equal(-1, new FloorNavigationPolicy().Resolve(Detect(Fortress()), 6));
    }

    /// <summary>
    /// Empty collision geometry must not invent an architectural level.
    /// </summary>
    [Fact]
    public void EmptyGraphHasNoFloors()
    {
        Assert.Empty(Detect(new NavGraph()).Levels);
    }

    /// <summary>
    /// Stairs and small floor irregularities do not create extra levels.
    /// </summary>
    [Fact]
    public void DetectsArchitecturalLevels()
    {
        var graph = new NavGraph();
        for (var level = 0; level < 3; level++)
        {
            var ids = TestSurfaces.Add(graph, level * 6, 8, 8, 0);
            foreach (var id in ids)
            {
                var point = graph.Nodes[id].Position;
                point.y += id % 2 * 0.05f;
                graph.Nodes[id].Position = point;
            }

            if (level > 0)
            {
                graph.Connect(ids[0] - 64, ids[0], PassageKind.Stairs);
            }
        }

        var layout = Detect(graph);
        Assert.Equal(3, layout.Levels.Count);
        Assert.Equal(
            new[] { 0, 1, 2 },
            new[] { graph.Nodes[0].Floor, graph.Nodes[64].Floor, graph.Nodes[128].Floor });
        Assert.Equal(2, FloorDetector.Nearest(layout.Levels, 11.9f));
    }

    private static FloorLayout Detect(NavGraph graph) =>
        new FloorDetector(new FloorLandingDetector()).Detect(graph, 1.25f);

    private static NavGraph Fortress()
    {
        var graph = new NavGraph();
        var lower = TestSurfaces.Add(graph, 0, 8, 8, 0);
        var lowerBridge = TestSurfaces.Add(graph, 4, 14, 14, 20, true);
        var upperBridge = TestSurfaces.Add(graph, 8, 14, 14, 20, true);
        var upper = TestSurfaces.Add(graph, 12, 8, 8, 50);
        graph.Connect(lower[0], lowerBridge[0], PassageKind.Stairs);
        graph.Connect(lowerBridge[0], upperBridge[0], PassageKind.Stairs);
        graph.Connect(upperBridge[0], upper[0], PassageKind.Stairs);
        return graph;
    }
}
