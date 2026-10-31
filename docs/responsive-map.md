# Responsive dungeon maps (Valheim 1.0.21)

## Behavior and compatibility

First-entry previews sample a bounded neighborhood without passage linking or navigation floor analysis.
The provisional layer contains samples within 1.2 m of the nearest floor under the player; it is not
an architectural-floor guarantee. Full sampling, worker-thread endpoint preselection, passage linking and worker-thread architectural analysis
continue independently. Navigation requests remain pending until the complete graph is ready.
The first preview is available in both explored and full-map modes. Published graphs are not modified
by a background scan. Cancellation disposes unpublished work.

`Exploration.Radius` defaults to **30 metres** (1–100), following gameplay feedback. The native game
radius is 100 m; plugin discovery deliberately remains blocked by collisions and physical floors.
The old 3 m radius and 0.35 s tracking delay are removed. Near-first indexed discovery and scan physics
share an active-work budget; unrelated game execution is excluded from its accounting. Blocked samples
are retried after each sweep, including while the player stands at a door. Large sweeps can take more
than one frame. No global scene query or physics operation is run on a worker thread. Endpoint preselection reads only
private sampled positions and removes pairs outside the existing slope/jump envelopes before physics linking.

Overview colors distinguish entered room layers (green) from unvisited scanned geometry (muted blue).
Explored mode shows the union of normal fog discovery and the known floor footprint of entered rooms.
Detail and minimap still use collision-visible discovery only. Manual volumes have no inferred rooms.
Room containment uses frozen oriented transforms, and visits store room-local entry anchors. Floor
numbers may change on a rescan without invalidating those anchors.

Save schema **3** adds `visitedRooms` records with `room` and `local` fields. Version 1/2 migrations
preserve all existing fields and initialize visits empty; old fog is not proof of entering an entire room.
Corrupt and unsupported future saves retain the repository's read-only/atomic-write protections.
Older plugin versions cannot read schema 3: keep the existing `.bak` when testing or rolling back.

## Incremental rendering

Coverage is rasterized once per geometry/projection. A reverse sample-to-tile index schedules only
changed 32-pixel tiles, with a halo for identical inward fog feathering across tile boundaries. Each
view consumes additions independently. CPU compositing uploads changed rectangles with SetPixels32;
Unity's Apply may still upload an entire backing texture depending on the graphics backend. Terrain
is never repainted merely because a marker moved. Overview catalog extents consume discovery additions
without repeatedly classifying the complete graph.

The optional private UI shader composes static terrain with fog/visit masks. It supports UI stencil,
RectMask2D clipping, tint and alpha. It never changes game materials, cameras, render pipelines or
world state. CPU compositing is the default when the bundle is absent or unsupported, with one warning.
Resources are disposed when their owning views/session are replaced or the plugin is unloaded.

## Rebuilding the optional shader

Use **Unity 6000.0.75f1**, matching the recorded Valheim 1.0.21 Unity baseline. From the repository root:

```powershell
./build/Build-MapShader.ps1 -UnityEditor 'C:/Program Files/Unity/Hub/Editor/6000.0.75f1/Editor/Unity.exe'
```

The isolated project in `assets/map-shader` builds `dungeonguide-ui` with Direct3D 11 and Vulkan variants.
The script copies it into the plugin content directory; subsequent .NET builds copy it beside the DLLs.
Copy the bundle beside DungeonGuide.dll when packaging. Generated Unity caches and bundles are ignored.
An older Unity editor is not a supported substitute. Building the .NET solution never starts Unity.

## Gameplay acceptance checklist

Record the numbered build, commit, graphics API, dungeon, other mods, save state and scan settings.
Compare the same dungeon and saved exploration before/after; do not change grid spacing or budget.
The previous MorkHalla log showed 19,278 nodes, 14 floors and 67.3 s to full navigation, with sampling
already finished by the 10 s progress entry. It does not contain room sizes or a first-image timestamp.
New diagnostics report room bounds, preview completion, full navigation readiness and first floor image.
Room bounds are transformed world AABBs, not exact internal walkable dimensions.

- Confirm the first image appears before navigation readiness; request a route early and watch it finish.
- On existing geometry at 60 FPS, target <=100 ms for nearest new visible fog; measure rather than infer.
- Walk past walls, open/close doors while standing, traverse slopes, stairs and stacked floors.
- Compare detail/minimap with All floors in explored/full modes; visit a room from only its lower layer.
- Preserve manual pan/zoom when full geometry replaces the preview; try exit, teleport and rescan.
- Reload migrated and schema-3 saves; verify room colors, chest names and notes persist.
- Compare CPU/shader on Direct3D 11/Vulkan, overview scrolling, overlay, Alt+M and multiplayer pins/pings.

Compilation and domain tests do not establish gameplay timings or graphics compatibility. The current
workspace has only Unity 2020.3.20f1, so the 6000.0.75 bundle and GPU path cannot be built/tested here.

## Native map pin maintenance fix

The previous Harmony pointer list incorrectly included `CreateMapNamePin`. Valheim 1.0.21 calls this
from `UpdatePins` before accessing the name's RectTransform. Suppressing creation can leave the name
data present without its UI and repeatedly throw at `RectTransform.anchoredPosition`. The pointer
policy now excludes marker lifecycle methods. World pins and other map plugins keep their native
maintenance calls even while the dungeon layer is active. This fixes the guard; it does not suppress
Unity exceptions or attempt to repair another plugin's pin data. Restart the game after replacing the
DLL to remove old Harmony patches and recreate map UI. The reported log used build 0.1.0.336.
