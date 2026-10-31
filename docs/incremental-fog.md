# Incremental fog updates (Valheim 1.0.21)

Discovery defaults to a 10-metre horizontal circle (Exploration.Radius, bounded to 1-10), blocked by walls
and the player's physical floor. Near-first indexed candidates advance every frame under the same
active physics budget as scanning. Blocked samples are retried while stationary. No Unity physics
is run on a worker. Each map consumes a monotonic addition journal independently.

Static floor coverage is cached. Reverse sample/tile indices invalidate only changed 32-pixel tiles,
with a halo matching full inward feather reconstruction. Overview extents consume additions without
rescanning the entire graph. Player markers refresh separately with dirty rectangle uploads.
Unity Apply may upload an entire backing texture, depending on the graphics backend.
Switching from full to explored never retains a full-map image while its replacement is prepared.
Published physical elevation preserves manual zoom/pan when provisional floor numbers change.

This step uses CPU compositing. Room masks and colors are composed without changing native game materials. Actual nearest-fog latency,
door interactions, UI rendering and frame timings require in-game measurement.

Existing BepInEx radius values above 10 m are clamped automatically on load; smaller custom values
remain valid. The final reveal test uses horizontal squared distance from the player's current
position, so candidates in the corners of the indexed square cannot reveal outside the circle.
Previously saved exploration remains known. This radius does not change scan coverage, sampling
precision or navigation limits.

## Continuous movement regression

Movement refreshes only an immediate 3 m candidate stream. An independent sweep keeps advancing
through the configured radius instead of restarting after every 0.5 m of movement. Alternating the
two streams gives both nearby and distant candidates a bounded share of the existing physics budget.
Geometry/session/floor changes discard both streams; completed sweeps retry blocked samples while
stationary. Distance and physical-floor filters use the current player position, including for older
candidates. Discovery rays normally originate at the player's actual eye position. If that view is blocked,
a sample within 3 m may use the same 0.65 m floor-level visibility check as navigation snapping.
This prevents a nearby reachable floor sample from remaining an Explore destination solely because
an overhang blocks the eye ray. The extra query reserves another operation from the shared physics
budget; if unavailable, discovery waits for a later sweep. Both views respect solid collision,
the 10 m circular radius and the player's physical floor. A nearby sample behind a solid wall
is not marked explored merely because the player approaches it. Already saved discovery remains known.
Actual overhang collision behavior and stuck-goal gameplay verification remain required.

The CPU renderer consumes the exploration mask directly. Unknown pixels
must remain fully opaque, and visible distant samples must not require stepping onto each tile.
Automated regressions cover movement starvation, nearby responsiveness, resets, door retries and
opaque unknown masks. Eye-level collision behavior and actual UI appearance require in-game checks
in Valheim 1.0.21, with RevealAll disabled; existing saved exploration remains preserved.
