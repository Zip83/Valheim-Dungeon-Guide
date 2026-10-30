using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Draws navigation instructions and short notices only when their associated activity is enabled.
/// </summary>
internal sealed class NavigationHud
{
    private readonly UserNotice _notice;
    private readonly NavigationInstructionFormatter _formatter;

    /// <summary>
    /// Uses transient notices and destination/direction formatting for the navigation panel.
    /// </summary>
    public NavigationHud(UserNotice notice, NavigationInstructionFormatter formatter)
    {
        _notice = notice;
        _formatter = formatter;
    }

    /// <summary>
    /// Displays active navigation guidance and transient notices without opening an unsolicited map window.
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

        if (Time.unscaledTime < _notice.Until)
        {
            GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height - 235, 600, 55), _notice.Text, style);
        }
    }
}
