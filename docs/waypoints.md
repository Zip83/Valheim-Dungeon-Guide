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

## Controls

- Alt+F7: save the current player position immediately, including before scan/navigation completes.
- Alt+F8: cycle personal locations across floors and request a return route.
- Alt+Shift+F7: name the selected location with Valheim's native text dialog (up to 80 characters).
- Alt+Ctrl+F7: remove the selected location. Deleting another marker does not stop unrelated navigation.
- Large single-floor dungeon map: Alt+left-click a visible sampled floor interior to add a location;
  click its pink diamond to navigate, Shift+click to name it, right-click to remove it.

Bindings are configurable under Keys. The map's expanded controls show localized hints and the legend
identifies the pink personal-location marker. Markers appear in detail, minimap and overlay on their
physical floor; the active one has a white center. The All floors view selects a floor first.

Pointer placement rejects walls, unsampled voids, hidden FOG geometry and stale preview projections.
Full permits explicit placement on scanned geometry without exploring it; FOG still clips hidden markers.
Route availability follows the existing verified graph and waits for navigation readiness. Markers do
not create passages, teleport players, freeze dropped items or synchronize with other players.
