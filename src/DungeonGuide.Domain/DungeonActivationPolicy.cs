namespace DungeonGuide;

/// <summary>
/// Restricts dungeon sessions to interiors unless the player explicitly enables outdoor manual scans.
/// </summary>
public sealed class DungeonActivationPolicy
{
    /// <summary>
    /// Allows a requested manual scan in an interior, or outdoors only with explicit permission.
    /// </summary>
    /// <param name="inInterior">Whether the game classifies the player's position as an interior.</param>
    /// <param name="allowOutdoorMapping">Explicit permission to map outdoor areas, including player bases.</param>
    /// <returns>Whether a manual scan may create a session at the player's position.</returns>
    public bool CanScanManually(bool inInterior, bool allowOutdoorMapping = false)
    {
        return inInterior || allowOutdoorMapping;
    }

    /// <summary>
    /// Rejects sessions after leaving their bounds or interior; outdoor permission applies only to manual areas.
    /// </summary>
    /// <param name="isManualArea">Whether the session was explicitly created by a manual scan.</param>
    /// <param name="insideBounds">Whether the player is still inside the session's scan bounds.</param>
    /// <param name="inInterior">Whether the game classifies the player's position as an interior.</param>
    /// <param name="allowOutdoorMapping">Explicit permission to retain manual sessions outdoors.</param>
    /// <returns>Whether the current session remains eligible for mapping.</returns>
    public bool CanRetainSession(
        bool isManualArea,
        bool insideBounds,
        bool inInterior,
        bool allowOutdoorMapping = false)
    {
        return insideBounds && (inInterior || isManualArea && allowOutdoorMapping);
    }
}
