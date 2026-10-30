using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Distinguishes missing routes, off-graph positions, and exhaustion of known exploration samples.
/// </summary>
internal enum NavigationStatus
{
    /// <summary>
    /// No navigation failure is currently recorded.
    /// </summary>
    None,
    /// <summary>
    /// The player could not be matched to a nearby supported sample.
    /// </summary>
    OffGraph,
    /// <summary>
    /// No currently permitted route reaches the requested goal.
    /// </summary>
    NoRoute,
    /// <summary>
    /// No accessible unexplored sample remains; this is not proof that every room was found.
    /// </summary>
    ExplorationExhausted
}
