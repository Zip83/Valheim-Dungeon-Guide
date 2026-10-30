using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Keeps dungeon visibility, exploration mode, and manual floor browsing separate from player position.
/// </summary>
public sealed class MapViewState
{
    /// <summary>
    /// Whether native maps display the dungeon layer instead of the world map.
    /// </summary>
    public bool Dungeon = true;

    /// <summary>
    /// Whether the player explicitly enabled the optional standalone map overlay.
    /// </summary>
    public bool Overlay;

    /// <summary>
    /// Whether the selected floor displays all scanned geometry rather than explored samples only.
    /// </summary>
    public bool RevealAll;

    /// <summary>
    /// Detected floor containing the player, updated with midpoint hysteresis.
    /// </summary>
    public int CurrentFloor
    {
        get; private set;
    }

    private int? _manualFloor;

    /// <summary>
    /// Whether verified breakable passages may be considered by navigation.
    /// </summary>
    public bool AllowBreakables;

    /// <summary>
    /// Whether previously verified jump and drop passages may be considered by navigation.
    /// </summary>
    public bool AllowJumps;

    /// <summary>
    /// Manual browsing selection when present, otherwise the tracked player floor.
    /// </summary>
    public int SelectedFloor => _manualFloor ?? CurrentFloor;

    /// <summary>
    /// Resets browsing and map mode for a newly entered dungeon without opening the optional overlay.
    /// </summary>
    public void Enter()
    {
        Dungeon = true;
        _manualFloor = null;
        CurrentFloor = 0;
    }

    /// <summary>
    /// Updates the player floor while stabilizing stair midpoint changes and clamping stale manual selections.
    /// </summary>
    /// <param name="levels">Detected floor elevations ordered from lowest to highest.</param>
    /// <param name="y">Player or sample elevation in world metres.</param>
    public void Track(IReadOnlyList<float> levels, float y)
    {
        if (levels.Count == 0)
        {
            return;
        }

        CurrentFloor = Math.Min(CurrentFloor, levels.Count - 1);
        if (_manualFloor.HasValue)
        {
            _manualFloor = Math.Min(_manualFloor.Value, levels.Count - 1);
        }

        var next = FloorDetector.Nearest(levels, y);
        if (Math.Abs(y - levels[next]) + 0.65f < Math.Abs(y - levels[CurrentFloor]))
        {
            CurrentFloor = next;
        }
    }

    /// <summary>Preserves physical floor selections when a preview is replaced by a fuller scan.</summary>
    /// <param name="previous">Elevations used by the previous published scan.</param>
    /// <param name="replacement">Elevations used by the replacement scan.</param>
    public void RemapFloors(IReadOnlyList<float> previous, IReadOnlyList<float> replacement)
    {
        if (previous.Count == 0 || replacement.Count == 0)
        {
            return;
        }

        var currentHeight = previous[Math.Min(CurrentFloor, previous.Count - 1)];
        CurrentFloor = FloorDetector.Nearest(replacement, currentHeight);
        if (_manualFloor.HasValue)
        {
            var selectedHeight = previous[Math.Min(_manualFloor.Value, previous.Count - 1)];
            _manualFloor = FloorDetector.Nearest(replacement, selectedHeight);
        }
    }

    /// <summary>
    /// Moves the manual browsing selection by a relative offset, clamped to available floors.
    /// </summary>
    /// <param name="delta">Relative browsing offset; positive selects a higher floor.</param>
    /// <param name="count">Number of floors currently available.</param>
    public void ChangeFloor(int delta, int count)
    {
        if (count > 0)
        {
            _manualFloor = Math.Max(0, Math.Min(count - 1, SelectedFloor + delta));
        }
    }

    /// <summary>
    /// Clears manual browsing so the selected floor follows the player again.
    /// </summary>
    public void Follow()
    {
        _manualFloor = null;
    }
}
