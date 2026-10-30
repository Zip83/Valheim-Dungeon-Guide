namespace DungeonGuide;

/// <summary>
/// Defines the geometric envelope of an unverified short jump, independently of engine collision queries.
/// </summary>
internal static class JumpCandidateGeometry
{
    /// <summary>
    /// Accepts horizontal gaps from 1.5 to 2.8 metres and rises from -2 to 0.9 metres, including endpoints.
    /// These limits describe candidates and do not establish that a player can complete a jump.
    /// </summary>
    /// <param name="horizontalDistance">Horizontal takeoff-to-landing distance in metres.</param>
    /// <param name="rise">Landing elevation minus takeoff elevation in metres.</param>
    /// <returns>True when both measurements lie inside the supported candidate envelope.</returns>
    public static bool WithinRange(float horizontalDistance, float rise)
    {
        return horizontalDistance >= 1.5f && horizontalDistance <= 2.8f && rise <= 0.9f && rise >= -2f;
    }

    /// <summary>
    /// Returns the existing parabolic clearance offset above the endpoint interpolation.
    /// Progress runs from zero at takeoff to one at landing; height is measured in metres.
    /// </summary>
    /// <param name="progress">Normalized progress along the arc, from zero to one.</param>
    /// <returns>Clearance offset above the interpolated endpoints in metres.</returns>
    public static float ArcLift(float progress)
    {
        return 0.8f + 4 * progress * (1 - progress) * 0.9f;
    }
}
