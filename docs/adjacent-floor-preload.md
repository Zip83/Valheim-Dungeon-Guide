# Dungeon floor preloading (Valheim 1.0.21)

The selected display floor runs first under the existing shared soft 1 ms presentation budget.
When following the player, this is the physical floor. Manual browsing immediately prioritizes the
explicitly selected floor. The player's physical floor and its valid upper/lower neighbors form the
next priority group. Missing edge neighbors are skipped. Unfinished members of that group receive
fair alternating slices before any distant floor begins construction.

After the physical neighborhood is ready, increasingly distant floors continue warming progressively.
At most one background floor receives a slice per frame, using only capacity left by foreground work.
Distant warmup alternates with round-robin maintenance of already ready floors, keeping discovery and
visited-room overlays current without starving the remaining dungeon. A selection/physical-floor
change recalculates priorities and preserves all ready and partially built resources.

Every prepared floor retains its raster, textures, fog mask throughout
the active dungeon geometry snapshot. Browsing away from a floor never evicts it. Returning publishes
the same ready image. The cache grows only as construction progresses; it does not allocate every
floor's full-size textures up front. Prefetch never marks samples explored or rooms visited.

Leaving/changing the dungeon or shutting down the plugin releases all cached resources. A rescan or
preview-to-final geometry replacement invalidates resources by graph identity, preventing stale floor
numbers/projections from being reused. Only the previous displayed image may remain temporarily while
its replacement finishes. Physical floor tracking runs before texture selection. The overview cache
is independent and already maintains its own progressively prepared thumbnails.

Full/FOG switching retains geometry and pending work. Both color-image variants remain updated together. Markers refresh separately under their existing visibility
policy. Discovery radius, sample accuracy, navigation readiness, physics limits and save data are unchanged.

The former three-neighbor working-set cap is intentionally removed: memory now scales with prepared
floors in the current snapshot. Each 768-square RGBA texture uses about 2.25 MiB before engine overhead;
Two color images are retained per floor, in addition to raster/mask bookkeeping. Unity allocation/upload calls cannot be preempted by the soft frame budget.

Regression coverage protects complete ordering, boundary floors, manual display priority, neighbor-first
warmup, distant progress, ready-floor maintenance, denied-frame scheduling, retained resources across
repeated browsing, stale-graph eviction and final cleanup. Actual Valheim stair transitions, UI response,
texture memory use and frame times remain manual gameplay checks.
