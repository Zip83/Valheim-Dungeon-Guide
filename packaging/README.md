# Valheim Dungeon Guide 0.2.1

Development target: Valheim 1.0.21 with BepInExPack Valheim 5.4.2351.
Gameplay verification of this repository build is pending.

Close Valheim. Extract this package's BepInEx directory into the game directory.
Keep both DungeonGuide.dll and DungeonGuide.Domain.dll together in
BepInEx/plugins/DungeonGuide/. Install BepInExPack first and remove duplicate
copies of this plugin. Disable competing dungeon-map mods in the same profile.

M opens the dungeon map. Alt+M switches to the ordinary world map for pins and pings.
Page Up/Down browse floors; Home follows and fits your current floor.
F6 toggles the optional overlay, hidden by default. Explored-only visibility is the default;
use the map button to show all scanned geometry on the selected floor.

F5 rescans. F7 marks the open or directly targeted chest; Shift+F7 adds a name/note;
Ctrl+F7 confirms collection at that actual chest. F8 cycles navigation destinations.
F9 starts exploration, Shift+F9 stops navigation, and F10 returns to the entrance
(or explicitly labeled scan origin when the entrance is unknown).

Settings: BepInEx/config/zip.dungeonguide.cfg
Saved exploration and chest notes: BepInEx/config/DungeonGuide/
Diagnostics: BepInEx/LogOutput.log

Maps depend on loaded geometry. Routes and detected floors may be incomplete.
Optional breakable/jump routes are disabled by default. The guide never controls
your character or transfers inventory items. The current interface is Czech.

Full documentation and source:
https://github.com/Zip83/Valheim-Dungeon-Guide

License: MIT; see LICENSE.
