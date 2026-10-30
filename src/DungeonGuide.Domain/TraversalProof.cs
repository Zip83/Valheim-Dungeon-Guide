using System;

namespace DungeonGuide;

/// <summary>
/// Stores evidence of a successful directional jump or drop using stable save field names.
/// </summary>
[Serializable]
public sealed class TraversalProof
{
    /// <summary>
    /// Recorded takeoff floor position.
    /// </summary>
    public Point from;

    /// <summary>
    /// Recorded landing floor position; proof is directional.
    /// </summary>
    public Point to;

    /// <summary>
    /// Player jump force when the transition was successfully observed.
    /// </summary>
    public float jumpForce;

    /// <summary>
    /// Player jump skill when the transition was successfully observed.
    /// </summary>
    public float jumpSkill;
}
