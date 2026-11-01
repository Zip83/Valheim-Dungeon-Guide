# Valheim Dungeon Guide

Development target: Valheim 1.0.21 with BepInExPack Valheim 5.4.2351.
Gameplay verification of this repository build is pending.

## Installation

With a mod manager, install this package and its BepInExPack dependency into your
Valheim profile, then launch through the manager. Server installation is not required.

For manual installation, close Valheim. Extract this package's `BepInEx` directory into the game directory.
Keep both `DungeonGuide.dll` and `DungeonGuide.Domain.dll` together in
`BepInEx/plugins/DungeonGuide/`, preserving the `cs/` translation subdirectory
with `DungeonGuide.Domain.resources.dll`. Install BepInExPack first and remove duplicate
copies of this plugin. Disable competing dungeon-map mods in the same profile.

## Build identity

CI packages are development builds, not automatically published stable releases.
The Thunderstore package version comes from the repository release prefix. See
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

The map shows explored areas by default. Dark fog hides unknown areas;
exploration reveals visible floor in a circle (10-metre radius by default), stopping at walls
and keeping other floors hidden. Saved exploration restores the same fog after re-entry.

The two-row toolbar uses Valheim artwork and fonts. **More** opens advanced
keyboard and mouse hints. It starts closed whenever the large map opens.
Verified jumps and breakable routes are automatic; there are no manual rescan or
traversal switches. Full scanned geometry temporarily removes
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
- **Go up / Go down map buttons** — Keep navigating upward or downward until
  stopped or replaced by another destination. At the known floor limit, navigation
  stays active and can resume when more geometry becomes available. Page Up and
  Page Down only change the displayed floor; they do not start navigation.

### Chests and collection

- **F7** — Mark the open or directly targeted chest for later collection.
- **Left Shift + F7** — Edit that chest's name or note.
- **Left Ctrl + F7** — Mark that actual chest as collected.
- **F8** — Cycle known chest navigation destinations.

Open the chest or aim at its interaction prompt before using the F7 shortcuts.
F8 chooses a destination; it does not select the chest edited by F7.
Collection markers record your confirmation and do not transfer inventory items.

### Personal return locations

Personal return locations: Alt+F7 marks here, Alt+F8 cycles return targets, Alt+Shift+F7 names
the selected location and Alt+Ctrl+F7 removes it. On a single-floor dungeon map use Alt+click
to add a pink diamond, click to navigate, Shift+click to name and right-click to remove it.
Locations persist per world/dungeon in schema 4; they do not prevent dropped item despawning.

## Settings and saved data

- **Configuration and configurable shortcuts:** `BepInEx/config/zip.dungeonguide.cfg`
- **Saved exploration and chest notes:** `BepInEx/config/DungeonGuide/`
- **Diagnostic log:** `BepInEx/LogOutput.log`

Geometry scanning and refresh are automatic. There is no manual rescan button
or shortcut. Outdoor bases retain the ordinary world map.

Completed layouts are cached across dungeon exits and game restarts. Current-floor
textures are prepared first, then adjacent floors and the remaining floors.
Prepared textures are retained during the dungeon session. Structural changes
refresh geometry automatically; passage permissions are rechecked separately.

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
Verified jumps and breakable routes are automatic. The guide never controls
your character or transfers inventory items.
Directional jumps require a previously observed successful crossing and current
geometry/capability checks. A crossing never authorizes its reverse direction.
When ordinary routing fails, a usable grappling hook can enable a single checked
crossing. Orange route sections and crosses identify the hook and its anchor;
the HUD warns explicitly. Ordinary sections are yellow; all routes have thicker,
outlined strokes. Hook firing and release remain manual. Swinging, chained hooks,
and modded drag/random/charged projectile attacks are not inferred.

[Full documentation and source](https://github.com/Zip83/Valheim-Dungeon-Guide)

License: MIT; see LICENSE.

## Responsive maps

Target Valheim 1.0.21. Maps use incremental CPU rendering and exploration defaults to a 10 m circle,
limited by walls and physical floors. The package contains the plugin DLLs, Czech translations,
installation notes and exact build identity. Preserve the cs translation directory when installing.

The **Legend** button opens a separate, scrollable color legend with native fonts
and actual color swatches. Close it with **Close** or by clicking outside the panel.
The legend starts hidden and closes when leaving the large dungeon map; map pointer
interaction is blocked while it is open.
