# Changelog

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
