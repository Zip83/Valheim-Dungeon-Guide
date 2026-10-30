using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Schedules the floor catalog and cached thumbnails under one cooperative overview budget.</summary>
internal sealed class DungeonOverviewRenderer : IDisposable
{
    private readonly FrameWorkBudget _budget = new(8192, 1);
    private readonly Dictionary<int, OverviewThumbnailRenderer> _thumbnails = new();

    private DungeonSession _session;
    private NavGraph _graph;
    private bool _revealAll;
    private int _explorationRevision = -1;
    private int _cursor;
    private OverviewFloorCatalog _catalog;
    private IEnumerator<byte> _catalogJob;

    /// <summary>Visible floors from the most recent completed classification.</summary>
    public IReadOnlyList<OverviewFloor> Floors { get; private set; } = Array.Empty<OverviewFloor>();

    /// <summary>Revision for reconciling the visible cards after classification or a reveal-mode reset.</summary>
    public int Revision
    {
        get; private set;
    }

    /// <summary>Whether the first visible-floor classification is still running.</summary>
    public bool Preparing => _catalogJob != null && Floors.Count == 0;

    /// <summary>Returns a completed thumbnail without allocating or performing rendering work.</summary>
    public Texture2D Texture(int floor) => _thumbnails.TryGetValue(floor, out var thumbnail) ? thumbnail.Texture : null;

    /// <summary>Rejects a card from an older scan while a replacement graph is awaiting UI refresh.</summary>
    public bool CanSelect(int floor)
    {
        if (_graph == null || _session?.Scan?.Graph != _graph)
        {
            return false;
        }

        foreach (var entry in Floors)
        {
            if (entry.Floor == floor)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Advances the current mode; changing from full to explored immediately discards full images.</summary>
    public void Tick(DungeonSession session, bool revealAll)
    {
        if (session?.Scan == null)
        {
            Dispose();
            return;
        }

        if (_session != session || _graph != session.Scan.Graph || _revealAll != revealAll)
        {
            Reset(session, revealAll);
        }

        _budget.BeginFrame();
        UpdateCatalog(session);
        UpdateThumbnail(session);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _catalogJob?.Dispose();
        _catalogJob = null;
        _catalog = null;

        foreach (var thumbnail in _thumbnails.Values)
        {
            thumbnail.Dispose();
        }

        _thumbnails.Clear();

        _session = null;
        _graph = null;
        Floors = Array.Empty<OverviewFloor>();
        _explorationRevision = -1;
        _cursor = 0;
        Revision++;
    }

    private void Reset(DungeonSession session, bool revealAll)
    {
        Dispose();

        _session = session;
        _graph = session.Scan.Graph;
        _revealAll = revealAll;
    }

    private void UpdateCatalog(DungeonSession session)
    {
        if (_catalogJob == null
            && (_catalog == null || !_revealAll && _explorationRevision != session.Exploration.Revision))
        {
            _explorationRevision = session.Exploration.Revision;
            _catalog = new OverviewFloorCatalog();
            _catalogJob = _catalog.Build(_graph, session.Exploration, _revealAll).GetEnumerator();
        }

        while (_catalogJob != null && _budget.TryStartWork())
        {
            if (!_catalogJob.MoveNext())
            {
                Floors = _catalog.Floors;
                _catalogJob.Dispose();
                _catalogJob = null;
                Revision++;
            }
        }
    }

    private void UpdateThumbnail(DungeonSession session)
    {
        if (Floors.Count == 0)
        {
            return;
        }

        var floor = Floors[_cursor++ % Floors.Count];
        if (!_thumbnails.TryGetValue(floor.Floor, out var thumbnail))
        {
            thumbnail = new OverviewThumbnailRenderer();
            _thumbnails.Add(floor.Floor, thumbnail);
        }

        thumbnail.Tick(session, floor, _revealAll, _budget);
    }
}
