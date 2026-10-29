using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Builds a navigation graph from loaded colliders under a bounded per-frame scan budget.
/// </summary>
internal sealed class GeometryScanner
{
    private static readonly (int, int)[] s_walkOffsets =
    {
        (1, 0),
        (0, 1),
        (1, 1),
        (1, -1)
    };

    private static readonly (int, int)[] s_gapOffsets =
    {
        (2, 0),
        (0, 2),
        (-2, 0),
        (0, -2)
    };

    private readonly GeometryPhysics _physics;
    private readonly PassageEvaluator _passages;
    private readonly JumpEvaluator _jumps;

    /// <summary>
    /// Connects floor sampling with ordinary passage and jump-candidate evaluation.
    /// </summary>
    public GeometryScanner(GeometryPhysics physics, PassageEvaluator passages, JumpEvaluator jumps)
    {
        _physics = physics;
        _passages = passages;
        _jumps = jumps;
    }

    /// <summary>
    /// Samples loaded geometry over multiple frames and invokes completion with graph and truncation metadata.
    /// </summary>
    public IEnumerator Scan(DungeonArea area, float spacing, int limit, int budget, Action<ScanResult> complete)
    {
        var result = new ScanResult
        {
            Spacing = spacing
        };
        var bounds = area.Bounds;
        var x0 = Mathf.FloorToInt(bounds.min.x / spacing);
        var x1 = Mathf.CeilToInt(bounds.max.x / spacing);
        var z0 = Mathf.FloorToInt(bounds.min.z / spacing);
        var z1 = Mathf.CeilToInt(bounds.max.z / spacing);
        var count = 0;
        var work = 0;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (var x = x0; x <= x1; x++)
        {
            for (var z = z0; z <= z1; z++)
            {
                if (count++ >= limit || result.Graph.Nodes.Count >= 100000)
                {
                    result.Truncated = true;
                    goto Link;
                }

                Column(area, result, x, z);
                if (++work >= budget || timer.ElapsedMilliseconds >= 4)
                {
                    work = 0;
                    yield return null;
                    timer.Restart();
                }
            }
        }

    Link:
        foreach (var col in result.Columns)
        {
            foreach (var offset in s_walkOffsets)
            {
                if (result.Columns.TryGetValue(
                    (col.Key.Item1 + offset.Item1, col.Key.Item2 + offset.Item2),
                    out var neighbours))
                {
                    foreach (var a in col.Value)
                    {
                        foreach (var b in neighbours)
                        {
                            _passages.Connect(result.Graph, a, b);
                        }
                    }
                }
            }

            // Only test cardinal small gaps. Never invent a reverse edge for a drop.
            foreach (var offset in s_gapOffsets)
            {
                if (result.Columns.TryGetValue(
                    (col.Key.Item1 + offset.Item1, col.Key.Item2 + offset.Item2),
                    out var neighbours))
                {
                    foreach (var a in col.Value)
                    {
                        foreach (var b in neighbours)
                        {
                            var pa = UnityPoints.V(result.Graph.Nodes[a].Position);
                            var pb = UnityPoints.V(result.Graph.Nodes[b].Position);
                            if (result.Graph.Nodes[a].Edges.Exists(e => e.To == b))
                            {
                                continue;
                            }

                            // A closed door or grate can occupy the missing intermediate grid cell.
                            // Evaluate it as a supported passage before considering an airborne gap.
                            _passages.Connect(result.Graph, a, b);
                            if (result.Graph.Nodes[a].Edges.Exists(e => e.To == b) || _physics.Supported((pa + pb) * 0.5f))
                            {
                                continue;
                            }

                            if (_jumps.Candidate(pa, pb))
                            {
                                result.Graph.Connect(a, b, pb.y < pa.y - 0.6f ? PassageKind.Drop : PassageKind.Jump, false, false);
                            }
                        }
                    }
                }
            }

            if (++work >= budget || timer.ElapsedMilliseconds >= 4)
            {
                work = 0;
                yield return null;
                timer.Restart();
            }
        }

        complete(result);
    }

    private void Column(DungeonArea area, ScanResult result, int x, int z)
    {
        var top = area.Bounds.max.y + 1;
        var bottom = area.Bounds.min.y - 1;
        var heights = new List<float>();
        var count = 0;
        while (top > bottom && count++ < 160)
        {
            if (!Physics.Raycast(
                new Vector3(x * result.Spacing, top, z * result.Spacing),
                Vector3.down,
                out var hit,
                top - bottom,
                _physics.Mask,
                QueryTriggerInteraction.Ignore))
            {
                break;
            }

            top = hit.point.y - 0.08f;
            if (hit.normal.y < 0.6f || _physics.Excluded(hit.collider)
                || heights.Any(y => Mathf.Abs(y - hit.point.y) < 0.55f))
            {
                continue;
            }

            if (area.Rooms.Length > 0 && !area.Rooms.Any(b => b.SqrDistance(hit.point) < 4))
            {
                continue;
            }

            if (!_physics.Standing(hit.point))
            {
                continue;
            }

            heights.Add(hit.point.y);
            var id = result.Graph.Nodes.Count;
            result.Graph.Nodes.Add(new NavNode(UnityPoints.P(hit.point), hit.normal.y > 0.97f));
            if (!result.Columns.TryGetValue((x, z), out var list))
            {
                result.Columns[(x, z)] = list = new List<int>();
            }

            list.Add(id);
        }

        if (count >= 160)
        {
            result.Truncated = true;
        }
    }
}
