using System;
using BepInEx.Logging;

namespace DungeonGuide;

/// <summary>
/// Opens and saves dungeon sessions while protecting corrupt or unsupported persistent state.
/// </summary>
internal sealed class SessionLifecycle
{
    private readonly GuideText _text;
    private readonly IRunRepository _repository;
    private readonly ManualLogSource _log;
    private readonly UserNotice _notice;

    /// <summary>
    /// Connects save persistence with diagnostics and short player notices.
    /// </summary>
    public SessionLifecycle(IRunRepository repository, ManualLogSource log, UserNotice notice, GuideText text)
    {
        _text = text;
        _repository = repository;
        _log = log;
        _notice = notice;
    }

    /// <summary>
    /// Loads persistent state into a new session, entering read-only mode when existing data cannot be safely used.
    /// </summary>
    public DungeonSession Open(DungeonArea area, string world, Point start)
    {
        var session = new DungeonSession
        {
            Area = area
        };
        try
        {
            session.Data = _repository.Load(world, area.Id, start);
        }
        catch (Exception e)
        {
            // Never overwrite a corrupt or future-version save with an empty run.
            session.ReadOnly = true;
            session.Data = new SavedRun
            {
                world = world,
                dungeon = area.Id,
                start = start
            };
            _log.LogError("Cannot load run; persistence disabled for this run: " + e);
            _notice.Show(_text.SaveReadFailed);
        }

        session.Exploration.Load(session.Data.visited);
        session.Chests = new ChestRegistry(session.Data.chests, () => _text.SelectedLanguage);
        return session;
    }

    /// <summary>
    /// Persists exploration and chest state only when the session is writable.
    /// </summary>
    /// <returns>Whether the complete state was saved; failures retain the existing diagnostic notice.</returns>
    public bool Save(DungeonSession session)
    {
        if (session == null || session.ReadOnly)
        {
            return false;
        }

        session.Data.visited = session.Exploration.Export();
        try
        {
            _repository.Save(session.Data);
            return true;
        }
        catch (Exception e)
        {
            _log.LogError("Cannot save guide run: " + e);
            _notice.Show(_text.SaveWriteFailed);
            return false;
        }
    }
}
