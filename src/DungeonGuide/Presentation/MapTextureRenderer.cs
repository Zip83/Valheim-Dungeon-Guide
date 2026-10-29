using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Rasterizes the selected dungeon floor, visible markers, and route onto the shared map texture.
/// </summary>
internal sealed class MapTextureRenderer : IDisposable
{
    private const int Size = 768;
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
    private readonly MapExtentFitter _extentFitter;

    /// <summary>
    /// Creates the shared map texture and uses the supplied floor-extent fitting policy.
    /// </summary>
    public MapTextureRenderer(MapExtentFitter extentFitter)
    {
        _extentFitter = extentFitter;
    }

    /// <summary>
    /// Returns a normalized viewport fitted to the selected floor extent.
    /// </summary>
    public Rect FitFloor(DungeonSession session, int floor)
    {
        var span = Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3);
        var fit = _extentFitter.Fit(session.Scan.Graph, floor, UnityPoints.P(session.Area.Bounds.center), span);
        return new Rect(fit.X, fit.Y, fit.Size, fit.Size);
    }

    /// <summary>
    /// Projects a world-space floor position into the shared texture coordinate system.
    /// </summary>
    public Vector2 UV(Point p) => new((p.x - _center.x) / Span + 0.5f, (p.z - _center.z) / Span + 0.5f);

    /// <summary>
    /// Rasterizes the selected floor, permitted chest markers, player position, and visible route sections.
    /// </summary>
    public void Render(DungeonSession session, MapViewState view, NavigationCoordinator navigator, Player player)
    {
        Span = Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3);
        _center = session.Area.Bounds.center;
        var background = MapPalette.TextureBackground;
        for (var i = 0; i < _pixels.Length; i++)
        {
            _pixels[i] = background;
        }

        var graph = session.Scan.Graph;
        var floor = view.SelectedFloor;
        var radius = Mathf.Clamp(Mathf.CeilToInt(session.Scan.Spacing / Span * Size * 0.52f), 1, 15);
        foreach (var node in graph.Nodes)
        {
            if (node.Floor != floor)
            {
                continue;
            }

            var seen = session.Exploration.Has(node);
            if (!view.RevealAll && !seen)
            {
                continue;
            }

            Square(node.Position, radius, seen ? MapPalette.ExploredFloor : MapPalette.ScannedFloor);
        }

        // Draw only the revealed portion of a route when fog is enabled.
        for (var i = 1; i < navigator.Route.Count; i++)
        {
            var a = graph.Nodes[navigator.Route[i - 1]];
            var b = graph.Nodes[navigator.Route[i]];
            if (a.Floor != floor || b.Floor != floor
                || !view.RevealAll && (!session.Exploration.Has(a) || !session.Exploration.Has(b)))
            {
                continue;
            }

            Segment(a.Position, b.Position, MapPalette.Route);
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Floor != floor || !view.RevealAll && !session.Exploration.Has(node))
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

        foreach (var chest in session.Data.chests)
        {
            if (chest.done || !view.RevealAll && !chest.discovered && !chest.collect)
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

        var exit = session.Area.Entrance ?? session.Data.start;
        var exitNode = graph.Nearest(exit, 3);
        if (FloorDetector.Nearest(session.Floors, exit.y) == floor
            && (view.RevealAll || exitNode >= 0 && session.Exploration.Has(graph.Nodes[exitNode])))
        {
            Square(exit, 6, MapPalette.Entrance);
        }

        if (view.CurrentFloor == floor)
        {
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
            _pixels[y * Size + x] = c;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => UnityEngine.Object.Destroy(Texture);
}
