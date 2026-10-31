using System;
using System.IO;
using System.Threading;

namespace DungeonGuide;

/// <summary>Prepares floors and persistent bytes while the scan graph is still exclusively owned by its worker.</summary>
internal sealed class GeometryCachePreparation
{
    private readonly string _key;
    private readonly float _spacing;
    private readonly bool _truncated;
    private readonly Point? _entrance;

    /// <summary>Sealed cache bytes, or null when optional cache encoding could not finish.</summary>
    public byte[] Payload
    {
        get; private set;
    }

    /// <summary>Optional persistence failure, which must not prevent an otherwise valid live map.</summary>
    public Exception Error
    {
        get; private set;
    }

    /// <summary>Copies primitive context before any worker accesses the unpublished graph.</summary>
    public GeometryCachePreparation(string key, float spacing, bool truncated, Point? entrance)
    {
        _key = key;
        _spacing = spacing;
        _truncated = truncated;
        _entrance = entrance;
    }

    /// <summary>Delegates authoritative floor analysis, then encodes only the private graph's managed values.</summary>
    public FloorLayout Analyze(FloorDetector detector, NavGraph graph, CancellationToken cancellation)
    {
        var layout = detector.Detect(graph, _spacing, cancellation);
        var geometry = new CachedGeometry(graph, layout)
        {
            Spacing = _spacing,
            Truncated = _truncated,
            Complete = true,
            Entrance = _entrance
        };
        Encode(geometry, cancellation);
        return layout;
    }

    private void Encode(CachedGeometry geometry, CancellationToken cancellation)
    {
        try
        {
            using var writer = new GeometryCacheWriter();
            foreach (var step in writer.Encode(_key, geometry))
            {
                cancellation.ThrowIfCancellationRequested();
                if (writer.Length > GeometryCacheFormat.MaxBytes)
                {
                    throw new InvalidDataException("The completed geometry exceeds the persistent cache size limit.");
                }
            }

            Payload = writer.Payload;
        }
        catch (Exception error) when (!(error is OperationCanceledException))
        {
            Error = error;
        }
    }
}
