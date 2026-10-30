namespace DungeonGuide;

/// <summary>
/// Selects an actual interactive chest without guessing from proximity.
/// </summary>
public sealed class ChestTargetPolicy
{
    /// <summary>
    /// Prefers a visible in-range open chest, then an interactive aimed chest, otherwise no target.
    /// </summary>
    /// <param name="openVisible">Whether the native container inventory is currently visible.</param>
    /// <param name="openInRange">Whether the open container satisfies the native interaction range.</param>
    /// <param name="aimedInteractive">Whether the interaction prompt identifies an aimed container.</param>
    /// <param name="aimedInRange">Whether that aimed container satisfies native interaction range.</param>
    /// <returns>The selected open or aimed source, or None when neither is interactive.</returns>
    public ChestTargetKind Select(bool openVisible, bool openInRange, bool aimedInteractive, bool aimedInRange)
    {
        if (openVisible && openInRange)
        {
            return ChestTargetKind.Open;
        }

        if (aimedInteractive && aimedInRange)
        {
            return ChestTargetKind.Aimed;
        }

        return ChestTargetKind.None;
    }
}
