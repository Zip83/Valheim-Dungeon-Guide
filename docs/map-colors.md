# Map colors and stair markers (Valheim 1.0.21)

The Legend button opens the English/Czech color legend on request in the large dungeon map.
Colors come directly from the renderer palette: floor, visited room (FULL only), fog, wall,
player, route, hook, stairs, entrance, personal location, chest, collection target, and unverified passage.

Both the detail renderer and overview consume actual room-layer visit coverage. In full-map mode,
green fills identify visited room layers; ordinary visible floor retains its base color. In explored
mode both detail and All floors keep the ordinary floor color under fog, without a green tint.
All floors may still expose the whole entered room layer; visit tracking and reveal coverage are
unchanged. Unexplored areas use dark charcoal cloud shading with a soft reveal edge,
independent of hidden floor geometry; the Full background remains unchanged.
Manual scan regions have no inferred room visits. Saves are unchanged.

Thick blue up/down arrows with dark navy outlines identify geometrically verified stairs,
including stairs assigned to a single architectural floor. A landing may show both directions.
Jump/drop candidates are not labeled as
stairs, and hidden markers stay clipped by fog. The initial geometry-only preview has no passage
graph; stair markers become available after navigation preparation.

Automated checks cover stair policies, same-floor raster indexing, both landing directions, and
localization key parity. Native UI wrapping, overlay placement and rendering in Valheim need manual
verification; compiling against game references does not establish those gameplay checks.

The player pointer is larger, with a dark outline, softly shaded white fill and antialiased edges.
It is centered at the player's position and follows the original character heading. Detail, minimap
and overlay share this marker layer. Glyph pixels are cached until heading changes by one degree;
movement only stamps the existing glyph at its new position. Other-floor browsing still hides it.
