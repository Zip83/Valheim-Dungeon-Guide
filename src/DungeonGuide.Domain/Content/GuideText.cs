using System;

namespace DungeonGuide;

/// <summary>
/// Selects Czech player-facing copy only for Valheim's Czech language, with English as the fallback.
/// </summary>
internal sealed class GuideText
{
    private readonly Func<string> _selectedLanguage;

    internal GuideText(Func<string> selectedLanguage = null)
    {
        _selectedLanguage = selectedLanguage;
    }

    internal string SelectedLanguage => _selectedLanguage?.Invoke();

    internal bool IsCzech => string.Equals(SelectedLanguage, "Czech", StringComparison.OrdinalIgnoreCase);

    internal string ChestTargetRequired => IsCzech
        ? "Zamiř na truhlu s nabídkou otevření, nebo ji otevři."
        : "Aim at a chest with an interaction prompt, or open it.";

    internal string ChestSaved => IsCzech ? "Uloženo: " : "Saved: ";

    internal string NoKnownChest => IsCzech
        ? "Žádná známá neprázdná ani označená truhla."
        : "No known non-empty or marked chest.";

    internal string NoAdjacentFloor => IsCzech
        ? "Další patro v tomto směru nebylo nalezeno."
        : "No next floor found in this direction.";

    internal string SaveReadFailed => IsCzech
        ? "Uložený průzkum nelze načíst. Soubor zůstává beze změny; podrobnosti v logu."
        : "Cannot load exploration. The file is unchanged; see the log.";

    internal string SaveWriteFailed => IsCzech
        ? "Průzkum se nepodařilo uložit. Podrobnosti v logu."
        : "Cannot save exploration. See the log.";

    internal string ChestNotePrompt => IsCzech ? "Název / poznámka k truhle" : "Chest name / note";

    internal string ItemCountSeparator => "× ";

    internal string ManualArea => IsCzech ? "Ruční oblast" : "Manual area";

    internal string OpenDoor => IsCzech ? "Otevři dveře" : "Open the door";

    internal string BreakObstacle => IsCzech ? "Rozbij překážku: " : "Break the obstacle: ";

    internal string RouteBlocked => IsCzech
        ? "Průchod blokován — přepočítávám cestu"
        : "Passage blocked — recalculating route";

    internal string WorldMap => IsCzech ? "Svět · Alt+M" : "World · Alt+M";

    internal string FloorDown => IsCzech ? "− patro" : "− floor";

    internal string FloorUp => IsCzech ? "+ patro" : "+ floor";

    internal string FollowFloor => IsCzech ? "Moje patro" : "My floor";

    internal string Explored => IsCzech ? "Prozkoumané" : "Explored";

    internal string NavigateUp => IsCzech ? "Vést výš ↑" : "Go up ↑";

    internal string NavigateDown => IsCzech ? "Vést níž ↓" : "Go down ↓";

    internal string Explore => IsCzech ? "Průzkum" : "Explore";

    internal string Entrance => IsCzech ? "Vchod" : "Entrance";

    internal string Stop => IsCzech ? "Zastavit" : "Stop";

    internal string Rescan => IsCzech ? "Obnovit sken" : "Rescan";

    internal string Chests => IsCzech ? "Truhly · F8" : "Chests · F8";

    internal string ChestControls => IsCzech
        ? "F7 značka · Shift+F7 název · Ctrl+F7 vyzvednuto"
        : "F7 mark · Shift+F7 name · Ctrl+F7 collected";

    internal string DungeonMap => "Dungeon · Alt+M";

    internal string PreparingMap => IsCzech ? "Připravuji mapu…" : "Preparing map…";

    internal string FullDungeon => IsCzech ? "Celý dungeon" : "Full dungeon";

    internal string BreakablePassages => IsCzech ? "Rozbitné průchody: " : "Breakable passages: ";

    internal string VerifiedJumps => IsCzech ? "Ověřené skoky: " : "Verified jumps: ";

    internal string Yes => IsCzech ? "ANO" : "YES";

    internal string No => IsCzech ? "NE" : "NO";

    internal string MapPanelsUnavailable => IsCzech
        ? "Valheim mapové panely nebyly nalezeny."
        : "Valheim map panels were not found.";

    internal string EntranceRoom => IsCzech ? "Vstupní místnost" : "Entrance room";

    internal string UnknownEntrance => IsCzech ? "Začátek skenu — vchod neznámý" : "Scan origin — entrance unknown";

    internal string ApproachPassage => IsCzech
        ? "Přibliž se k průchodu nebo obnov sken."
        : "Approach a passage or rescan.";

    internal string ExplorationExhausted => IsCzech
        ? "Dostupné části projity; úplnost dungeonu není potvrzená."
        : "Reachable areas explored; dungeon completeness is unconfirmed.";

    internal string NoVerifiedRoute => IsCzech
        ? "Ověřená cesta nenalezena. Otevři průchod nebo obnov sken."
        : "No verified route found. Open a passage or rescan.";

    internal string PreparingRoute => IsCzech ? "Připravuji trasu…" : "Preparing route…";

    internal string NearDestination => IsCzech ? "Cíl poblíž" : "Destination nearby";

    internal string JumpInstruction => IsCzech ? "Skoč k označenému dopadu" : "Jump to the marked landing";

    internal string DropInstruction => IsCzech
        ? "Jednosměrný seskok — návrat nemusí být možný"
        : "One-way drop — a return route may not exist";

    internal string Straight => IsCzech ? "Rovně" : "Straight ahead";

    internal string TurnAround => IsCzech ? "Otoč se" : "Turn around";

    internal string Right => IsCzech ? "Vpravo" : "Right";

    internal string Left => IsCzech ? "Vlevo" : "Left";

    internal string UpIndicator => " ↑";

    internal string DownIndicator => " ↓";

    internal string Separator => " · ";

    internal string OverlayControls => IsCzech
        ? "F6 skrýt · PgUp/PgDn patro · Home moje patro"
        : "F6 hide · PgUp/PgDn floor · Home my floor";

    internal string FloorUnknown => IsCzech ? "Patro není určeno" : "Floor unknown";

    internal string EntranceFloor => IsCzech ? "Vstupní patro 0" : "Entrance floor 0";

    internal string FloorNegative => IsCzech ? "Patro −" : "Floor −";

    internal string FloorPositive => IsCzech ? "Patro +" : "Floor +";

    internal string FloorPrefix => IsCzech ? "Patro " : "Floor ";

    internal string BottomUp => IsCzech ? " (odspodu)" : " (from bottom)";

    internal string CollectChest => IsCzech ? "Vyzvednout: " : "Collect: ";

    internal string ChestDestination => IsCzech ? "Truhla: " : "Chest: ";

    internal string ChestPrefix => IsCzech ? "Truhla " : "Chest ";
}
