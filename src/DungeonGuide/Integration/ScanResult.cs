using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Holds sampled floor geometry, its horizontal lookup index, and scan completeness metadata.
/// </summary>
internal sealed class ScanResult
{
    /// <summary>
    /// Completed sampled geometry and its directed passages.
    /// </summary>
    public readonly NavGraph Graph = new();

    /// <summary>
    /// Horizontal grid index grouping vertically stacked samples for lookup.
    /// </summary>
    public readonly Dictionary<(int, int), List<int>> Columns = new();

    /// <summary>
    /// Horizontal sampling-grid distance in metres.
    /// </summary>
    public float Spacing;

    /// <summary>
    /// Whether the scan stopped at its configured column cap.
    /// </summary>
    public bool Truncated;
}
