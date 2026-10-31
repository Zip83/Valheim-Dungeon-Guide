using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Checks gravity-affected hook reach, projectile expiry, and numerical boundary cases.</summary>
public sealed class GrappleShotTests
{
    /// <summary>The low arc arrives at the requested horizontal, elevated, vertical, or lower anchor.</summary>
    [Theory]
    [InlineData(10, 0, 0)]
    [InlineData(10, 5, 4)]
    [InlineData(0, 5, 0)]
    [InlineData(0, -5, 0)]
    public void HitsRequestedAnchor(float x, float y, float z)
    {
        Assert.True(GrappleShot.TryCreate(new Point(x, y, z), 30, 10, 3, out var shot));
        var end = shot.At(1);
        Assert.InRange(end.x, x - 0.001f, x + 0.001f);
        Assert.InRange(end.y, y - 0.001f, y + 0.001f);
        Assert.InRange(end.z, z - 0.001f, z + 0.001f);
    }

    /// <summary>Gravity requires aiming above the direct line to the anchor.</summary>
    [Fact]
    public void ChecksArcAboveSightLine()
    {
        Assert.True(GrappleShot.TryCreate(new Point(20, 0, 0), 20, 10, 3, out var shot));
        Assert.True(shot.At(0.5f).y > 0);
    }

    /// <summary>A gravity-free projectile retains its straight trajectory.</summary>
    [Fact]
    public void SupportsStraightProjectile()
    {
        Assert.True(GrappleShot.TryCreate(new Point(10, 4, 0), 20, 0, 1, out var shot));
        Assert.Equal(2, shot.At(0.5f).y);
    }

    /// <summary>Unreachable range and projectile expiry cannot become hook routes.</summary>
    [Theory]
    [InlineData(45, 0, 20, 10, 10)]
    [InlineData(10, 0, 20, 10, 0.1f)]
    [InlineData(0, 81, 40, 10, 10)]
    [InlineData(0, 0, 20, 10, 10)]
    public void RejectsUnreachableShot(float distance, float rise, float speed, float gravity, float lifetime)
    {
        Assert.False(GrappleShot.TryCreate(new Point(distance, rise, 0), speed, gravity, lifetime, out _));
    }

    /// <summary>The maximum horizontal range at equal elevation is accepted at the tangent boundary.</summary>
    [Fact]
    public void AcceptsBallisticRangeBoundary()
    {
        Assert.True(GrappleShot.TryCreate(new Point(40, 0, 0), 20, 10, 3, out _));
    }

    /// <summary>Malformed or unsupported native metadata never creates a usable trajectory.</summary>
    [Theory]
    [InlineData(float.NaN, 10, 1)]
    [InlineData(float.PositiveInfinity, 10, 1)]
    [InlineData(0, 10, 1)]
    [InlineData(20, -1, 1)]
    [InlineData(20, 10, -1)]
    public void RejectsInvalidCapabilities(float speed, float gravity, float lifetime)
    {
        Assert.False(GrappleShot.TryCreate(new Point(5, 0, 0), speed, gravity, lifetime, out _));
    }
}
