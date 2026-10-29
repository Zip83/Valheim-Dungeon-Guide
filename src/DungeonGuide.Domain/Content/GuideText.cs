namespace DungeonGuide;

/// <summary>
/// Centralizes the current Czech player-facing copy independently of routing and game API access.
/// </summary>
internal static class GuideText
{
    internal const string ChestTargetRequired = "Zamiř na truhlu s nabídkou otevření, nebo ji otevři.";
    internal const string ChestSaved = "Uloženo: ";
    internal const string NoKnownChest = "Žádná známá neprázdná ani označená truhla.";
    internal const string NoAdjacentFloor = "Další patro v tomto směru nebylo nalezeno.";
    internal const string SaveReadFailed = "Uložený průzkum nelze načíst. Soubor zůstává beze změny; podrobnosti v logu.";
    internal const string SaveWriteFailed = "Průzkum se nepodařilo uložit. Podrobnosti v logu.";
    internal const string ChestNotePrompt = "Název / poznámka k truhle";
    internal const string ItemCountSeparator = "× ";
    internal const string ManualArea = "Ruční oblast";
    internal const string OpenDoor = "Otevři dveře";
    internal const string BreakObstacle = "Rozbij překážku: ";
    internal const string RouteBlocked = "Průchod blokován — přepočítávám cestu";
    internal const string WorldMap = "Svět · Alt+M";
    internal const string FloorDown = "− patro";
    internal const string FloorUp = "+ patro";
    internal const string FollowFloor = "Moje patro";
    internal const string Explored = "Prozkoumané";
    internal const string NavigateUp = "Vést výš ↑";
    internal const string NavigateDown = "Vést níž ↓";
    internal const string Explore = "Průzkum";
    internal const string Entrance = "Vchod";
    internal const string Stop = "Zastavit";
    internal const string Rescan = "Obnovit sken";
    internal const string Chests = "Truhly · F8";
    internal const string ChestControls = "F7 značka · Shift+F7 název · Ctrl+F7 vyzvednuto";
    internal const string DungeonMap = "Dungeon · Alt+M";
    internal const string PreparingMap = "Připravuji mapu…";
    internal const string FullDungeon = "Celý dungeon";
    internal const string BreakablePassages = "Rozbitné průchody: ";
    internal const string VerifiedJumps = "Ověřené skoky: ";
    internal const string Yes = "ANO";
    internal const string No = "NE";
    internal const string MapPanelsUnavailable = "Valheim mapové panely nebyly nalezeny.";
    internal const string EntranceRoom = "Vstupní místnost";
    internal const string UnknownEntrance = "Začátek skenu — vchod neznámý";
    internal const string ApproachPassage = "Přibliž se k průchodu nebo obnov sken.";
    internal const string ExplorationExhausted = "Dostupné části projity; úplnost dungeonu není potvrzená.";
    internal const string NoVerifiedRoute = "Ověřená cesta nenalezena. Otevři průchod nebo obnov sken.";
    internal const string PreparingRoute = "Připravuji trasu…";
    internal const string NearDestination = "Cíl poblíž";
    internal const string JumpInstruction = "Skoč k označenému dopadu";
    internal const string DropInstruction = "Jednosměrný seskok — návrat nemusí být možný";
    internal const string Straight = "Rovně";
    internal const string TurnAround = "Otoč se";
    internal const string Right = "Vpravo";
    internal const string Left = "Vlevo";
    internal const string UpIndicator = " ↑";
    internal const string DownIndicator = " ↓";
    internal const string Separator = " · ";
    internal const string OverlayControls = "F6 skrýt · PgUp/PgDn patro · Home moje patro";
    internal const string FloorUnknown = "Patro není určeno";
    internal const string EntranceFloor = "Vstupní patro 0";
    internal const string FloorNegative = "Patro −";
    internal const string FloorPositive = "Patro +";
    internal const string FloorPrefix = "Patro ";
    internal const string BottomUp = " (odspodu)";
    internal const string CollectChest = "Vyzvednout: ";
    internal const string ChestDestination = "Truhla: ";
    internal const string ChestPrefix = "Truhla ";
}
