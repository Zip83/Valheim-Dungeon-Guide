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
    private int _visibleCursor;
    private int _allCursor;
    private int _revisionBase;
    private bool _preloadAll;
    private OverviewCatalogPair _catalog;
    private IEnumerator<byte> _catalogJob;

    /// <summary>Only metadata permitted by the active visibility mode.</summary>
    public IReadOnlyList<OverviewFloor> Floors => _catalog?.Visible(_revealAll) ?? Array.Empty<OverviewFloor>();

    /// <summary>Geometry metadata used to prepare hidden cards without making them selectable.</summary>
    public IReadOnlyList<OverviewFloor> AllFloors => _catalog?.AllFloors ?? Array.Empty<OverviewFloor>();

    /// <summary>Reconciles visible cards after catalog publication or mode changes.</summary>
    public int Revision => _revisionBase + (_catalog?.Revision ?? 0);

    /// <summary>Whether the paired initial catalog is still being prepared.</summary>
    public bool Preparing => _catalog == null || !_catalog.Ready;

    /// <summary>Returns a completed thumbnail without allocating or performing rendering work.</summary>
    public Texture2D Texture(int floor) => _thumbnails.TryGetValue(floor, out var thumbnail) ? thumbnail.Texture : null;

    /// <summary>Returns framing paired with the selected cached thumbnail.</summary>
    public Rect Viewport(int floor) => _thumbnails.TryGetValue(floor, out var thumbnail)
        ? thumbnail.Viewport : new Rect(0, 0, 1, 1);

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

    /// <summary>Changes visible metadata and ready image selection without disposing cached floors.</summary>
    public void Tick(DungeonSession session, bool revealAll)
    {
        if (session?.Scan == null)
        {
            Dispose();
            return;
        }

        if (_session != session || _graph != session.Scan.Graph)
        {
            Reset(session);
        }

        SetMode(session, revealAll);
        _budget.BeginFrame();
        UpdateCatalog(session);
        UpdateThumbnail(session);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _revisionBase = Revision + 1;
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
        _explorationRevision = -1;
        _visibleCursor = _allCursor = 0;
        _preloadAll = false;
    }

    private void Reset(DungeonSession session)
    {
        Dispose();

        _session = session;
        _graph = session.Scan.Graph;
    }

    private void SetMode(DungeonSession session, bool revealAll)
    {
        if (_revealAll == revealAll)
        {
            return;
        }

        _revealAll = revealAll;
        _revisionBase++;
        foreach (var pair in _thumbnails)
        {
            pair.Value.SetKnownFrame(_catalog?.KnownFloor(pair.Key), session.Scan.Spacing);
            pair.Value.SetMode(revealAll);
        }
    }

    private void UpdateCatalog(DungeonSession session)
    {
        if (_catalogJob == null
            && (_catalog == null || _explorationRevision != session.OverviewExploration.Revision))
        {
            _explorationRevision = session.OverviewExploration.Revision;
            if (_catalog == null)
            {
                _catalog = new OverviewCatalogPair();
                _catalogJob = _catalog.Build(_graph, session.OverviewExploration).GetEnumerator();
            }
            else
            {
                _catalogJob = _catalog.Update(session.OverviewExploration).GetEnumerator();
            }
        }

        while (_catalogJob != null && _budget.TryStartWork())
        {
            if (!_catalogJob.MoveNext())
            {
                _catalogJob.Dispose();
                _catalogJob = null;
            }
        }
    }

    private void UpdateThumbnail(DungeonSession session)
    {
        if (AllFloors.Count == 0)
        {
            return;
        }

        var current = Player.m_localPlayer && session.Floors.Count > 0
            ? FloorDetector.Nearest(session.Floors, Player.m_localPlayer.transform.position.y) : -1;
        var playerFloor = _catalog.FullFloor(current);
        if (playerFloor != null)
        {
            AdvanceThumbnail(session, playerFloor);
        }

        AdvanceThumbnail(session, NextThumbnail());
    }

    private OverviewFloor NextThumbnail()
    {
        var all = _revealAll || _preloadAll || Floors.Count == 0;
        _preloadAll = !_preloadAll;
        var floors = all ? AllFloors : Floors;
        var cursor = all ? _allCursor : _visibleCursor;
        var index = cursor % floors.Count;
        if (all)
        {
            _allCursor = (index + 1) % floors.Count;
        }
        else
        {
            _visibleCursor = (index + 1) % floors.Count;
        }

        return _catalog.FullFloor(floors[index].Floor);
    }

    private void AdvanceThumbnail(DungeonSession session, OverviewFloor floor)
    {
        if (!_budget.TryStartWork())
        {
            return;
        }

        if (!_thumbnails.TryGetValue(floor.Floor, out var thumbnail))
        {
            thumbnail = new OverviewThumbnailRenderer();
            _thumbnails.Add(floor.Floor, thumbnail);
        }

        thumbnail.Tick(session, floor, _catalog.KnownFloor(floor.Floor), _revealAll, _budget);
    }
}
