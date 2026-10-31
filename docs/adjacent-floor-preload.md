# Adjacent floor preloading (Valheim 1.0.21)

The detail/minimap renderer retains the player's physical floor and its existing immediate neighbors.
The lowest floor has only an upper neighbor, the highest only a lower neighbor, and a single-floor
snapshot does not request background work. Manual browsing keeps its selected floor as an additional
foreground entry without replacing the physical neighborhood. The overview thumbnail cache remains
independent.

The selected floor runs first under the existing shared 1 ms presentation budget. Once its first
image is ready, at most one background floor receives a slice of the remaining budget per frame.
Round-robin ordering prevents either neighbor from starving. Entries retain display geometry without
keeping obsolete scan-column/pair indices. Each entry retains its completed raster,
texture, fog mask. Entering an already prepared floor publishes that
same image without repeating raster construction. Prefetch reads exploration; it never marks geometry
as discovered or rooms as visited. Dynamic player/route markers continue updating separately.

The working set is bounded to three physical-neighborhood floors, plus one manually browsed floor.
A previously displayed image may temporarily add one protected entry while its replacement is being
prepared. Old neighbors and obsolete graph builds are disposed; the displayed old snapshot stays only
until replacement publication. New sessions release every entry. Graph identity prevents preview/full
floor-number collisions from reusing the wrong geometry, and cached physical heights preserve the
existing viewport behavior. The player-floor tracking step runs before texture selection.

Switching full/explored visibility modes immediately discards every cached composited image and marker
layer, while keeping static raster work. This prevents full-map pixels appearing through explored-mode
fog. Physics scan limits, navigation readiness, discovery radius, saved data and sample accuracy are
unchanged. The presentation time limit remains soft: Unity allocation/upload calls cannot be preempted.

Regression coverage checks boundary indices, manual selection, foreground priority, exhausted budgets,
fair neighbor admission, actual completed raster reuse, stale-graph eviction and resource cleanup.
In-game stair transitions, native UI behavior, texture memory use and real frame times remain manual checks;
passing domain tests and compiling against Valheim references are not gameplay performance evidence.

An isolated Unity 6000.0.75 CPU fixture also verified actual texture reuse, static-raster retention
while invalidating visibility-mode images, opaque fog after switching from Full, stale-work rejection,
old-display retirement and final texture cleanup. It uses minimal session DTOs and editor-compatible
immediate texture destruction, so this evidence is distinct from Valheim UI gameplay tests.
