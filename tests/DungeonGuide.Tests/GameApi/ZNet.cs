using System;
using System.Diagnostics.CodeAnalysis;

namespace DungeonGuide;

/// <summary>Models only the native world-descriptor contract for the linked production reader.</summary>
internal sealed class ZNet
{
    /// <summary>Native network singleton, which may precede its world descriptor.</summary>
    [SuppressMessage("Style", "IDE1006", Justification = "Matches the native ZNet API used by production code.")]
    public static ZNet instance
    {
        get; set;
    }

    /// <summary>Descriptor received from the server, or null during connection transitions.</summary>
    public WorldDescriptor Descriptor
    {
        get; set;
    }

    /// <summary>Models Unity's false comparison for a destroyed native object.</summary>
    public bool Destroyed
    {
        get; set;
    }

    /// <summary>Returns the descriptor without dereferencing a possibly missing world.</summary>
    public WorldDescriptor GetWorld()
    {
        if (Destroyed)
        {
            throw new InvalidOperationException("Destroyed network must not be queried.");
        }

        return Descriptor;
    }

    /// <summary>Reproduces the native unchecked UID accessor that fails before descriptor arrival.</summary>
    public long GetWorldUID() => Descriptor.m_uid;

    /// <summary>Models native Unity object lifetime checks.</summary>
    public static implicit operator bool(ZNet network) => network != null && !network.Destroyed;

    /// <summary>Contains the native UID field consumed by the production reader.</summary>
    public sealed class WorldDescriptor
    {
        /// <summary>Initializes the identity supplied by the server.</summary>
        public WorldDescriptor(long uid) => m_uid = uid;

        /// <summary>Native descriptor UID; zero is valid and is not a missing-world sentinel.</summary>
        [SuppressMessage("Style", "IDE1006", Justification = "Matches the native World API used by production code.")]
        public readonly long m_uid;
    }
}
