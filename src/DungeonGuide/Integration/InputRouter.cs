using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Translates configured shortcuts into commands while respecting active game input dialogs.
/// </summary>
internal sealed class InputRouter
{
    private readonly GuideSettings _settings;
    private readonly Action<GuideCommand> _command;

    /// <summary>
    /// Uses configured shortcuts and the supplied command dispatcher without owning navigation behavior.
    /// </summary>
    public InputRouter(GuideSettings settings, Action<GuideCommand> command)
    {
        _settings = settings;
        _command = command;
    }

    /// <summary>
    /// Dispatches configured shortcuts unless native dialogs or game input ownership prevent them.
    /// </summary>
    public void Tick(bool mapVisible)
    {
        if (!Player.m_localPlayer || TextInput.IsVisible() || Chat.instance && Chat.instance.HasFocus())
        {
            return;
        }

        if (_settings.World.Value.IsDown())
        {
            _command(GuideCommand.ToggleWorld);
        }

        if (_settings.Overlay.Value.IsDown())
        {
            _command(GuideCommand.ToggleOverlay);
        }

        if (!DispatchLocationShortcuts())
        {
            DispatchChestShortcuts();
            DispatchNavigationShortcuts();
        }

        if (mapVisible)
        {
            DispatchFloorShortcuts();
        }
    }

    private bool DispatchLocationShortcuts()
    {
        if (_settings.RemoveLocation.Value.IsDown())
        {
            _command(GuideCommand.RemoveLocation);
        }
        else if (_settings.RenameLocation.Value.IsDown())
        {
            _command(GuideCommand.RenameLocation);
        }
        else if (_settings.MarkLocation.Value.IsDown())
        {
            _command(GuideCommand.MarkLocation);
        }
        else if (_settings.NextLocation.Value.IsDown())
        {
            _command(GuideCommand.NextLocation);
        }
        else
        {
            return false;
        }

        return true;
    }

    private void DispatchChestShortcuts()
    {
        if (_settings.Name.Value.IsDown())
        {
            _command(GuideCommand.NameChest);
        }
        else if (_settings.Collected.Value.IsDown())
        {
            _command(GuideCommand.CollectChest);
        }
        else if (_settings.Mark.Value.IsDown())
        {
            _command(GuideCommand.MarkChest);
        }
    }

    private void DispatchNavigationShortcuts()
    {
        if (_settings.NextChest.Value.IsDown())
        {
            _command(GuideCommand.NextChest);
        }

        if (_settings.Stop.Value.IsDown())
        {
            _command(GuideCommand.Stop);
        }
        else if (_settings.Explore.Value.IsDown())
        {
            _command(GuideCommand.Explore);
        }

        if (_settings.Return.Value.IsDown())
        {
            _command(GuideCommand.Entrance);
        }
    }

    private void DispatchFloorShortcuts()
    {
        if (Minimap.IsOpen() && Input.GetKeyDown(KeyCode.End))
        {
            _command(GuideCommand.ToggleOverview);
        }

        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            _command(GuideCommand.FloorUp);
        }

        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            _command(GuideCommand.FloorDown);
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            _command(GuideCommand.Follow);
        }
    }
}
