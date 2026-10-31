using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Keeps full and discovered floor metadata ready without rebuilding on a visibility toggle.</summary>
internal sealed class OverviewCatalogPair
{
    private readonly OverviewFloorCatalog _full = new();
    private readonly OverviewFloorCatalog _known = new();
    private NavGraph _graph;
    private int _generation;

    /// <summary>Whether both initial lists are complete.</summary>
    public bool Ready
    {
        get; private set;
    }

    /// <summary>Changes when published metadata or its availability changes.</summary>
    public int Revision
    {
        get; private set;
    }

    /// <summary>Complete geometry extents available for hidden thumbnail preparation.</summary>
    public IReadOnlyList<OverviewFloor> AllFloors => Ready ? _full.Floors : Array.Empty<OverviewFloor>();

    /// <summary>Returns only metadata permitted by the selected mode; reading does not schedule work.</summary>
    public IReadOnlyList<OverviewFloor> Visible(bool reveal)
    {
        if (!Ready)
        {
            return Array.Empty<OverviewFloor>();
        }

        return reveal ? _full.Floors : _known.Floors;
    }

    /// <summary>Builds both views cooperatively and publishes them together.</summary>
    public IEnumerable<byte> Build(NavGraph graph, Exploration exploration)
    {
        Ready = false;
        Revision++;
        _graph = graph;
        _generation = exploration.Generation;
        foreach (var step in _full.Build(graph, exploration, true))
        {
            yield return step;
        }

        foreach (var step in _known.Build(graph, exploration, false))
        {
            yield return step;
        }

        Publish(exploration, _generation);
    }

    /// <summary>Maintains the discovered view even while Full is visible.</summary>
    public IEnumerable<byte> Update(Exploration exploration)
    {
        var generation = exploration.Generation;
        var reset = _generation != generation;
        if (reset)
        {
            Ready = false;
            Revision++;
        }

        var work = reset ? _known.Build(_graph, exploration, false) : _known.Update(exploration);
        using (var job = work.GetEnumerator())
        {
            while (generation == exploration.Generation && job.MoveNext())
            {
                yield return job.Current;
            }
        }

        Publish(exploration, generation);
    }

    /// <summary>Finds full geometry for one cached floor independently of its visible crop.</summary>
    public OverviewFloor FullFloor(int floor) => Find(AllFloors, floor);

    /// <summary>Finds the conservative discovered extent, or null for an unknown floor.</summary>
    public OverviewFloor KnownFloor(int floor) => Find(Visible(false), floor);

    private void Publish(Exploration exploration, int generation)
    {
        Ready = generation == exploration.Generation;
        _generation = generation;
        Revision++;
    }

    private static OverviewFloor Find(IReadOnlyList<OverviewFloor> floors, int id)
    {
        for (var i = 0; i < floors.Count; i++)
        {
            if (floors[i].Floor == id)
            {
                return floors[i];
            }
        }

        return null;
    }
}
