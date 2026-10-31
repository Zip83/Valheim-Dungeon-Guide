# Optional GPU map compositing (Valheim 1.0.21)

The plugin owns a private material per map surface. Static terrain and linear-data fog/visit masks
are combined only in its UI layer. Native game materials, cameras and rendering pipelines remain
unchanged. The shader supports UI stencil, clipping, transparency and map crops. Unsupported or
missing bundles select the incremental CPU renderer with a single warning. Owned textures, materials
and the reference-counted bundle are released when their views are disposed.

Mask channels must be sampled as linear data. Palette values are passed as vectors to avoid Unity's
implicit Color-property conversion. An explicit runtime color-space uniform preserves CPU map colors
in gamma and linear projects. Fog feathering and room visibility remain computed from the same masks.

## Rebuild

Use Unity **6000.0.75f1**. The isolated project enables the built-in AssetBundle/UI modules and builds
Windows Direct3D 11 and Vulkan variants. A forced rebuild prevents stale module/type metadata.

```powershell
./build/Build-MapShader.ps1 -UnityEditor 'C:/Program Files/Unity/Hub/Editor/6000.0.75f1/Editor/Unity.exe'
```

The script copies `dungeonguide-ui` into plugin content, and the .NET build copies it beside the DLLs.
The verified runtime bundle `src/DungeonGuide/Content/dungeonguide-ui` is intentionally versioned
alongside `map-shader-bundle.json`, as explicitly requested for CI without Unity account setup.
This small first-party runtime asset is an exception to the general generated-output rule; editor
caches and other build output remain excluded. CI builds the C# plugin, validates the bundle header,
SHA-256 and source hashes, and requires that exact bundle in the downloadable plugin artifact.
`build-info.json` records its identity. GitHub runners do not install or activate Unity and need no
Unity secrets. Runtime CPU fallback remains available on unsupported hardware.

When changing shader/build inputs, rebuild locally and refresh the manifest after verification.
CI rejects stale sources, modified bundles and missing files instead of publishing a CPU-only package.
Source hashes normalize line endings and trailing whitespace, including Unity-rewritten metadata.
Compiling a changed shader requires a locally activated Unity Editor; normal C# changes do not.

## Graphics checks

`MapShaderSmokeCheck.Run` builds and reloads the bundle, then draws an isolated quad into a render target.
Separate checks verify explored terrain, muted full-map terrain, visited-room color, opaque fog and UI
clipping. `UseLinearColorSpace` selects linear rendering for the next editor invocation. Run checks with
`-batchmode -force-d3d11` and `-batchmode -force-vulkan`; omit `-nographics` for rendering checks.

On 2026-10-31 the bundle compiled and loaded in Unity 6000.0.75f1. All five graphics checks passed on
Direct3D 11 and Vulkan in gamma and linear modes. This is actual GPU rendering in an isolated Unity
Editor project; it does not establish Valheim gameplay timings, world-map multiplayer behavior or FPS.

## Map palette

Explored and full-map modes share the darker blue-gray floor palette. Full mode removes the fog;
it does not recolor revealed floor. Actually visited room coverage remains green. Unknown space
uses lighter gray cloud shading with the existing inward feathering, preserving completely opaque
fog beyond explored coverage. CPU and GPU palette values match.

The updated palette and compiled bundle passed all five isolated pixel checks on Direct3D 11 and
Vulkan in Linear color space on 2026-10-31. The earlier Gamma checks describe the previous bundle;
the updated palette still needs Gamma and Valheim gameplay verification. Package validation checks
the new bundle hash against the updated shader source, so CI cannot ship an older compiled shader.
