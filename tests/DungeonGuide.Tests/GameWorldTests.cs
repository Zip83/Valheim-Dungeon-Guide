using System;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Exercises the production world reader through connecting and disconnected network states.</summary>
public sealed class GameWorldTests : IDisposable
{
    /// <summary>No network instance means there is no usable world identity.</summary>
    [Fact]
    public void MissingNetworkReturnsNoWorld()
    {
        ZNet.instance = null;

        Assert.Null(GameWorld.Id);
    }

    /// <summary>A network exists before the server supplies its world descriptor.</summary>
    [Fact]
    public void ConnectingNetworkReturnsNoWorld()
    {
        ZNet.instance = new ZNet();

        Assert.Null(GameWorld.Id);
    }

    /// <summary>The reader follows descriptor arrival, removal and replacement without retaining an old UID.</summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(long.MaxValue)]
    public void TracksCurrentWorldDescriptor(long uid)
    {
        ZNet.instance = new ZNet();
        Assert.Null(GameWorld.Id);

        ZNet.instance.Descriptor = new ZNet.WorldDescriptor(uid);
        Assert.Equal(uid, GameWorld.Id);

        ZNet.instance.Descriptor = null;
        Assert.Null(GameWorld.Id);

        ZNet.instance.Descriptor = new ZNet.WorldDescriptor(-1);
        Assert.Equal(-1L, GameWorld.Id);
    }

    /// <summary>Unity's destroyed-object state must not query the former network instance.</summary>
    [Fact]
    public void DestroyedNetworkReturnsNoWorld()
    {
        ZNet.instance = new ZNet { Destroyed = true };

        Assert.Null(GameWorld.Id);
    }

    /// <inheritdoc />
    public void Dispose() => ZNet.instance = null;
}
