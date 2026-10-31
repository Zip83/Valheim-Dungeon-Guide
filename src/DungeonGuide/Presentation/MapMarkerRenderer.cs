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
    private const int RouteRadius = 2;
    private const int RouteOutlineRadius = 3;
    private const int AnchorRadius = 7;
    private readonly List<int> _touched = new();
    private readonly Color32[] _pixels = new Color32[Size * Size];
    private readonly PlayerMarkerRenderer _player;
    private readonly StairMarkerRenderer _stairs;

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

    /// <summary>Clears the backing texture before incremental rectangle uploads begin.</summary>
    public MapMarkerRenderer()
    {
        _player = new PlayerMarkerRenderer(Set);
        _stairs = new StairMarkerRenderer(StrokePixel);
        Texture.SetPixels32(_pixels);
        Texture.Apply(false);
    }

    private Vector3 _center;
    private FloorFogMask _fog;
    private bool _reveal;
    private bool _playerMarker;
    private int _left;
    private int _right;
    private int _bottom;
    private int _top;
    private Color32[] _upload;

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
        _left = _bottom = Size;
        _right = _top = -1;
        foreach (var index in _touched)
        {
            IncludePixel(index % Size, index / Size);
        }

        ClearPixels();
        var floor = raster.Floor;

        DrawRoute(session, navigator, floor, reveal);

        DrawFeatures(session, floor, reveal, raster);

        DrawChests(session, floor, reveal);

        DrawWaypoints(session, navigator, floor);

        DrawEntrance(session, floor, reveal);

        DrawPlayer(view, player, floor);

        UploadChanges();
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

    private void Segment(Point a, Point b, Color32 c, int radius)
    {
        var va = UV(a) * Size;
        var vb = UV(b) * Size;
        var n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(va, vb)));
        for (var i = 0; i <= n; i++)
        {
            var p = Vector2.Lerp(va, vb, (float)i / n);
            StrokePixel((int)p.x, (int)p.y, radius, c);
        }
    }

    private void StrokePixel(int x, int y, int radius, Color32 color)
    {
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    Set(x + dx, y + dy, color);
                }
            }
        }
    }

    private void DrawGrappleAnchor(Point anchor)
    {
        var uv = UV(anchor) * Size;
        var x = Mathf.RoundToInt(uv.x);
        var y = Mathf.RoundToInt(uv.y);
        AnchorCross(x, y, 2, MapPalette.RouteOutline);
        AnchorCross(x, y, 0, MapPalette.GrappleRoute);
    }

    private void AnchorCross(int x, int y, int radius, Color32 color)
    {
        for (var offset = -AnchorRadius; offset <= AnchorRadius; offset++)
        {
            StrokePixel(x + offset, y, radius, color);
            StrokePixel(x, y + offset, radius, color);
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
            IncludePixel(x, y);
        }
    }

    private void IncludePixel(int x, int y)
    {
        _left = Mathf.Min(_left, x);
        _right = Mathf.Max(_right, x);
        _bottom = Mathf.Min(_bottom, y);
        _top = Mathf.Max(_top, y);
    }

    private void UploadChanges()
    {
        if (_right < _left || _top < _bottom)
        {
            return;
        }

        var width = _right - _left + 1;
        var height = _top - _bottom + 1;
        if (_upload == null || _upload.Length != width * height)
        {
            _upload = new Color32[width * height];
        }

        for (var y = 0; y < height; y++)
        {
            Array.Copy(_pixels, (_bottom + y) * Size + _left, _upload, y * width, width);
        }

        Texture.SetPixels32(_left, _bottom, width, height, _upload);
        Texture.Apply(false);
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
        _left = _bottom = Size;
        _right = _top = -1;
        foreach (var index in _touched)
        {
            IncludePixel(index % Size, index / Size);
        }

        ClearPixels();
        UploadChanges();
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
            var grapple = navigator.IsGrapplePassage(navigator.Route[i - 1], navigator.Route[i]);
            var onFloor = grapple ? a.Floor == floor || b.Floor == floor : a.Floor == floor && b.Floor == floor;
            if (!onFloor
                || !reveal && (!session.Exploration.Has(a) || !session.Exploration.Has(b)))
            {
                continue;
            }

            Segment(a.Position, b.Position, MapPalette.RouteOutline, RouteOutlineRadius);
            Segment(a.Position, b.Position, grapple ? MapPalette.GrappleRoute : MapPalette.Route, RouteRadius);
            if (grapple)
            {
                DrawGrappleAnchor(UnityPoints.P(navigator.GrappleAnchor));
            }
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

            DrawStairDirections(graph, id);

            if (node.Edges.Any(e => !e.Verified
                    && (e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop || e.Kind == PassageKind.Breakable)))
            {
                Square(node.Position, 2, MapPalette.UnverifiedPassage);
            }
        }
    }

    private void DrawStairDirections(NavGraph graph, int id)
    {
        var directions = StairMarkerPolicy.Directions(graph, id);
        var uv = UV(graph.Nodes[id].Position);
        var x = (int)(uv.x * Size);
        var y = (int)(uv.y * Size);
        _stairs.Draw(x, y, directions.Up, directions.Down);
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

    private void DrawWaypoints(DungeonSession session, NavigationCoordinator navigator, int floor)
    {
        foreach (var record in session.Data.waypoints)
        {
            if (FloorDetector.Nearest(session.Floors, record.position.y) == floor)
            {
                DrawWaypoint(record.position, navigator.Waypoint == record);
            }
        }
    }

    private void DrawWaypoint(Point position, bool selected)
    {
        var uv = UV(position) * Size;
        for (var y = -7; y <= 7; y++)
        {
            for (var x = -7; x <= 7; x++)
            {
                var distance = Math.Abs(x) + Math.Abs(y);
                if (distance <= 7)
                {
                    var color = distance >= 6 ? MapPalette.TextureBackground : MapPalette.Waypoint;
                    Set((int)uv.x + x, (int)uv.y + y, selected && distance <= 2 ? MapPalette.Player : color);
                }
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
        if (view.CurrentFloor != floor)
        {
            return;
        }

        _playerMarker = true;
        var uv = UV(UnityPoints.P(player.transform.position));
        _player.Draw((int)(uv.x * Size), (int)(uv.y * Size), player.transform.eulerAngles.y);
    }
}
