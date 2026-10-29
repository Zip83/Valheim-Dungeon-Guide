using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Reads Valheim's language setting at most once per frame without depending on the operating-system culture.
/// </summary>
internal sealed class GameLanguage
{
    private int _frame = -1;
    private string _selectedLanguage;

    /// <summary>
    /// Returns the game's selected language, or null before localization is available so text falls back to English.
    /// </summary>
    public string Current()
    {
        if (_frame != Time.frameCount)
        {
            _frame = Time.frameCount;
            _selectedLanguage = Localization.instance?.GetSelectedLanguage();
        }

        return _selectedLanguage;
    }
}
