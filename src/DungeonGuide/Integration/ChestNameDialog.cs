using System;

namespace DungeonGuide;

/// <summary>
/// Adapts chest note editing to the native game text-input dialog.
/// </summary>
internal sealed class ChestNameDialog
{
    private readonly GuideText _text;

    /// <summary>
    /// Uses the shared player-facing text catalog for the game's selected language.
    /// </summary>
    public ChestNameDialog(GuideText text)
    {
        _text = text;
    }

    /// <summary>
    /// Adapts the native text-input receiver contract to a pending chest-note update.
    /// </summary>
    private sealed class Receiver : TextReceiver
    {
        private readonly string _text;
        private readonly Action<string> _accept;

        /// <summary>
        /// Retains initial dialog text and the callback that accepts the edited chest note.
        /// </summary>
        public Receiver(string text, Action<string> accept)
        {
            _text = text;
            _accept = accept;
        }

        /// <inheritdoc/>
        public string GetText() => _text;

        /// <inheritdoc/>
        public void SetText(string value) => _accept(value);
    }

    /// <summary>
    /// Shows the native text dialog with existing text and forwards the accepted note.
    /// </summary>
    public void Request(string text, Action<string> accept)
    {
        if (TextInput.instance)
        {
            TextInput.instance.RequestText(new Receiver(text, accept), _text.ChestNotePrompt, 80);
        }
    }
}
