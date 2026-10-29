using System;

namespace DungeonGuide;

/// <summary>
/// Formats entrance-relative floor labels with an explicit fallback when the entrance is unknown.
/// </summary>
public sealed class FloorNumbering
{
    private readonly GuideText _text;

    /// <summary>
    /// Uses the supplied game-language accessor, or English when no accessor is available.
    /// </summary>
    /// <param name="selectedLanguage">Accessor for Valheim's selected language; null uses English.</param>
    public FloorNumbering(Func<string> selectedLanguage = null)
    {
        _text = new GuideText(selectedLanguage);
    }

    /// <summary>
    /// Labels the entrance as zero, lower floors negatively, and higher floors positively; unknown entrances use
    /// bottom-up numbering.
    /// </summary>
    /// <param name="levels">Detected floor elevations ordered from lowest to highest.</param>
    /// <param name="floor">Zero-based index into the detected floor elevations.</param>
    /// <param name="entranceHeight">Known entrance elevation, or null when no entrance has been identified.</param>
    /// <returns>An entrance-relative label, an explicit bottom-up fallback, or an unknown-floor label.</returns>
    public string Label(System.Collections.Generic.IReadOnlyList<float> levels, int floor, float? entranceHeight)
    {
        if (levels.Count == 0 || floor < 0 || floor >= levels.Count)
        {
            return _text.FloorUnknown;
        }

        if (!entranceHeight.HasValue)
        {
            return _text.FloorPrefix + floor + _text.BottomUp;
        }

        var relative = floor - FloorDetector.Nearest(levels, entranceHeight.Value);
        return relative == 0
            ? _text.EntranceFloor
            : relative > 0 ? _text.FloorPositive + relative : _text.FloorNegative + (-relative);
    }
}
