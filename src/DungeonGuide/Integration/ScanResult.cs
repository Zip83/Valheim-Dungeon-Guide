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
    public readonly NavGraph Graph;

    /// <summary>
    /// Horizontal grid index grouping vertically stacked samples for lookup.
    /// </summary>
    public readonly Dictionary<(int, int), List<int>> Columns = new();

    /// <summary>
    /// Directional jump/drop edges indexed during linking, avoiding a synchronous full-graph pass at publication.
    /// </summary>
    public readonly Dictionary<(string, string), NavEdge> TraversalCandidates = new();

    /// <summary>
    /// Horizontal sampling-grid distance in metres.
    /// </summary>
    public float Spacing;

    /// <summary>Recognized destructible identities used to refresh opened passages without a full floor scan.</summary>
    public readonly SpecialPassageIndex SpecialPassages = new();

    /// <summary>Worker-selected endpoint candidates, consumed only by the private linking phase.</summary>
    public PassagePairCatalog Pairs;

    /// <summary>
    /// Whether a work cap or saturated physics query made the scan potentially incomplete.
    /// </summary>
    public bool Truncated;

    /// <summary>Creates private scan indexes around a new or exclusively owned restored graph.</summary>
    public ScanResult(NavGraph graph = null)
    {
        Graph = graph ?? new NavGraph();
    }

}
