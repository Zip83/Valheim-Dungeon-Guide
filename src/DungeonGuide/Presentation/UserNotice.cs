namespace DungeonGuide;

/// <summary>Displays transient notices through Valheim's native message HUD and its game font.</summary>
internal sealed class UserNotice
{
    /// <summary>Shows a native center notice when the game's message HUD is available.</summary>
    /// <param name="text">Localized player-facing notice, including any existing chest name.</param>
    public void Show(string text)
    {
        if (MessageHud.instance)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
        }
    }
}
