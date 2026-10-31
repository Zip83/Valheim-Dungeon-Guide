using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Projects incremental room visits and fog additions onto cached overview coverage.</summary>
internal sealed class RoomVisitRecorder
{
    private readonly Dictionary<(string room, int floor), List<NavNode>> _layers = new();
    private readonly Dictionary<string, DungeonRoom> _rooms = new();
    private readonly Dictionary<string, NavNode> _nodes = new();
    private readonly FrameWorkBudget _budget = new(8192, 0.25);
    private DungeonSession _session;
    private NavGraph _graph;
    private int _visits;
    private int _cursor;
    private IEnumerator<byte> _work;

    /// <summary>Records entries immediately and advances only new coverage under a presentation budget.</summary>
    public void Track(DungeonSession session, Vector3 player)
    {
        RoomEntryRecorder.Track(session, player);
        EnsureGraph(session);
        AdvanceCoverage(session);
    }

    private void EnsureGraph(DungeonSession session)
    {
        if (_session != session || _graph != session.Scan?.Graph)
        {
            _work?.Dispose();
            _session = session;
            _graph = session.Scan?.Graph;
            _layers.Clear();
            _rooms.Clear();
            _nodes.Clear();
            _visits = _cursor = 0;
            _work = InitializeCoverage(session).GetEnumerator();
        }

    }

    private void AdvanceCoverage(DungeonSession session)
    {
        if (_work == null)
        {
            _work = AddCoverage(session).GetEnumerator();
        }

        _budget.BeginFrame();
        while (_work != null && _budget.TryStartWork())
        {
            if (!_work.MoveNext())
            {
                _work.Dispose();
                _work = null;
            }
        }
    }

    /// <summary>Releases the old graph and pending work when the player leaves the session.</summary>
    public void Reset()
    {
        _work?.Dispose();
        _work = null;
        _session = null;
        _graph = null;
        _layers.Clear();
        _rooms.Clear();
        _nodes.Clear();
    }

    private IEnumerable<byte> InitializeCoverage(DungeonSession session)
    {
        foreach (var step in BuildIndex(session))
        {
            yield return step;
        }

        foreach (var step in RestoreKnownCoverage(session))
        {
            yield return step;
        }
    }

    private IEnumerable<byte> BuildIndex(DungeonSession session)
    {
        foreach (var room in session.Area.RoomVolumes)
        {
            _rooms[room.Id] = room;
        }

        if (_graph == null)
        {
            yield break;
        }

        foreach (var node in _graph.Nodes)
        {
            yield return 0;
            _nodes[node.Key] = node;
            foreach (var step in IndexRoomLayers(session.Area, node))
            {
                yield return step;
            }
        }
    }

    private IEnumerable<byte> IndexRoomLayers(DungeonArea area, NavNode node)
    {
        if (node.Floor < 0)
        {
            yield break;
        }

        foreach (var room in area.RoomVolumes)
        {
            yield return 0;
            if (room.Contains(UnityPoints.V(node.Position)))
            {
                AddLayerNode(room.Id, node);
            }
        }
    }

    private void AddLayerNode(string room, NavNode node)
    {
        var key = (room, node.Floor);
        if (!_layers.TryGetValue(key, out var nodes))
        {
            nodes = new List<NavNode>();
            _layers.Add(key, nodes);
        }

        nodes.Add(node);
    }

    private IEnumerable<byte> RestoreKnownCoverage(DungeonSession session)
    {
        if (_graph == null)
        {
            yield break;
        }

        foreach (var node in _graph.Nodes)
        {
            yield return 0;
            if (session.Exploration.Has(node))
            {
                session.OverviewExploration.Reveal(node);
            }
        }
    }

    private IEnumerable<byte> AddCoverage(DungeonSession session)
    {
        foreach (var step in CopyExplorationAdditions(session))
        {
            yield return step;
        }

        var count = session.Data.visitedRooms.Count;
        while (_visits < count)
        {
            yield return 0;
            var visit = session.Data.visitedRooms[_visits++];
            foreach (var node in FindVisitedLayer(session, visit))
            {
                yield return 0;
                session.VisitedRoomCoverage.Reveal(node);
                session.OverviewExploration.Reveal(node);
            }
        }
    }

    private IEnumerable<byte> CopyExplorationAdditions(DungeonSession session)
    {
        var count = session.Exploration.Changes.Count;
        while (_cursor < count)
        {
            yield return 0;
            if (_nodes.TryGetValue(session.Exploration.Changes[_cursor++].Key, out var node))
            {
                session.OverviewExploration.Reveal(node);
            }
        }
    }

    private IEnumerable<NavNode> FindVisitedLayer(DungeonSession session, RoomVisit visit)
    {
        if (_graph == null || visit == null || visit.room == null || !_rooms.TryGetValue(visit.room, out var room))
        {
            return System.Array.Empty<NavNode>();
        }

        var floor = RoomVisitLayerPolicy.Resolve(session.Floors, room.Height(visit.local), session.HasPartialGeometry);
        return floor >= 0 && _layers.TryGetValue((visit.room, floor), out var nodes)
            ? nodes : System.Array.Empty<NavNode>();
    }
}
