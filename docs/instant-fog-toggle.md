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
