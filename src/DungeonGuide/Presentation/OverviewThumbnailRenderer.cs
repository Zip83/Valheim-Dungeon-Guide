using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Keeps a mode-independent full-floor image and an optional sharper discovered-area projection.</summary>
internal sealed class OverviewThumbnailRenderer : IDisposable
{
    private readonly ProjectedOverviewSurface _full = new();
    private readonly ProjectedOverviewSurface _known = new();
    private OverviewFrame _knownFrame;
    private bool _hasKnown;
    private bool _reveal;

    private bool UsesKnown => !_reveal && _hasKnown && !_knownFrame.Matches(_full.Raster)
        && _knownFrame.Matches(_known.Raster);

    private ProjectedOverviewSurface Selected => UsesKnown ? _known : _full;

    /// <summary>Completed image selected without recreating resources on a mode change.</summary>
    public Texture2D Texture => Selected.Texture;

    /// <summary>Conservative crop of the known extent while the full-floor fallback is displayed.</summary>
    public Rect Viewport
    {
        get
        {
            if (_reveal || !_hasKnown || Selected.Raster == null)
            {
                return new Rect(0, 0, 1, 1);
            }

            var crop = _knownFrame.Crop(Selected.Raster);
            return new Rect(crop.X, crop.Y, crop.Size, crop.Size);
        }
    }

    /// <summary>Changes ready image selection and preserves the discovered-only projection.</summary>
    public void SetMode(bool reveal)
    {
        _reveal = reveal;
        _full.SetMode(reveal);
        _known.SetMode(false);
    }

    /// <summary>Updates framing metadata without starting geometry or texture work.</summary>
    public void SetKnownFrame(OverviewFloor known, float spacing)
    {
        _hasKnown = known != null;
        if (_hasKnown)
        {
            _knownFrame = new OverviewFrame(known, spacing);
        }
    }

    /// <summary>Prioritizes the displayed projection and uses spare budget for the other prepared view.</summary>
    public void Tick(DungeonSession session, OverviewFloor full, OverviewFloor known,
        bool reveal, FrameWorkBudget budget)
    {
        SetMode(reveal);
        SetKnownFrame(known, session.Scan.Spacing);
        var fullFrame = new OverviewFrame(full, session.Scan.Spacing);
        var primaryKnown = UsesKnown;
        var primary = primaryKnown ? _known : _full;
        primary.Tick(session, primaryKnown ? _knownFrame : fullFrame, reveal && !primaryKnown, budget);
        AdvanceSecondary(session, fullFrame, primaryKnown, budget);
    }

    private void AdvanceSecondary(DungeonSession session, OverviewFrame fullFrame,
        bool primaryKnown, FrameWorkBudget budget)
    {
        if (!_full.Ready || !_hasKnown || _knownFrame.Matches(_full.Raster) || !budget.TryStartWork())
        {
            return;
        }

        if (primaryKnown)
        {
            _full.Tick(session, fullFrame, false, budget);
        }
        else
        {
            _known.Tick(session, _knownFrame, false, budget);
        }
    }

    /// <summary>Releases both projections and any unfinished replacements.</summary>
    public void Dispose()
    {
        _full.Dispose();
        _known.Dispose();
    }
}
