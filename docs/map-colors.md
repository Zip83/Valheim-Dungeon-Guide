# Map colors and stair markers (Valheim 1.0.21)

The large dungeon map, including All floors, displays a wrapping English/Czech legend below its
controls. The optional overlay shows the same legend below the map. Colors are taken directly from
the renderer palette: floor, visited room, fog, wall, player, route, stairs, entrance, chest, collection
target and unverified passage. The legend does not cover the map or capture pointer input.

Both the detail renderer and overview consume actual room-layer visit coverage. In full-map mode,
green fills identify visited room layers; ordinary visible floor retains its base color. In explored
mode the detail map still clips that color by line-of-sight discovery, while All floors may expose
the whole entered room layer. Manual scan regions have no inferred room visits. Saves are unchanged.

Blue up/down chevrons identify geometrically verified stairs, including stairs assigned to a single
architectural floor. A landing may show both directions. Jump/drop candidates are not labeled as
stairs, and hidden markers stay clipped by fog. The initial geometry-only preview has no passage
graph; stair markers become available after navigation preparation.

Automated checks cover stair policies, same-floor raster indexing, both landing directions, and
localization key parity. Native UI wrapping, overlay placement and rendering in Valheim need manual
verification; compiling against game references does not establish those gameplay checks.
