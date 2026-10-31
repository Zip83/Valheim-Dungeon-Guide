# Derived dungeon geometry cache

Geometry cache version 1 is separate from the existing versioned JSON save.
It does not migrate or replace exploration, chest records, personal locations,
room visits or observed traversal proofs. Those remain in the existing save.

The bounded binary payload contains an identity, sampling spacing, completion
and truncation flags, an optional observed doorway arrival, architectural floor
elevations, supported landing indices, sampled floor positions and directed
passages. Text uses length-prefixed UTF-8. Counts, finite coordinates, passage
kinds, node references and floor assignments are validated before publication.
Limits are 250,000 nodes, 4,000,000 edges, 4,096 floors and 64 MiB of payload.

Only walk and stair verification can survive storage. Door and breakable
permissions must be checked against the loaded world and current player.
Jump/drop edges require the existing persisted traversal evidence and current
geometry/capability validation. Stored geometry alone grants no special action.

The integration layer fingerprints world, dungeon, installed game version,
sampling inputs and sorted room identities/bounds. A different fingerprint
cannot load the previous layout as the current dungeon. Full scan graphs remain
private during worker analysis and encoding; only sealed bytes reach disk I/O.
Published preview graphs are copied cooperatively on the game thread.

Storage appends SHA-256, writes a unique temporary file and atomically replaces
the destination while retaining a `.bak`. An invalidation marker prevents old
geometry from becoming authoritative after a known structural change. Corrupt
files are preserved before recovery; unsupported/newer formats are protected
from replacement, including when an invalidation marker already exists.
