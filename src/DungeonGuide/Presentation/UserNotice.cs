using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Stores a short-lived player notice separately from technical logging.
/// </summary>
internal sealed class UserNotice
{
    /// <summary>
    /// Latest short player-facing message.
    /// </summary>
    public string Text
    {
        get; private set;
    }

    /// <summary>
    /// Unscaled game time at which the message expires.
    /// </summary>
    public float Until
    {
        get; private set;
    }

    /// <summary>
    /// Replaces the current message and schedules its short display interval.
    /// </summary>
    public void Show(string text)
    {
        Text = text;
        Until = Time.unscaledTime + 4;
    }
}
