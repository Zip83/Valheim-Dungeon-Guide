# Incremental fog updates (Valheim 1.0.21)

Discovery defaults to 30 horizontal metres (Exploration.Radius, bounded to 1-100), blocked by walls
and the player's physical floor. Near-first indexed candidates advance every frame under the same
active physics budget as scanning. Blocked samples are retried while stationary. No Unity physics
is run on a worker. Each map consumes a monotonic addition journal independently.

Static floor coverage is cached. Reverse sample/tile indices invalidate only changed 32-pixel tiles,
with a halo matching full inward feather reconstruction. Overview extents consume additions without
rescanning the entire graph. Player markers refresh separately with dirty rectangle uploads.
Unity Apply may upload an entire backing texture, depending on the graphics backend.
Switching from full to explored never retains a full-map image while its replacement is prepared.
Published physical elevation preserves manual zoom/pan when provisional floor numbers change.

This step uses CPU compositing. The surface/material boundary supports subsequent room masks
and the optional shader without altering native game materials. Actual nearest-fog latency,
door interactions, UI rendering and frame timings require in-game measurement.
