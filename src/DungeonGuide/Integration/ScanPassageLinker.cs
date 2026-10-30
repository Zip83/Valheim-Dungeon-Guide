using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Links ordinary neighboring samples before testing small gaps for each sampled column.
/// </summary>
internal sealed class ScanPassageLinker
{
    private static readonly (int, int)[] s_walkOffsets = { (1, 0), (0, 1), (1, 1), (1, -1) };
    private readonly PassageEvaluator _passages;
    private readonly ScanGapLinker _gaps;

    /// <summary>
    /// Separates ordinary passage evaluation from cooperative gap evaluation.
    /// </summary>
    public ScanPassageLinker(PassageEvaluator passages, ScanGapLinker gaps)
    {
        _passages = passages;
        _gaps = gaps;
    }

    /// <summary>
    /// Accounts for empty-neighbor columns and preserves ordinary-then-gap ordering within each column.
    /// </summary>
    public IEnumerable<object> Link(ScanResult result, FrameWorkBudget work)
    {
        foreach (var column in result.Columns.Keys)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            foreach (var pause in LinkWalks(result, column, work))
            {
                yield return pause;
            }

            foreach (var pause in _gaps.LinkColumn(result, column, work))
            {
                yield return pause;
            }
        }
    }

    private IEnumerable<object> LinkWalks(ScanResult result, (int x, int z) column, FrameWorkBudget work)
    {
        foreach (var pair in NeighborColumnPairs.Enumerate(result.Columns, column, s_walkOffsets))
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            _passages.Connect(result.Graph, pair.from, pair.to);
        }
    }
}
