# Entered room layers (Valheim 1.0.21)

Actual entry anchors are recorded in frozen oriented room coordinates before scan completion.
Persistent identities include dungeon-scoped room pose/prefab identity and local entry elevation,
not transient ascending floor numbers. Full layout publication reprojects visits after renumbering.
An unknown upper entry cannot mark a downstairs-only preview as visited. Manual scan volumes have
no inferred rooms.

All floors shows entered room layers in green. Explored mode shows the union of collision-visible
samples and the known floor footprint of entered rooms; full mode keeps unvisited geometry muted.
Detail and minimap retain collision-visible fog. Only sampled floor coverage is colored, not the
room's empty bounding rectangle. Discovery additions and room visits update independent coverage.

Save schema 3 adds visitedRooms records with room and local fields. Schema 1/2 upgrades preserve
existing exploration, notes, chests and traversal proofs, and initialize visits empty. Previously
revealed fog is not evidence of entering the entire room. Atomic saves and future/corrupt-save
read-only protections remain. Preserve a pre-upgrade backup if testing an older plugin again.

Unit tests cover rotated containment, layer remapping, migration and stable serialized contracts.
Gameplay interaction, room colors and multi-floor geometry still require manual verification.
