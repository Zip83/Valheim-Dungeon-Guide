# In-game verification

Target: **Valheim 1.0.21**, BepInExPack Valheim **5.4.2351**, plugin **0.2.1**.
Status: pending for this repository build. Record actual results, dungeon type,
other map mods, versions, and relevant log excerpts when performing these checks.

## Loading and map interaction

1. With the game closed, install both project DLLs in a test profile. Avoid duplicate
   plugin copies. Start the game and check `BepInEx/LogOutput.log` for startup or patch errors.
2. Enter a Mistlands mine. The standalone overlay must remain hidden and a completed
   scan must not create a persistent status window. M opens a readable current-floor map.
3. On first opening, the entire floor fits; unknown samples stay hidden in explored
   mode. Zoom and pan, close and reopen: the view stays. Changing floors fits the new
   floor. Home returns to the current floor and fits it.
4. Toggle full scanned geometry and explored-only mode. Check the native minimap
   and optional F6 overlay use the same visibility policy. Restart: overlay is hidden.
5. Alt+M shows the ordinary world map. Add/select pins and exchange pings with a
   teammate. Toggle back and verify native map controls resume normally outside dungeons.

## Floors and navigation

1. At a known entrance, verify floor 0, negative floors below, and positive above.
   Falling to the bottom with a feather cape must not redefine floor 0. Unknown
   entrances must show explicit bottom-up numbering and a labeled scan-origin fallback.
2. In a Deep North fortress with perimeter bridges, use Navigate up/down. A destination
   must reach a broad floor at the requested physical elevation, beyond the stair end;
   a nearby bridge or landing on the same architectural level must not finish the route.
3. Repeat after a rescan. If a destination disappears, report no route rather than
   silently replacing its elevation. Compare detected floors with the actual building.
4. Explore and return to the entrance. Disconnected/unsampled areas must not be
   claimed complete. Verify fog reveals nearby visible geometry without revealing
   stacked floors above or below the character.
5. Check closed/open/locked doors. With optional routes disabled, breakable barriers
   and jumps must not become ordinary walking links. Enable them and verify only
   approved breakables and previously observed directional jumps/drops qualify.
   Check changed geometry, blocked landings, and jump capability changes invalidate routes.

## Chests and persistence

1. Open or aim at a chest and press F7, Shift+F7, and Ctrl+F7. Confirm the actual chest
   is edited without requiring F8 first. Nearby walls and adjacent chests must not
   cause a different chest to be selected.
2. Leave items, add a note, and navigate back via F8. Check its floor indicator and
   content snapshot. Collection confirmation must not move or delete inventory items.
3. Leave/re-enter, then restart the game. Exploration, chest identity, notes, and
   collected markers must persist independently for different worlds/dungeons.
4. In a disposable test profile, check version 1 migration and an invalid/future save.
   The latter must remain unchanged and produce a useful diagnostic, not an empty replacement.

A narrow real floor may be conservatively omitted. Record its geometry instead
of treating fewer detected levels as proof of correct detection. A successful
scan only describes loaded collision geometry; it is not proof of every room being mapped.
