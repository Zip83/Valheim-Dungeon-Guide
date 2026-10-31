using System;

namespace DungeonGuide;

/// <summary>
/// Adapts note editing to the native game text-input dialog.
/// </summary>
internal sealed class NoteDialog
{
    /// <summary>
    /// Adapts the native text-input receiver contract to a pending note update.
    /// </summary>
    private sealed class Receiver : TextReceiver
    {
        private readonly string _text;
        private readonly Action<string> _accept;

        /// <summary>
        /// Retains initial dialog text and the callback that accepts the edited note.
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
    public void Request(string text, string prompt, Action<string> accept)
    {
        if (TextInput.instance)
        {
            TextInput.instance.RequestText(new Receiver(text, accept), prompt, 80);
        }
    }
}
