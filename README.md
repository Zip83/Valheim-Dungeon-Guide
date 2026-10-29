# Valheim Dungeon Guide

A client-side BepInEx mod for navigating Valheim's dungeons, exploring their floors, and finding your way back to chests left behind.

**Development target:** Valheim **1.0.21** with BepInExPack Valheim **5.4.2351**.
See [compatibility and verification](docs/development.md#compatibility-and-verification)
for the recorded environment and test status.

## Planned capabilities

- Dungeon floor maps in the native map view and minimap.
- Explored areas by default, with an optional full scanned floor view.
- Automatic current-floor selection and manual browsing of higher and lower floors.
- Routes toward unexplored areas, marked chests, the entrance, and adjacent floors.
- Persistent exploration, chest notes, and collection markers.
- Switching between the dungeon map and Valheim's world map for pins and multiplayer pings.
- An optional overlay that appears only when manually enabled.

Floor detection and routes depend on the geometry loaded by the game. Unusual layouts can require rescanning or manual navigation; coverage of every room is not guaranteed.

## Contributing

See [Development](docs/development.md) for build commands, project structure,
and CI checks.

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines and
[C# coding conventions](docs/coding-conventions.md) for code structure and formatting.
AI coding assistants must also follow [AGENTS.md](AGENTS.md).

## License

[MIT](LICENSE). Valheim and third-party components remain the property of their respective owners.
