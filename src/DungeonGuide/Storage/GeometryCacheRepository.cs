using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace DungeonGuide;

/// <summary>Reads checksummed derived geometry and atomically stores it without overwriting unreadable cache files.</summary>
internal sealed class GeometryCacheRepository
{
    private readonly string _directory;
    private readonly HashSet<string> _protected = new(StringComparer.Ordinal);

    /// <summary>Retains a cache directory; all operations are called by the serial background worker.</summary>
    public GeometryCacheRepository(string directory)
    {
        _directory = directory;
    }

    /// <summary>Loads a matching, uninvalidated cache; corrupt or unsupported files remain protected from replacement.</summary>
    public CachedGeometry Load(string key, CancellationToken cancellation)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            EnsureKnownFormat(path);
            var payload = ReadPayload(path);
            using var stream = new MemoryStream(payload);
            var geometry = GeometryCacheReader.Read(stream, key, cancellation);
            return File.Exists(path + ".stale") ? null : geometry;
        }
        catch (Exception error) when (!(error is OperationCanceledException))
        {
            PreserveUnreadable(key, path, error);
            throw;
        }
    }

    /// <summary>Writes one sealed private payload and retains the previous cache as a backup.</summary>
    public bool Save(string key, byte[] payload, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        if (_protected.Contains(key))
        {
            return false;
        }

        GeometryCacheFormat.Count(payload.Length, GeometryCacheFormat.MaxBytes);
        var path = PathFor(key);
        if (File.Exists(path) && !CanReplace(key, path))
        {
            return false;
        }

        Directory.CreateDirectory(_directory);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            WriteTemporary(temporary, payload);
            cancellation.ThrowIfCancellationRequested();
            PublishTemporary(temporary, path);
            File.Delete(path + ".stale");
            return true;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private bool CanReplace(string key, string path)
    {
        try
        {
            EnsureKnownFormat(path);
            _ = ReadPayload(path);
            return true;
        }
        catch (Exception error)
        {
            PreserveUnreadable(key, path, error);
            return !_protected.Contains(key);
        }
    }

    private static void EnsureKnownFormat(string path)
    {
        using var input = File.OpenRead(path);
        using var reader = new BinaryReader(input);
        if (GeometryCacheFormat.ReadText(reader) != "ValheimDungeonGeometry"
            || reader.ReadInt32() > GeometryCacheFormat.Version)
        {
            throw new NotSupportedException("The existing geometry cache requires a different or newer format.");
        }
    }

    private static void WriteTemporary(string temporary, byte[] payload)
    {
        using (var output = File.Create(temporary))
        using (var hash = SHA256.Create())
        {
            output.Write(payload, 0, payload.Length);
            var checksum = hash.ComputeHash(payload);
            output.Write(checksum, 0, checksum.Length);
            output.Flush();
        }
    }

    private static void PublishTemporary(string temporary, string path)
    {
        if (File.Exists(path))
        {
            File.Replace(temporary, path, path + ".bak");
        }
        else
        {
            File.Move(temporary, path);
        }
    }

    /// <summary>Marks old geometry unavailable before a replacement scan, without discarding the previous file.</summary>
    public void Invalidate(string key)
    {
        if (_protected.Contains(key))
        {
            return;
        }

        var path = PathFor(key);
        if (File.Exists(path) && !CanReplace(key, path))
        {
            return;
        }

        MarkStale(path);
    }

    private void PreserveUnreadable(string key, string path, Exception error)
    {
        if (error is NotSupportedException)
        {
            _protected.Add(key);
            return;
        }

        try
        {
            if (new FileInfo(path).Length > GeometryCacheFormat.MaxBytes + 32L)
            {
                _protected.Add(key);
                return;
            }

            File.Copy(path, path + ".invalid." + Guid.NewGuid().ToString("N"));
            MarkStale(path);
        }
        catch (Exception)
        {
            // If preserving the damaged file fails, keep it untouched instead of replacing it.
            _protected.Add(key);
        }
    }

    private void MarkStale(string path)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path + ".stale", "Geometry changed; awaiting a replacement scan.");
    }

    private static byte[] ReadPayload(string path)
    {
        using var input = File.OpenRead(path);
        var length = input.Length;
        if (length < 32 || length > GeometryCacheFormat.MaxBytes + 32L)
        {
            throw new InvalidDataException("Geometry cache file size is invalid.");
        }

        using var reader = new BinaryReader(input);
        var bytes = reader.ReadBytes((int)length);
        if (bytes.Length != length)
        {
            throw new EndOfStreamException("Incomplete geometry cache payload.");
        }

        var payload = new byte[bytes.Length - 32];
        Array.Copy(bytes, payload, payload.Length);
        using var hash = SHA256.Create();
        if (!hash.ComputeHash(payload).SequenceEqual(bytes.Skip(payload.Length)))
        {
            throw new InvalidDataException("Geometry cache checksum failed; preserve the existing file.");
        }

        return payload;
    }

    private string PathFor(string key)
    {
        if (key == null || key.Length != 64 || key.Any(c => !IsLowerHex(c)))
        {
            throw new ArgumentException("Geometry cache keys must be lowercase SHA-256 digests.", nameof(key));
        }

        return Path.Combine(_directory, key + ".geometry");
    }

    private static bool IsLowerHex(char value) => value >= '0' && value <= '9' || value >= 'a' && value <= 'f';

}
