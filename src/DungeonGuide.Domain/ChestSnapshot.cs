using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>
/// Carries a single locally observed container identity, position, and inventory summary.
/// </summary>
public sealed class ChestSnapshot
{
    /// <summary>
    /// Stable network identifier when available, otherwise a position-based identifier.
    /// </summary>
    public string Id;

    /// <summary>
    /// Position identifier used to match records saved before network identities were stored.
    /// </summary>
    public string LegacyId;

    /// <summary>
    /// Locally available inventory summary, or null when the inventory could not be inspected.
    /// </summary>
    public string Contents;

    /// <summary>
    /// Observed world-space container position.
    /// </summary>
    public Point Position;

    /// <summary>
    /// Whether the container lies in explored geometry or was explicitly selected.
    /// </summary>
    public bool Discovered;
}
