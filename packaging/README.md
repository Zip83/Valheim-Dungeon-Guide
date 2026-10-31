# Valheim Dungeon Guide 0.1.1

Development target: Valheim 1.0.21 with BepInExPack Valheim 5.4.2351.
Gameplay verification of this repository build is pending.

## Installation

Close Valheim. Extract this package's `BepInEx` directory into the game directory.
Keep both `DungeonGuide.dll` and `DungeonGuide.Domain.dll` together in
`BepInEx/plugins/DungeonGuide/`, preserving the `cs/` translation subdirectory
with `DungeonGuide.Domain.resources.dll`. Install BepInExPack first and remove duplicate
copies of this plugin. Disable competing dungeon-map mods in the same profile.

## Build identity

This is a development build of the release version shown above. See
`build-info.json` for its exact development version, commit, PR, CI run, and
Valheim target. The startup log reports the full build identity; BepInEx uses
its numeric equivalent. Include the development version in bug reports.

## Default controls

### Map and floors

- **M** — Open or close the map. Inside a dungeon, it shows the dungeon floor map.
- **Left Alt + M** — Switch between dungeon and ordinary world maps for pins and pings.
- **Page Up / Page Down** — Browse higher or lower floors while the dungeon map or overlay is visible.
- **End / All floors button** — Switch the large dungeon map between a floor
  detail and a scrolling overview. Click a thumbnail to open that floor.
- **Home** — Return to your current floor and fit the entire floor into view.
- **Mouse wheel** — Zoom the floor detail, or scroll through floors in the overview.
- **Hold left mouse button and drag** — Pan the floor detail.
- **F6** — Show or hide the optional standalone map overlay. It starts hidden.
- **Insert** — Rescan dungeon geometry.

The map shows explored areas by default. Cloud fog completely hides unknown rooms;
exploration reveals nearby visible floor within the configured radius (30 metres by default), stopping at walls
and keeping other floors hidden. Saved exploration restores the same fog after re-entry.

The two-row toolbar uses Valheim artwork and fonts. **More** opens advanced
controls: rescan, reveal mode, breakable passages, and verified jumps. It starts
closed whenever the large map opens. Full scanned geometry temporarily removes
fog on the selected floor without adding exploration to your save.

The overview follows the same reveal mode: explored mode lists only discovered
floors and keeps unknown portions under fog; full-map mode lists every scanned
floor. Thumbnails are prepared incrementally. During an initial scan, the
available preview may cover only part of the dungeon. The minimap and optional
overlay continue to show a single floor.

### Navigation

- **F9** — Start navigation toward unexplored reachable areas.
- **Left Shift + F9** — Stop navigation.
- **F10** — Navigate to the entrance. If the entrance is unknown, the destination
  is explicitly labeled as the scan origin.
- **Go up / Go down map buttons** — Navigate to an adjacent floor. Page Up and
  Page Down only change the displayed floor; they do not start navigation.

### Chests and collection

- **F7** — Mark the open or directly targeted chest for later collection.
- **Left Shift + F7** — Edit that chest's name or note.
- **Left Ctrl + F7** — Mark that actual chest as collected.
- **F8** — Cycle known chest navigation destinations.

Open the chest or aim at its interaction prompt before using the F7 shortcuts.
F8 chooses a destination; it does not select the chest edited by F7.
Collection markers record your confirmation and do not transfer inventory items.

## Settings and saved data

- **Configuration and configurable shortcuts:** `BepInEx/config/zip.dungeonguide.cfg`
- **Saved exploration and chest notes:** `BepInEx/config/DungeonGuide/`
- **Diagnostic log:** `BepInEx/LogOutput.log`

**Insert** is the default rescan shortcut; **F5** stays available for the Valheim
console. Existing configurations keep saved shortcuts: under `[Keys]`, change
`Rescan = F5` to `Rescan = Insert` if upgrading from the old default.

Outdoor bases keep the ordinary world map by default. To map outdoors manually,
enable `Display.AllowOutdoorMapping` in the configuration, then press **Insert**.
Enabling this option never starts an outdoor scan automatically.

## Scanning responsiveness

Scanning is spread across frames and leaves gameplay and other plugins running.
The default soft time budget is **1 ms per frame**, configurable through
`Scan.MillisecondsPerFrame` (0.25–4 ms). Lower values spread scans over more frames.
Pure floor analysis runs on a worker; leaving the dungeon cancels the pending scan.
During a rescan, the previous map stays available until the replacement is ready.
A single Unity physics query cannot be interrupted, so actual frame-time impact
still needs in-game verification.

## Language and limitations

The interface defaults to English and uses Czech only when Valheim is set to Czech.
Other game languages fall back to English. Existing chest names and notes stay unchanged.

Maps depend on loaded geometry. Routes and detected floors may be incomplete.
Optional breakable/jump routes are disabled by default. The guide never controls
your character or transfers inventory items.

[Full documentation and source](https://github.com/Zip83/Valheim-Dungeon-Guide)

License: MIT; see LICENSE.

## Responsive map development build

Targets Valheim **1.0.21**. Initial maps no longer wait for navigation linking. Discovery defaults to
30 m and remains blocked by walls and floors. All floors marks actually entered room layers in green.
Saves migrate to schema 3; keep the previous backup for rollback. The optional UI shader uses a separate
`dungeonguide-ui` bundle; without it the plugin uses incremental CPU rendering. Gameplay verification
and GPU comparisons are pending.
