# Personal return locations (Valheim 1.0.21)

Schema 4 adds `waypoints` to each world/dungeon save. Each record contains an independent stable `id`,
a player-owned `label` and a physical world `position`, including elevation. It is not a container or
an item tracker. Dropped items can still disappear under the game's normal rules; the marker records
only where the player asked to return.

Migration preserves exploration, actual room visits, chests, notes and traversal records. Legacy
schemas acquire an empty list; no location is inferred from explored geometry. Newer schemas and
invalid/duplicate location records are rejected through the existing read-only/corrupt-save protection.
Existing atomic replacement and backup behavior remains in use. Older plugin builds reject schema 4
rather than overwriting its unknown data. Personal locations are local and are not synchronized to peers.
