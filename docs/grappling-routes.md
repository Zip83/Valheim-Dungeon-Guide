# Grappling hook fallback

Target: Valheim 1.0.21, BepInEx 5.4.23.5.

Navigation first searches the existing verified graph, including observed jumps
and directional drops. Only when this returns no route does it search for a
hook-assisted crossing. A shorter hook shortcut never replaces an available
ordinary route. Breakable passages still require their existing permission.

The player must carry a usable hook with sufficient durability and launch stamina.
Capabilities come from the inventory item's actual attack, projectile, and native
`GrapplingPoint` prefab, rather than a guessed item name or fixed range. The search
checks a standable supported landing, body clearance along the straight approach,
and a static surface beyond the landing that accepts the native hook attachment.
Native spawn offsets, launch speed, gravity, lifetime, and collision radius define
the low ballistic arc, which is checked for intervening geometry.
Doors, moving bodies, characters, and `GrapplingBlocker` surfaces are rejected.

The shared scan/exploration frame budget bounds connectivity indexing, candidate
columns, and physics checks. Changing the goal, scan, or session cancels pending
work. The search does not add hook edges to the published floor graph or save them
as verified jumps. Losing the hook cancels the fallback. Selected hook geometry is
rechecked periodically; ordinary routing is retried first on every route refresh.

## Presentation

Ordinary route sections remain yellow. Hook sections are orange, with an orange
cross marking the checked attachment surface (allow for the projectile arc when
aiming). A hook crossing between floors is
shown on its launch and landing floors. Fog continues to clip all markers; a hook
route does not reveal unexplored geometry. Route strokes are five texture pixels
wide with a seven-pixel dark outline, compared with the former two-pixel stroke.
The legend and prominent navigation instructions explain the hook requirement in
English and Czech. Equip and use the hook manually; the guide never fires it,
changes equipment, or moves the character.

## Scope and verification

The initial fallback supports **one straight hook crossing** between ordinary
connected components. Chained hooks, swinging around corners, and projectile
prefabs with drag, random velocity, charge-dependent launches, or unsupported
spawn joints are not inferred. A supported landing and clear pull
line are conservative geometric checks, not a simulation of every launch and
release outcome. Some physically possible hook maneuvers will remain unavailable.

Domain regressions cover ordinary-route preference, directed approach/departure,
verified jumps, breakable permissions, budget yields, cancellation, and graph
immutability. Local Unity physics fixtures exercise elevated landings, walls,
attachment blockers,
ordinary-route preference, inventory loss, session cancellation, and stale work, with
stubbed Valheim inventory/player components. Compilation checks native game
members against the installed game
references. These do not replace gameplay verification. The entrance climb,
hook aim marker, release/landing behavior, dynamic obstacles, and minimap/overlay
readability still require manual testing in Valheim 1.0.21.
