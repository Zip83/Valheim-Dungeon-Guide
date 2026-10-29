namespace DungeonGuide;

/// <summary>
/// Identifies the interactive source selected for a chest command.
/// </summary>
public enum ChestTargetKind
{
    /// <summary>
    /// No open or aimed container satisfies the game interaction checks.
    /// </summary>
    None,
    /// <summary>
    /// The visible open inventory belongs to an in-range container.
    /// </summary>
    Open,
    /// <summary>
    /// The interaction crosshair identifies an in-range container.
    /// </summary>
    Aimed
}
