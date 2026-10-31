using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Draws navigation instructions only while navigation is active.
/// </summary>
internal sealed class NavigationHud
{
    private readonly NavigationInstructionFormatter _formatter;

    /// <summary>
    /// Uses destination/direction formatting for the navigation panel.
    /// </summary>
    public NavigationHud(NavigationInstructionFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Displays active navigation guidance without opening an unsolicited map window.
    /// </summary>
    public void Draw(DungeonSession session, MapViewState view, NavigationCoordinator navigator, string blockedHint)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter
        };
        if (session != null && view.Dungeon && navigator.Goal != NavigationGoal.None && Player.m_localPlayer)
        {
            var x = Screen.width / 2f - 250;
            float y = Screen.height - 170;
            GUI.Box(new Rect(x, y, 500, 78), GUIContent.none);
            GUI.Label(new Rect(x + 8, y + 4, 484, 30), _formatter.Title(session, navigator), style);
            GUI.Label(
                new Rect(x + 8, y + 35, 484, 38),
                _formatter.Instruction(session, navigator, Player.m_localPlayer, blockedHint),
                style);
        }
    }
}
