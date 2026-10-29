using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Coordinates chest commands, persistence, and navigation requests for the actual selected container.
/// </summary>
internal sealed class ChestActions
{
    private readonly GuideText _text;
    private readonly ChestSelector _selector;
    private readonly ChestSnapshotReader _reader;
    private readonly GraphLocator _locator;
    private readonly SessionLifecycle _lifecycle;
    private readonly UserNotice _notice;
    private readonly ChestNameDialog _nameDialog;

    /// <summary>
    /// Raised when a chest action selects a navigation destination.
    /// </summary>
    public event Action<ChestRecord> DestinationRequested;

    /// <summary>
    /// Connects interactive chest selection, snapshot reading, note entry, persistence, and destination requests.
    /// </summary>
    public ChestActions(
        ChestSelector selector,
        ChestSnapshotReader reader,
        ChestNameDialog nameDialog,
        GraphLocator locator,
        SessionLifecycle lifecycle,
        UserNotice notice,
        GuideText text)
    {
        _text = text;
        _selector = selector;
        _reader = reader;
        _nameDialog = nameDialog;
        _locator = locator;
        _lifecycle = lifecycle;
        _notice = notice;
    }

    /// <summary>
    /// Merges locally observable chest snapshots into the active session without revealing unexplored targets.
    /// </summary>
    public void Observe(DungeonSession session)
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            if (!session.Area.Bounds.Contains(c.transform.position))
            {
                continue;
            }

            var snapshot = _reader.Read(c, false);
            var n = _locator.Snap(session.Scan, UnityPoints.V(snapshot.Position), 4);
            snapshot.Discovered = n >= 0 && session.Exploration.Has(session.Scan.Graph.Nodes[n]);
            session.Chests.Observe(snapshot);
        }
    }

    private ChestRecord Actual(DungeonSession session)
    {
        var selected = _selector.Select(Player.m_localPlayer);
        if (!selected)
        {
            _notice.Show(_text.ChestTargetRequired);
            return null;
        }

        // Explicit interaction may target a chest whose root lies outside imprecise dungeon bounds.
        return session.Chests.Observe(_reader.Read(selected, true));
    }

    /// <summary>
    /// Marks the actual open or aimed chest for collection and saves its state.
    /// </summary>
    public void Mark(DungeonSession session)
    {
        var chest = Actual(session);
        if (chest == null)
        {
            return;
        }

        session.Chests.Mark(chest);
        _lifecycle.Save(session);
        DestinationRequested?.Invoke(chest);
        _notice.Show(_text.CollectChest + chest.label);
    }

    /// <summary>
    /// Confirms collection at the actual chest, preserving unrelated navigation and collection targets.
    /// </summary>
    public ChestRecord Collect(DungeonSession session)
    {
        var chest = Actual(session);
        if (chest == null)
        {
            return null;
        }

        session.Chests.Collect(chest);
        _lifecycle.Save(session);
        _notice.Show("Vyzvednuto: " + chest.label);
        return chest;
    }

    /// <summary>
    /// Opens native note editing for the actual chest and saves accepted text while the session remains active.
    /// </summary>
    public void Name(DungeonSession session, Func<bool> stillActive)
    {
        var chest = Actual(session);
        if (chest == null || !TextInput.instance)
        {
            return;
        }

        _nameDialog.Request(
            chest.label,
            value =>
        {
            if (!stillActive() || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            chest.label = value.Trim();
            session.Chests.Mark(chest);
            _lifecycle.Save(session);
            _notice.Show(_text.ChestSaved + chest.label);
        });
    }
}
