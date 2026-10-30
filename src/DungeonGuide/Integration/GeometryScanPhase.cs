namespace DungeonGuide;

/// <summary>Identifies the current phase of cooperative geometry collection.</summary>
internal enum GeometryScanPhase
{
    Idle,
    Sampling,
    Linking,
    Complete
}
