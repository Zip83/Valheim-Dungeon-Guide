# Valheim Dungeon Guide 0.1.0

Development target: Valheim 1.0.21 with BepInExPack Valheim 5.4.2351.
Gameplay verification of this repository build is pending.

## Installation

Close Valheim. Extract this package's `BepInEx` directory into the game directory.
Keep both `DungeonGuide.dll` and `DungeonGuide.Domain.dll` together in
`BepInEx/plugins/DungeonGuide/`, preserving the `cs/` translation subdirectory
with `DungeonGuide.Domain.resources.dll`. Install BepInExPack first and remove duplicate
copies of this plugin. Disable competing dungeon-map mods in the same profile.

## Default controls

### Map and floors

- **M** — Open or close the map. Inside a dungeon, it shows the dungeon floor map.
- **Left Alt + M** — Switch between dungeon and ordinary world maps for pins and pings.
- **Page Up / Page Down** — Browse higher or lower floors while the dungeon map or overlay is visible.
- **Home** — Return to your current floor and fit the entire floor into view.
- **Mouse wheel** — Zoom the full dungeon map in or out.
- **Hold left mouse button and drag** — Pan the full dungeon map.
- **F6** — Show or hide the optional standalone map overlay. It starts hidden.
- **F5** — Rescan dungeon geometry.

The map shows explored areas by default. Use its reveal-mode button to switch
between explored areas and all scanned geometry on the selected floor.

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

Outdoor bases keep the ordinary world map by default. To map outdoors manually,
enable `Display.AllowOutdoorMapping` in the configuration, then press **F5**.
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
