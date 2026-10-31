using System;

namespace DungeonGuide;

/// <summary>Solves the low ballistic arc for a gravity-affected hook projectile without drag.</summary>
internal readonly struct GrappleShot
{
    private readonly Point _velocity;
    private readonly float _gravity;
    private readonly float _duration;

    private GrappleShot(Point velocity, float gravity, float duration)
    {
        _velocity = velocity;
        _gravity = gravity;
        _duration = duration;
    }

    /// <summary>Calculates relative projectile position at a normalized fraction of the impact time.</summary>
    public Point At(float progress)
    {
        var time = _duration * progress;
        return new Point(_velocity.x * time, _velocity.y * time - 0.5f * _gravity * time * time, _velocity.z * time);
    }

    /// <summary>Rejects unreachable shots and impacts after the projectile's positive lifetime; zero means unlimited.</summary>
    /// <param name="delta">Anchor displacement from the native shot origin, in world metres.</param>
    /// <param name="speed">Launch speed in metres per second.</param>
    /// <param name="gravity">Non-negative downward acceleration in metres per second squared.</param>
    /// <param name="lifetime">Projectile expiry in seconds, or zero for no expiry.</param>
    /// <param name="shot">Low-arc trajectory on success; default on failure.</param>
    /// <returns>Whether a finite trajectory reaches the anchor before expiry.</returns>
    public static bool TryCreate(Point delta, float speed, float gravity, float lifetime, out GrappleShot shot)
    {
        shot = default;
        if (!Finite(delta.x) || !Finite(delta.y) || !Finite(delta.z) || !Finite(speed) || speed <= 0
            || !Finite(gravity) || gravity < 0 || !Finite(lifetime) || lifetime < 0)
        {
            return false;
        }

        var squaredDistance = (double)delta.x * delta.x + (double)delta.y * delta.y + (double)delta.z * delta.z;
        var duration = Duration(squaredDistance, delta.y, speed, gravity);
        if (!(duration > 0) || !Finite(duration) || lifetime > 0 && duration > lifetime)
        {
            return false;
        }

        var velocity = new Point(delta.x / duration,
            delta.y / duration + 0.5f * gravity * duration, delta.z / duration);
        shot = new GrappleShot(velocity, gravity, duration);
        return true;
    }

    private static float Duration(double squaredDistance, float rise, float speed, float gravity)
    {
        var energy = (double)speed * speed - gravity * rise;
        var discriminant = energy * energy - (double)gravity * gravity * squaredDistance;
        if (energy <= 0 || discriminant < 0)
        {
            return 0;
        }

        return (float)Math.Sqrt(2 * squaredDistance / (energy + Math.Sqrt(discriminant)));
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
