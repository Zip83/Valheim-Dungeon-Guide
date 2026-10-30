using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Collects distinct standable floor samples from one vertical ray column.
/// </summary>
internal sealed class ScanColumnSampler
{
    private readonly GeometryPhysics _physics;

    /// <summary>
    /// Uses the shared physics filters, buffers, and overflow accounting.
    /// </summary>
    /// <param name="physics">Shared collision queries and geometry filters.</param>
    public ScanColumnSampler(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>
    /// Filters ray hits and records supported floor points, yielding before each expensive query.
    /// </summary>
    /// <param name="area">Scan bounds and optional loaded-room constraints.</param>
    /// <param name="result">Private graph and column index receiving accepted samples.</param>
    /// <param name="x">Horizontal X grid coordinate, multiplied by the sample spacing.</param>
    /// <param name="z">Horizontal Z grid coordinate, multiplied by the sample spacing.</param>
    /// <param name="work">Budget shared with the other scan phases.</param>
    /// <returns>Frame pauses while the vertical column is sampled and checked for standing clearance.</returns>
    public IEnumerable<object> Sample(
        DungeonArea area, ScanResult result, int x, int z, FrameWorkBudget work)
    {
        var heights = new List<float>();
        foreach (var hit in CastColumn(area, result, x, z, work))
        {
            if (!hit.HasValue)
            {
                yield return null;
                continue;
            }

            if (!IsFloorSurface(hit.Value, area, heights))
            {
                continue;
            }

            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            if (_physics.Standing(hit.Value.point))
            {
                heights.Add(hit.Value.point.y);
                RecordSample(result, x, z, hit.Value);
            }
        }
    }

    private IEnumerable<RaycastHit?> CastColumn(
        DungeonArea area, ScanResult result, int x, int z, FrameWorkBudget work)
    {
        var top = area.Bounds.max.y + 1;
        var bottom = area.Bounds.min.y - 1;
        var count = 0;
        while (top > bottom && count++ < 160)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            if (!CastDown(x * result.Spacing, z * result.Spacing, top, bottom, out var hit))
            {
                break;
            }

            top = hit.point.y - 0.08f;
            yield return hit;
        }

        result.Truncated |= count >= 160;
    }

    private bool CastDown(float x, float z, float top, float bottom, out RaycastHit hit)
    {
        return Physics.Raycast(new Vector3(x, top, z), Vector3.down, out hit, top - bottom,
            _physics.Mask, QueryTriggerInteraction.Ignore);
    }

    private bool IsFloorSurface(RaycastHit hit, DungeonArea area, List<float> heights)
    {
        return hit.normal.y >= 0.6f && !_physics.Excluded(hit.collider)
            && !heights.Any(y => Mathf.Abs(y - hit.point.y) < 0.55f)
            && (area.Rooms.Length == 0 || area.Rooms.Any(bounds => bounds.SqrDistance(hit.point) < 4));
    }

    private static void RecordSample(ScanResult result, int x, int z, RaycastHit hit)
    {
        var id = result.Graph.Nodes.Count;
        result.Graph.Nodes.Add(new NavNode(UnityPoints.P(hit.point), hit.normal.y > 0.97f));
        if (!result.Columns.TryGetValue((x, z), out var column))
        {
            column = new List<int>();
            result.Columns[(x, z)] = column;
        }

        column.Add(id);
    }
}
