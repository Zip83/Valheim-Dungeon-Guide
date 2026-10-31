using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Checks atomic derived-cache recovery, future-data protection and ordered asynchronous I/O.</summary>
public sealed class GeometryCacheRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DungeonGeometryTests", Guid.NewGuid().ToString("N"));
    private readonly string _key = new('a', 64);

    /// <summary>A fresh repository restores a completed payload and replacement retains the previous file.</summary>
    [Fact]
    public void IndependentReloadAndAtomicReplacement()
    {
        var repository = new GeometryCacheRepository(_directory);
        Assert.True(repository.Save(_key, Payload()));
        Assert.True(new GeometryCacheRepository(_directory).Load(_key, default).Complete);
        repository.Invalidate(_key);
        Assert.Null(new GeometryCacheRepository(_directory).Load(_key, default));
        Assert.True(repository.Save(_key, Payload()));
        Assert.True(File.Exists(FilePath + ".bak"));
        Assert.False(File.Exists(FilePath + ".stale"));
    }

    /// <summary>Corrupt bytes are preserved before a replacement can repair this disposable cache.</summary>
    [Fact]
    public void PreservesCorruptionBeforeRepair()
    {
        var repository = new GeometryCacheRepository(_directory);
        repository.Save(_key, Payload());
        var bytes = File.ReadAllBytes(FilePath);
        bytes[bytes.Length - 1] ^= 1;
        File.WriteAllBytes(FilePath, bytes);
        Assert.Throws<InvalidDataException>(() => repository.Load(_key, default));
        Assert.Contains(Directory.GetFiles(_directory, "*.invalid.*"), path => File.ReadAllBytes(path).SequenceEqual(bytes));
        Assert.True(repository.Save(_key, Payload()));
        Assert.NotNull(repository.Load(_key, default));
    }

    /// <summary>Future data remains protected even if a stale marker exists or no read preceded the attempted write.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProtectsFutureDataWithoutPriorLoad(bool stale)
    {
        var repository = new GeometryCacheRepository(_directory);
        var payload = Payload();
        using (var stream = new MemoryStream(payload))
        using (var reader = new BinaryReader(stream))
        {
            GeometryCacheFormat.ReadText(reader);
            Array.Copy(BitConverter.GetBytes(GeometryCacheFormat.Version + 1), 0, payload, stream.Position, 4);
        }

        repository.Save(_key, payload);
        var original = File.ReadAllBytes(FilePath);
        if (stale)
        {
            File.WriteAllText(FilePath + ".stale", "previous invalidation");
        }

        var independent = new GeometryCacheRepository(_directory);
        Assert.False(independent.Save(_key, Payload()));
        Assert.Throws<NotSupportedException>(() => independent.Load(_key, default));
        independent.Invalidate(_key);
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        Assert.Equal(stale, File.Exists(FilePath + ".stale"));
    }

    /// <summary>Revoked publication cannot replace an earlier completed map.</summary>
    [Fact]
    public void CanceledWriteRetainsPreviousMap()
    {
        var repository = new GeometryCacheRepository(_directory);
        repository.Save(_key, Payload());
        var original = File.ReadAllBytes(FilePath);
        Assert.Throws<OperationCanceledException>(() => repository.Save(_key, Payload(), new CancellationToken(true)));
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    /// <summary>A failed operation cannot reorder or prevent later cache invalidation and repair jobs.</summary>
    [Fact]
    public async Task WorkerPreservesSubmissionOrderAfterFailure()
    {
        var worker = new GeometryCacheWorker();
        var repository = new GeometryCacheRepository(_directory);
        var first = worker.Enqueue(() => repository.Save(_key, Payload()));
        var failure = worker.Enqueue<bool>(() => throw new IOException("fixture failure"));
        var invalidation = worker.Enqueue(() =>
        {
            repository.Invalidate(_key);
            return repository.Load(_key, default);
        });
        Assert.True(await first);
        await Assert.ThrowsAsync<IOException>(() => failure);
        Assert.Null(await invalidation);
        Assert.True(await worker.Enqueue(() => repository.Save(_key, Payload())));
    }

    private string FilePath => Path.Combine(_directory, _key + ".geometry");

    private byte[] Payload()
    {
        using var writer = new GeometryCacheWriter();
        foreach (var step in writer.Encode(_key, new CachedGeometry { Spacing = 1.25f, Complete = true }))
        {
        }

        return writer.Payload;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
