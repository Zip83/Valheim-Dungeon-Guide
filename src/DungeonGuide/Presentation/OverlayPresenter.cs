using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Draws the standalone floor map only after the player explicitly enables it.
/// </summary>
internal sealed class OverlayPresenter
{
    private readonly MapTextureRenderer _renderer;
    private readonly FloorNumbering _numbering;

    /// <summary>
    /// Uses the shared floor texture and floor-label formatter for a manually enabled map.
    /// </summary>
    public OverlayPresenter(MapTextureRenderer renderer, FloorNumbering numbering)
    {
        _renderer = renderer;
        _numbering = numbering;
    }

    /// <summary>
    /// Draws a floor map only when the player manually enabled the standalone overlay.
    /// </summary>
    public void Draw(DungeonSession session, MapViewState state)
    {
        if (!state.Overlay || session == null || !state.Dungeon || Minimap.IsOpen())
        {
            return;
        }

        var old = GUI.matrix;
        var scale = Mathf.Min(1, Screen.height / 800f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUI.Box(new Rect(20, 90, 470, 530), GUIContent.none);
        GUI.Label(
            new Rect(32, 99, 440, 30),
            session.Scan == null
                ? GuideText.PreparingMap
                : _numbering.Label(session.Floors, state.SelectedFloor, session.Area.Entrance?.y)
                + GuideText.Separator
                + (state.RevealAll ? GuideText.FullDungeon : GuideText.Explored));
        if (session.Scan != null)
        {
            GUI.DrawTextureWithTexCoords(
                new Rect(32, 135, 446, 446),
                _renderer.Texture,
                _renderer.FitFloor(session, state.SelectedFloor));
        }

        GUI.Label(new Rect(32, 590, 440, 24), GuideText.OverlayControls);
        GUI.matrix = old;
    }
}
