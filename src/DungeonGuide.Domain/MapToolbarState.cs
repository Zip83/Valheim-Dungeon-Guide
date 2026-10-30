namespace DungeonGuide;

/// <summary>Keeps advanced controls explicitly opened and resets them at each large-map opening.</summary>
internal sealed class MapToolbarState
{
    private bool _wasOpen;

    /// <summary>Whether the player explicitly expanded advanced map controls.</summary>
    public bool Expanded
    {
        get; private set;
    }

    /// <summary>Tracks large-map opening edges without repeatedly closing an already open dropdown.</summary>
    public void SetMapOpen(bool open)
    {
        if (!open || !_wasOpen)
        {
            Collapse();
        }

        _wasOpen = open;
    }

    /// <summary>Toggles advanced controls in response to a user action.</summary>
    public void Toggle() => Expanded = !Expanded;

    /// <summary>Closes advanced controls when switching to the world map.</summary>
    public void Collapse() => Expanded = false;
}
