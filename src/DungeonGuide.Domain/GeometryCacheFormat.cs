using System;
using System.IO;
using System.Text;

namespace DungeonGuide;

/// <summary>Defines bounded binary primitives and version limits for derived dungeon geometry caches.</summary>
internal static class GeometryCacheFormat
{
    internal const int Version = 1;
    internal const int MaxNodes = 250000;
    internal const int MaxEdges = 4000000;
    internal const int MaxFloors = 4096;
    internal const int MaxBytes = 64 * 1024 * 1024;

    internal static void WriteText(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? "");
        Count(bytes.Length, 4096);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    internal static string ReadText(BinaryReader reader)
    {
        var count = Count(reader.ReadInt32(), 4096);
        var bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
        {
            throw new EndOfStreamException("Incomplete geometry cache text.");
        }

        return new UTF8Encoding(false, true).GetString(bytes);
    }

    internal static int Count(int count, int limit)
    {
        if (count < 0 || count > limit)
        {
            throw new InvalidDataException("Geometry cache count exceeds its supported limits.");
        }

        return count;
    }

    internal static void WritePoint(BinaryWriter writer, Point point)
    {
        writer.Write(point.x);
        writer.Write(point.y);
        writer.Write(point.z);
    }

    internal static Point ReadPoint(BinaryReader reader) => new(Finite(reader.ReadSingle()),
        Finite(reader.ReadSingle()), Finite(reader.ReadSingle()));

    internal static float Finite(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new InvalidDataException("Geometry cache contains non-finite coordinates.");
        }

        return value;
    }

    internal static int Node(int node, int count)
    {
        if (node < 0 || node >= count)
        {
            throw new InvalidDataException("Geometry cache contains an invalid node reference.");
        }

        return node;
    }
}
