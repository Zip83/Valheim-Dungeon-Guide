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

## Continuous movement regression

Movement refreshes only an immediate 3 m candidate stream. An independent sweep keeps advancing
through the configured radius instead of restarting after every 0.5 m of movement. Alternating the
two streams gives both nearby and distant candidates a bounded share of the existing physics budget.
Geometry/session/floor changes discard both streams; completed sweeps retry blocked samples while
stationary. Distance and physical-floor filters use the current player position, including for older
candidates. Discovery rays originate at the player's actual eye position instead of a fixed 0.65 m
above the floor, while retaining solid collision checks and the same-floor restriction.

The CPU renderer uses the same exploration mask regardless of shader availability. Unknown pixels
must remain fully opaque, and visible distant samples must not require stepping onto each tile.
Automated regressions cover movement starvation, nearby responsiveness, resets, door retries and
opaque unknown masks. Eye-level collision behavior and actual UI appearance require in-game checks
in Valheim 1.0.21, with RevealAll disabled; existing saved exploration remains preserved.
