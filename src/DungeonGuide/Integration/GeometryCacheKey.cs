using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Fingerprints a world's dungeon layout and sampling inputs independently of object ordering.</summary>
internal static class GeometryCacheKey
{
    /// <summary>Changes when the game, world, room poses/bounds or geometry sampling inputs change.</summary>
    public static string Create(DungeonArea area, string world, GuideSettings settings)
    {
        var text = new StringBuilder("geometry-rules-1|").Append(world).Append('|').Append(area.Id).Append('|').Append(global::Version.GetVersionString())
            .Append('|').Append(Mathf.Clamp(settings.Spacing.Value, 0.75f, 3).ToString("R", CultureInfo.InvariantCulture))
            .Append('|').Append(Mathf.Clamp(settings.Columns.Value, 1000, 120000).ToString(CultureInfo.InvariantCulture));
        Append(text, area.Bounds);
        var rooms = area.RoomVolumes.Select((room, index) =>
            Append(new StringBuilder(room.Id), area.Rooms[index]).ToString()).OrderBy(value => value, System.StringComparer.Ordinal);
        foreach (var room in rooms)
        {
            text.Append('|').Append(room);
        }

        using var hash = SHA256.Create();
        return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(value => value.ToString("x2")));
    }

    private static StringBuilder Append(StringBuilder text, Bounds bounds)
    {
        Append(text, bounds.center);
        Append(text, bounds.size);
        return text;
    }

    private static void Append(StringBuilder text, Vector3 value)
    {
        text.Append('|').Append(value.x.ToString("R", CultureInfo.InvariantCulture))
            .Append('|').Append(value.y.ToString("R", CultureInfo.InvariantCulture))
            .Append('|').Append(value.z.ToString("R", CultureInfo.InvariantCulture));
    }
}
