using System;
using System.Globalization;
using System.Resources;

namespace DungeonGuide;

/// <summary>
/// Resolves typed text keys through .NET resources using Valheim's language and an English fallback.
/// </summary>
internal sealed class GuideText
{
    private static readonly ResourceManager s_resources =
        new("DungeonGuide.Domain.Content.Strings", typeof(GuideText).Assembly);
    private static readonly CultureInfo s_czech = CultureInfo.GetCultureInfo("cs");
    private readonly Func<string> _selectedLanguage;

    internal GuideText(Func<string> selectedLanguage = null)
    {
        _selectedLanguage = selectedLanguage;
    }

    internal string SelectedLanguage => _selectedLanguage?.Invoke();

    internal bool IsCzech => string.Equals(SelectedLanguage, "Czech", StringComparison.OrdinalIgnoreCase);

    internal string ChestTargetRequired => Get(nameof(ChestTargetRequired));

    internal string ChestSaved => Get(nameof(ChestSaved));

    internal string NoKnownChest => Get(nameof(NoKnownChest));

    internal string NoAdjacentFloor => Get(nameof(NoAdjacentFloor));

    internal string SaveReadFailed => Get(nameof(SaveReadFailed));

    internal string SaveWriteFailed => Get(nameof(SaveWriteFailed));

    internal string ChestNotePrompt => Get(nameof(ChestNotePrompt));

    internal string ItemCountSeparator => Get(nameof(ItemCountSeparator));

    internal string ManualArea => Get(nameof(ManualArea));

    internal string OpenDoor => Get(nameof(OpenDoor));

    internal string BreakObstacle => Get(nameof(BreakObstacle));

    internal string RouteBlocked => Get(nameof(RouteBlocked));

    internal string WorldMap => Get(nameof(WorldMap));

    internal string FloorDown => Get(nameof(FloorDown));

    internal string FloorUp => Get(nameof(FloorUp));

    internal string FollowFloor => Get(nameof(FollowFloor));

    internal string Explored => Get(nameof(Explored));

    internal string NavigateUp => Get(nameof(NavigateUp));

    internal string NavigateDown => Get(nameof(NavigateDown));

    internal string Explore => Get(nameof(Explore));

    internal string Entrance => Get(nameof(Entrance));

    internal string Stop => Get(nameof(Stop));

    internal string Rescan => Get(nameof(Rescan));

    internal string Chests => Get(nameof(Chests));

    internal string ChestControls => Get(nameof(ChestControls));

    internal string DungeonMap => Get(nameof(DungeonMap));

    internal string PreparingMap => Get(nameof(PreparingMap));

    internal string FullDungeon => Get(nameof(FullDungeon));

    internal string BreakablePassages => Get(nameof(BreakablePassages));

    internal string VerifiedJumps => Get(nameof(VerifiedJumps));

    internal string Yes => Get(nameof(Yes));

    internal string No => Get(nameof(No));

    internal string MapPanelsUnavailable => Get(nameof(MapPanelsUnavailable));

    internal string EntranceRoom => Get(nameof(EntranceRoom));

    internal string UnknownEntrance => Get(nameof(UnknownEntrance));

    internal string ApproachPassage => Get(nameof(ApproachPassage));

    internal string ExplorationExhausted => Get(nameof(ExplorationExhausted));

    internal string NoVerifiedRoute => Get(nameof(NoVerifiedRoute));

    internal string PreparingRoute => Get(nameof(PreparingRoute));

    internal string NearDestination => Get(nameof(NearDestination));

    internal string JumpInstruction => Get(nameof(JumpInstruction));

    internal string DropInstruction => Get(nameof(DropInstruction));

    internal string Straight => Get(nameof(Straight));

    internal string TurnAround => Get(nameof(TurnAround));

    internal string Right => Get(nameof(Right));

    internal string Left => Get(nameof(Left));

    internal string UpIndicator => Get(nameof(UpIndicator));

    internal string DownIndicator => Get(nameof(DownIndicator));

    internal string Separator => Get(nameof(Separator));

    internal string OverlayControls => Get(nameof(OverlayControls));

    internal string FloorUnknown => Get(nameof(FloorUnknown));

    internal string EntranceFloor => Get(nameof(EntranceFloor));

    internal string FloorNegative => Get(nameof(FloorNegative));

    internal string FloorPositive => Get(nameof(FloorPositive));

    internal string FloorPrefix => Get(nameof(FloorPrefix));

    internal string BottomUp => Get(nameof(BottomUp));

    internal string CollectChest => Get(nameof(CollectChest));

    internal string ChestDestination => Get(nameof(ChestDestination));

    internal string ChestPrefix => Get(nameof(ChestPrefix));

    internal string More => Get(nameof(More));

    internal string Less => Get(nameof(Less));

    internal string Overview => Get(nameof(Overview));

    internal string FloorDetail => Get(nameof(FloorDetail));

    internal string NoExploredFloors => Get(nameof(NoExploredFloors));

    internal string OverviewHint => Get(nameof(OverviewHint));

    internal string PreviewSuffix => Get(nameof(PreviewSuffix));

    private string Get(string key) =>
        s_resources.GetString(key, IsCzech ? s_czech : CultureInfo.InvariantCulture)
        ?? throw new MissingManifestResourceException($"Missing localized text: {key}");
}
