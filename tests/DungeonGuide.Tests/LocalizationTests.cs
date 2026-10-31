using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects English fallback, Czech selection, live language changes, and user-owned chest names.
/// </summary>
public sealed class LocalizationTests
{
    /// <summary>
    /// A catalog without game localization available starts in English.
    /// </summary>
    [Fact]
    public void StartsInEnglishWithoutGameLanguage()
    {
        Assert.Equal("Explore", new GuideText().Explore);
        Assert.Equal("Entrance floor 0", new FloorNumbering().Label(new[] { 0f }, 0, 0));
    }

    /// <summary>
    /// Languages without a translation, including unavailable settings, use English copy.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("English")]
    [InlineData("German")]
    [InlineData("French")]
    [InlineData("Czechish")]
    public void UsesEnglishFallback(string language)
    {
        var text = new GuideText(() => language);

        Assert.Equal("Explore", text.Explore);
        Assert.Equal("Open the door", text.OpenDoor);
        Assert.Equal("Chest name / note", text.ChestNotePrompt);
        Assert.Equal("Collected: ", text.ChestCollected);
    }

    /// <summary>
    /// Only the game's Czech language name selects Czech instructions and captions.
    /// </summary>
    [Theory]
    [InlineData("Czech")]
    [InlineData("czech")]
    public void SelectsCzech(string language)
    {
        var text = new GuideText(() => language);

        Assert.Equal("Průzkum", text.Explore);
        Assert.Equal("Otevři dveře", text.OpenDoor);
        Assert.Equal("Název / poznámka k truhle", text.ChestNotePrompt);
        Assert.Equal("Vyzvednuto: ", text.ChestCollected);
    }

    /// <summary>
    /// Existing consumers follow game-language changes without global language state or reconstruction.
    /// </summary>
    [Fact]
    public void UpdatesExistingCatalogAndFloorLabels()
    {
        var language = "English";
        var text = new GuideText(() => language);
        var numbering = new FloorNumbering(() => language);
        var floors = new[] { 0f, 12f };

        Assert.Equal("Explore", text.Explore);
        Assert.Equal("Floor +1", numbering.Label(floors, 1, 0));

        language = "Czech";
        Assert.Equal("Průzkum", text.Explore);
        Assert.Equal("Patro +1", numbering.Label(floors, 1, 0));

        language = "German";
        Assert.Equal("Explore", text.Explore);
        Assert.Equal("Floor +1", numbering.Label(floors, 1, 0));
    }

    /// <summary>
    /// English floor labels preserve entrance-relative numbering and the unknown-entrance fallback.
    /// </summary>
    [Theory]
    [InlineData(1, 12f, "Entrance floor 0")]
    [InlineData(0, 12f, "Floor −1")]
    [InlineData(2, 12f, "Floor +1")]
    [InlineData(0, null, "Floor 0 (from bottom)")]
    [InlineData(99, 12f, "Floor unknown")]
    public void FormatsEnglishFloorLabels(int floor, float? entrance, string expected)
    {
        Assert.Equal(expected, new FloorNumbering().Label(new[] { 0f, 12f, 24f }, floor, entrance));
    }

    /// <summary>
    /// Every catalog entry has both supported texts and falls back consistently for an unsupported language.
    /// </summary>
    [Fact]
    public void ProvidesCompleteCatalogsAndFallback()
    {
        var english = new GuideText();
        var czech = new GuideText(() => "Czech");
        var unsupported = new GuideText(() => "German");
        var properties = typeof(GuideText).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(p => p.PropertyType == typeof(string) && p.Name != nameof(GuideText.SelectedLanguage));

        var resources = new ResourceManager("DungeonGuide.Domain.Content.Strings", typeof(GuideText).Assembly);
        var englishResources = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false);
        var czechResources = resources.GetResourceSet(CultureInfo.GetCultureInfo("cs"), true, false);

        Assert.NotNull(englishResources);
        Assert.NotNull(czechResources);

        var englishKeys = englishResources.Cast<DictionaryEntry>().Select(e => (string)e.Key).OrderBy(k => k);
        var czechKeys = czechResources.Cast<DictionaryEntry>().Select(e => (string)e.Key).OrderBy(k => k);
        Assert.Equal(properties.Select(p => p.Name).OrderBy(k => k), englishKeys);
        Assert.Equal(englishKeys, czechKeys);

        foreach (var property in properties)
        {
            var englishValue = (string)property.GetValue(english);
            var czechValue = (string)property.GetValue(czech);

            Assert.False(string.IsNullOrWhiteSpace(englishValue));
            Assert.False(string.IsNullOrWhiteSpace(czechValue));
            Assert.Equal(englishValue, property.GetValue(unsupported));
        }
    }

    /// <summary>
    /// Resource lookup uses the game selection even when the operating-system UI culture disagrees.
    /// </summary>
    [Theory]
    [InlineData("cs-CZ", null, "Explore")]
    [InlineData("cs-CZ", "German", "Explore")]
    [InlineData("de-DE", "Czech", "Průzkum")]
    public void IgnoresOperatingSystemUiCulture(string uiCulture, string gameLanguage, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);

            Assert.Equal(expected, new GuideText(() => gameLanguage).Explore);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>
    /// Only new automatic names use the current language; existing saved names remain user-owned text.
    /// </summary>
    [Theory]
    [InlineData("English", "Chest 2")]
    [InlineData("Czech", "Truhla 2")]
    [InlineData("German", "Chest 2")]
    [InlineData(null, "Chest 2")]
    public void LocalizesNewChestNamesWithoutRewritingNotes(string language, string expected)
    {
        var old = new ChestRecord { id = "saved", label = "Moje železo / my iron" };
        var registry = new ChestRegistry(new List<ChestRecord> { old }, () => language);
        var created = registry.Observe(new ChestSnapshot { Id = "new", LegacyId = "new" });
        var observed = registry.Observe(new ChestSnapshot { Id = "saved", LegacyId = "saved" });

        Assert.Equal(expected, created.label);
        Assert.Same(old, observed);
        Assert.Equal("Moje železo / my iron", observed.label);
    }
}
