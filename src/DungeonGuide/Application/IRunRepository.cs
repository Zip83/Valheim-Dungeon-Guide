namespace DungeonGuide;

/// <summary>
/// Defines the persistence boundary for a single world and dungeon save.
/// </summary>
internal interface IRunRepository
{
    /// <summary>
    /// Loads the identified world/dungeon state or initializes a new save at the supplied scan origin.
    /// </summary>
    SavedRun Load(string world, string dungeon, Point start);
    /// <summary>
    /// Atomically persists one complete dungeon save state.
    /// </summary>
    void Save(SavedRun data);
}
