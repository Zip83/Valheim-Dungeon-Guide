# Changelog

## 0.1.1

Development target: **Valheim 1.0.21**, BepInExPack Valheim **5.4.2351**.

- Fix the dungeon map pointer guard incorrectly suppressing native `CreateMapNamePin`.
  Native `UpdatePins` must create name UI before updating its RectTransform; suppressing it
  could flood the log with NullReferenceException and disrupt shared map updates.
- Keep native marker creation, removal and updates available to the game and other map mods.
- Display an initial local map before navigation linking. Preselect possible passage pairs
  on a worker and update fog tiles incrementally, with a configurable 30 m discovery default.
- Track entered room layers independently and highlight them in the floor overview.
- Prepare an optional isolated UI shader with an incremental CPU fallback. The provided
  development package uses CPU rendering; the Unity 6000.0.75 shader bundle is not built here.

Save schema 3 migrates schemas 1/2 without inferring room visits from previously revealed fog.
Release compilation and 227 domain tests pass; gameplay and graphics API checks remain pending.
The independent ShowPlantProgress missing-field error reported in the same log is not fixed by this patch.

## 0.1.0

Development target: **Valheim 1.0.21**, BepInExPack Valheim **5.4.2351**.

- Cooperative sampling/linking with a 1 ms default soft frame budget, cancellable
  background floor analysis, and reusable clearance buffers.
- English interface by default; Czech follows Valheim's Czech language selection.
  Other languages fall back to English, and saved names/notes remain unchanged.
- Automatic activation only inside generated dungeon rooms; outdoor bases retain
  the native map unless manual outdoor mapping is explicitly enabled.
- Native dungeon floor maps and minimap, explored-only by default, with a full scanned view toggle.
- First-open floor fitting, persistent manual viewport, and entrance-relative floor labels.
- Exploration, chest, entrance, and physical adjacent-floor navigation.
- Persistent chest notes, contents snapshots, collection markers, and exploration.
- World-map switching for pins and pings; a manually enabled standalone overlay.
- Opt-in verified breakable, jump, and one-way drop routes.
- Separated domain assembly, automated regression tests, and two-DLL CI artifacts.

Save schema remains version 2 with version 1 migration. Automated checks do not
replace pending in-game verification of this repository build.
