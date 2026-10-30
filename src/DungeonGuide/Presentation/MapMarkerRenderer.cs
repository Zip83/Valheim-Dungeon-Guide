using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Draws dynamic navigation markers separately from the cached floor geometry.
/// </summary>
internal sealed class MapMarkerRenderer : IDisposable
{
    private const int Size = 768;
    private readonly List<int> _touched = new();
    private readonly Color32[] _pixels = new Color32[Size * Size];

    /// <summary>
    /// Shared texture used by the native dungeon map views and optional overlay.
    /// </summary>
    public Texture2D Texture
    {
        get;
    } = new Texture2D(
        Size,
        Size,
        TextureFormat.RGBA32,
        false)
    {
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
    };

    /// <summary>
    /// World-space width represented by the complete square map texture.
    /// </summary>
    public float Span { get; private set; } = 1;

    private Vector3 _center;
    private FloorFogMask _fog;
    private bool _reveal;
    private bool _playerMarker;

    /// <summary>
    /// Projects a world-space floor position into the shared texture coordinate system.
    /// </summary>
    public Vector2 UV(Point p) => new((p.x - _center.x) / Span + 0.5f, (p.z - _center.z) / Span + 0.5f);

    /// <summary>
    /// Rasterizes the selected floor, permitted chest markers, player position, and visible route sections.
    /// </summary>
    public void Render(DungeonSession session, MapViewState view, NavigationCoordinator navigator, Player player,
        FloorPlanRaster raster, FloorFogMask fog, bool reveal)
    {
        _fog = fog;
        _reveal = reveal;
        _playerMarker = false;
        Span = raster.Span;
        _center = UnityPoints.V(raster.Center);
        ClearPixels();
        var floor = raster.Floor;

        DrawRoute(session, navigator, floor, reveal);

        DrawFeatures(session, floor, reveal, raster);

        DrawChests(session, floor, reveal);

        DrawEntrance(session, floor, reveal);

        DrawPlayer(view, player, floor);

        Texture.SetPixels32(_pixels);
        Texture.Apply(false);
    }

    private void Square(Point p, int r, Color32 c)
    {
        var uv = UV(p);
        var px = (int)(uv.x * Size);
        var py = (int)(uv.y * Size);
        for (var x = -r; x <= r; x++)
        {
            for (var y = -r; y <= r; y++)
            {
                Set(px + x, py + y, c);
            }
        }
    }

    private void Segment(Point a, Point b, Color32 c)
    {
        var va = UV(a) * Size;
        var vb = UV(b) * Size;
        var n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(va, vb)));
        for (var i = 0; i <= n; i++)
        {
            var p = Vector2.Lerp(va, vb, (float)i / n);
            Set((int)p.x, (int)p.y, c);
            Set((int)p.x + 1, (int)p.y, c);
        }
    }

    private void Chevron(int x, int y, bool up, Color32 c)
    {
        for (var i = -4; i <= 4; i++)
        {
            Set(x + i, y + (up ? 1 : -1) * (4 - Math.Abs(i)), c);
        }
    }

    private void Set(int x, int y, Color32 c)
    {
        if (x >= 0 && x < Size && y >= 0 && y < Size)
        {
            var index = y * Size + x;
            if (!_playerMarker && !_reveal && _fog.Opacity[index] == 255)
            {
                return;
            }

            if (_pixels[index].a == 0)
            {
                _touched.Add(index);
            }

            _pixels[index] = c;
        }
    }

    private void ClearPixels()
    {
        foreach (var index in _touched)
        {
            _pixels[index] = default;
        }

        _touched.Clear();
    }

    /// <summary>Removes markers belonging to an exited session or superseded scan.</summary>
    public void Clear()
    {
        ClearPixels();
        Texture.SetPixels32(_pixels);
        Texture.Apply(false);
    }

    /// <inheritdoc/>
    public void Dispose() => UnityEngine.Object.Destroy(Texture);

    private void DrawRoute(DungeonSession session, NavigationCoordinator navigator, int floor, bool reveal)
    {
        var graph = session.Scan.Graph;

        // Draw only the revealed portion of a route when fog is enabled.
        for (var i = 1; i < navigator.Route.Count; i++)
        {
            var a = graph.Nodes[navigator.Route[i - 1]];
            var b = graph.Nodes[navigator.Route[i]];
            if (a.Floor != floor || b.Floor != floor
                || !reveal && (!session.Exploration.Has(a) || !session.Exploration.Has(b)))
            {
                continue;
            }

            Segment(a.Position, b.Position, MapPalette.Route);
        }
    }

    private void DrawFeatures(DungeonSession session, int floor, bool reveal, FloorPlanRaster raster)
    {
        var graph = session.Scan.Graph;

        foreach (var id in raster.Features)
        {
            var node = graph.Nodes[id];
            if (node.Floor != floor || !reveal && !session.Exploration.Has(node))
            {
                continue;
            }

            var cross = node.Edges.FirstOrDefault(e => graph.Nodes[e.To].Floor != floor);
            if (cross != null)
            {
                // A small directional chevron marks a floor crossing rather than an extra floor slice.
                var uv = UV(node.Position);
                var x = (int)(uv.x * Size);
                var y = (int)(uv.y * Size);
                var up = graph.Nodes[cross.To].Floor > floor;
                Chevron(x, y, up, MapPalette.FloorTransition);
            }

            if (node.Edges.Any(e => !e.Verified
                    && (e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop || e.Kind == PassageKind.Breakable)))
            {
                Square(node.Position, 2, MapPalette.UnverifiedPassage);
            }
        }
    }

    private void DrawChests(DungeonSession session, int floor, bool reveal)
    {
        foreach (var chest in session.Data.chests)
        {
            if (chest.done || !reveal && !chest.discovered && !chest.collect)
            {
                continue;
            }

            var cf = FloorDetector.Nearest(session.Floors, chest.position.y);
            if (cf == floor)
            {
                Square(chest.position, 5, chest.collect ? MapPalette.CollectionChest : MapPalette.KnownChest);
            }
            else if (chest.collect)
            {
                var uv = UV(chest.position);
                Chevron((int)(uv.x * Size), (int)(uv.y * Size), cf > floor, MapPalette.CollectionChest);
            }
        }
    }

    private void DrawEntrance(DungeonSession session, int floor, bool reveal)
    {
        var graph = session.Scan.Graph;

        var exit = session.Area.Entrance ?? session.Data.start;
        var exitNode = graph.Nearest(exit, 3);
        if (FloorDetector.Nearest(session.Floors, exit.y) == floor
            && (reveal || exitNode >= 0 && session.Exploration.Has(graph.Nodes[exitNode])))
        {
            Square(exit, 6, MapPalette.Entrance);
        }
    }

    private void DrawPlayer(MapViewState view, Player player, int floor)
    {
        if (view.CurrentFloor == floor)
        {
            _playerMarker = true;
            var uv = UV(UnityPoints.P(player.transform.position));
            var yaw = player.transform.eulerAngles.y * Mathf.Deg2Rad;
            var x = (int)(uv.x * Size);
            var y = (int)(uv.y * Size);
            for (var i = 0; i < 13; i++)
            {
                for (var side = -Mathf.Max(1, (12 - i) / 3); side <= Mathf.Max(1, (12 - i) / 3); side++)
                {
                    Set(
                        x + (int)(Mathf.Sin(yaw) * i + Mathf.Cos(yaw) * side),
                        y + (int)(Mathf.Cos(yaw) * i - Mathf.Sin(yaw) * side),
                        MapPalette.Player);
                }
            }
        }
    }
}
