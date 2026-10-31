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
        if (_session != session || _graph != session.Scan?.Graph)
        {
            _work?.Dispose();
            _session = session;
            _graph = session.Scan?.Graph;
            _layers.Clear();
            _rooms.Clear();
            _nodes.Clear();
            _visits = _cursor = 0;
            _work = BuildIndex(session).GetEnumerator();
        }

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
            if (session.Exploration.Has(node))
            {
                session.OverviewExploration.Reveal(node);
            }

            if (node.Floor < 0)
            {
                continue;
            }

            foreach (var room in session.Area.RoomVolumes)
            {
                yield return 0;
                if (room.Contains(UnityPoints.V(node.Position)))
                {
                    var key = (room.Id, node.Floor);
                    if (!_layers.TryGetValue(key, out var nodes))
                    {
                        nodes = new List<NavNode>();
                        _layers.Add(key, nodes);
                    }

                    nodes.Add(node);
                }
            }
        }
    }

    private IEnumerable<byte> AddCoverage(DungeonSession session)
    {
        while (_cursor < session.Exploration.Changes.Count)
        {
            yield return 0;
            if (_nodes.TryGetValue(session.Exploration.Changes[_cursor++].Key, out var node))
            {
                session.OverviewExploration.Reveal(node);
            }
        }

        if (_graph == null || session.Floors.Count == 0)
        {
            yield break;
        }

        while (_visits < session.Data.visitedRooms.Count)
        {
            yield return 0;
            var visit = session.Data.visitedRooms[_visits++];
            if (visit == null || visit.room == null || !_rooms.TryGetValue(visit.room, out var room))
            {
                continue;
            }

            var height = room.Height(visit.local);
            var floor = RoomVisitLayerPolicy.Resolve(session.Floors, height, session.HasPartialGeometry);
            if (floor < 0)
            {
                continue;
            }

            if (_layers.TryGetValue((visit.room, floor), out var nodes))
            {
                foreach (var node in nodes)
                {
                    yield return 0;
                    session.VisitedRoomCoverage.Reveal(node);
                    session.OverviewExploration.Reveal(node);
                }
            }
        }
    }
}
