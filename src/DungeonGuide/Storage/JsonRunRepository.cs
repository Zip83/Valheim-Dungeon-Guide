using System;
using System.IO;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Loads versioned dungeon state and atomically replaces JSON save files.
/// </summary>
internal sealed class JsonRunRepository : IRunRepository
{
    private readonly string _directory;
    private readonly SaveMigration _migration;

    /// <summary>
    /// Creates the save directory and uses the supplied migration policy for loaded records.
    /// </summary>
    public JsonRunRepository(string directory, SaveMigration migration)
    {
        _directory = directory;
        _migration = migration;
        Directory.CreateDirectory(directory);
    }

    private string PathFor(string world, string dungeon) => Path.Combine(_directory, world + "_" + dungeon + ".json");

    /// <summary>
    /// Loads and migrates matching dungeon state, rejecting corrupt identities and unsupported formats.
    /// </summary>
    public SavedRun Load(string world, string dungeon, Point start)
    {
        var path = PathFor(world, dungeon);
        if (!File.Exists(path))
        {
            return new SavedRun
            {
                world = world,
                dungeon = dungeon,
                start = start
            };
        }

        var data = JsonUtility.FromJson<SavedRun>(File.ReadAllText(path));
        if (data == null || data.world != world || data.dungeon != dungeon)
        {
            throw new InvalidDataException("The saved area identity does not match the active dungeon.");
        }

        return _migration.Upgrade(data);
    }

    /// <summary>
    /// Writes a complete temporary JSON file and atomically replaces the prior save while retaining its previous
    /// contents.
    /// </summary>
    public void Save(SavedRun data)
    {
        var path = PathFor(data.world, data.dungeon);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(data, true));
        if (File.Exists(path))
        {
            File.Replace(temp, path, path + ".bak");
        }
        else
        {
            File.Move(temp, path);
        }
    }
}
