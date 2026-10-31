# Immediate Full / FOG switching (Valheim 1.0.21)

Ready floor surfaces retain geometry, exploration and pending tile work across mode changes.
Two CPU-composited color images are maintained together; changing visibility selects an already
uploaded Full or FOG texture without rebuilding the raster or repainting the map. Each completed
tile updates both images, including while Full is active. Repeated toggling cannot restart tile work.

Initial readiness waits for both variants. Geometry that has never been prepared still needs its
first cooperative build. Existing budgets and sampling accuracy remain unchanged. Reused tile
buffers avoid per-tile allocation; the retained second RGBA image allows immediate reverse switches.

Physical floor, zoom/pan, visited colors and marker visibility follow their existing policies.
Session or geometry replacement releases owned resources. Isolated CPU fixtures check texture
identity, progress during repeated toggling and visible/visited colors; actual gameplay timing and
native UI behavior still require Valheim verification.

## All floors

Full and known floor catalogs are prepared together and kept across mode changes. Reading the other
view does not rescan nodes, and known framing still excludes undiscovered corridor bounds. Hidden
floor images and cards warm progressively under the existing budget; hidden cards are inactive and
cannot be selected in FOG mode. Mode changes hide/show retained cards instead of destroying them.

Each thumbnail first prepares a full-floor projection with both visibility variants. A discovered-only
view can immediately crop its fog image to the known world-space extent. An optional higher-resolution
known-area projection is retained and updated using spare budget, preserving the original framing and
image detail. Crops beyond the full texture use its clamped opaque fog border; they cannot reveal
geometry outside known coverage. Once a thumbnail is visible, its Full image has already been prepared.

Newly discovered extents may fall back to the ready full-floor fog crop while their sharper projection
catches up. Initially unprepared floor images and cards still require cooperative warmup. The overview
uses the existing soft budget, with the player's floor prioritized. No floor metadata is made visible
merely because its image/card was prepared in the background.
