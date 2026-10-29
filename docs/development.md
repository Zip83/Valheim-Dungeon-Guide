# Development

## Compatibility and verification

The development target was checked against a local Windows installation on
**2026-10-29**:

| Component | Recorded version |
| --- | --- |
| Valheim | **1.0.21** |
| Steam build ID | `26249040` |
| BepInExPack Valheim | `5.4.2351` |
| BepInEx loader | `5.4.23.5` |
| Unity runtime | `6000.0.75.2503836` |
| Installed Harmony assembly | `2.9.0.0` |

Versions were read from `BepInEx/LogOutput.log`, the installed assemblies, and
`appmanifest_892970.acf`. Compilation and automated domain checks are separate
from load and gameplay checks. This repository build has not yet been load-tested;
Mistlands and Deep North gameplay checks are pending. Other game versions are unverified.

Compile-time dependencies are BepInEx.BaseLib **5.4.21**, UnityEngine.Modules
**6000.0.75**, and HarmonyX **2.9.0**. CI obtains game reference assemblies from
Digitalroot.Valheim.Common.References **1.0.22**, whose package release notes
identify Valheim **1.0.21**, and UI references from Digitalroot.References.Unity
**6000.0.75**. These are compile-time references, not runtime dependencies to ship.
Publicized reference assemblies do not establish private API stability. The
optional local game reference mode below checks against the installed game files.
Both the default reference build and the local Valheim 1.0.21 reference build
passed on Windows with zero compiler warnings and errors on 2026-10-29.

Update the README and release notes with the target version for each release.
Record the environment, date, and actual verification method here; never infer
load or gameplay compatibility from compilation.

## Build

Install the SDK selected by [`global.json`](../global.json), then run from the
repository root:

```sh
dotnet restore Valheim.DungeonGuide.slnx --locked-mode
dotnet format Valheim.DungeonGuide.slnx --no-restore --verify-no-changes --severity warn
dotnet build Valheim.DungeonGuide.slnx --configuration Release --no-restore
dotnet test Valheim.DungeonGuide.slnx --configuration Release --no-build --no-restore
```

The plugin targets **.NET Framework 4.8**, matching the game reference assemblies.
The independent domain library targets **.NET Standard 2.0**, usable by the game
and the **.NET 10** test runner. All projects use C# 10 with nullable reference
types disabled. SDK selection does not change the runtime APIs available in Unity.

No game installation is needed for the default build. NuGet restores external
compile-time references and .NET Framework reference assemblies on both Windows
and Linux. An explicit Microsoft.NETFramework.ReferenceAssemblies 1.0.3 reference
keeps the dependency graph identical when a Windows runner already has a targeting
pack installed. Dependencies use source mapping and committed lock files. `PackageDownload`
acquires exact game/UI references without adding them as runtime assets. To check
against a local installation instead, run:

```powershell
dotnet build Valheim.DungeonGuide.slnx -c Release --no-restore '-p:GameDir=D:/Games/Steam/steamapps/common/Valheim'
```

`VALHEIM_DIR` can supply the same path. Game reference hints use that installation's
`valheim_Data/Managed`; loader, Harmony, and Unity module references retain their
pinned compile-time baseline. No local paths or proprietary DLLs belong in Git.
When changing dependencies, restore with `--force-evaluate` and review each lock file.

Deploy **both** `DungeonGuide.dll` and `DungeonGuide.Domain.dll` from
`src/DungeonGuide/bin/Release/net48/`. Do not deploy third-party reference DLLs,
test outputs, or framework assemblies. CI prepares the same two-DLL layout.

## Structure

`Valheim.DungeonGuide.slnx` includes all production and test projects. Register new
projects with `dotnet sln Valheim.DungeonGuide.slnx add path/to/project.csproj`.

| Location | Responsibility |
| --- | --- |
| `src/DungeonGuide/DungeonGuide.cs` | Composition and Unity lifecycle callbacks |
| `src/DungeonGuide.Domain/` | Geometry graph, route/floor policies, exploration, chest rules, save records |
| `src/DungeonGuide.Domain/Content/` | Shared player-facing copy; Czech for this version |
| `src/DungeonGuide/Application/` | Session and navigation coordination, commands, scheduling |
| `src/DungeonGuide/Integration/` | Unity/game inspection, bounded scans, input, Harmony hooks |
| `src/DungeonGuide/Presentation/` | Native-map integration, texture/UI rendering, instructions, map palette |
| `src/DungeonGuide/Storage/` | JSON persistence and repository boundary |
| `tests/DungeonGuide.Tests/` | xUnit regressions against the production domain assembly |

The domain project has no Unity, BepInEx, Harmony, or filesystem references.
`GuideText` is internal shared content exposed only to the plugin through a friend
assembly; tests retain independent expected labels. The pure floor-label formatter
uses this catalog. `MapPalette` owns visual colors. Configuration keys and engine
API identifiers remain at their respective integration boundaries.

`PluginInfo` owns the stable GUID, display name, and version. Keep its version
aligned with both production projects. `Awake` is a Unity message, not an override
of `BaseUnityPlugin`; lifecycle callbacks have their own XML documentation.

## Style and compatibility contracts

`Directory.Build.props` enables XML documentation, build-time style analysis, and
warnings as errors for every project. `.editorconfig` holds required rules.
`Directory.Build.targets` runs the read-only style formatter before compilation
to catch IDE-only rules; design-time builds skip it to prevent recursion. Builds
never rewrite code. Logical grouping and XML documentation quality still need review.

The compiler enforces CS1591 on externally visible API. Public members of internal
types are documented by convention as well. See the [coding conventions](coding-conventions.md).

Two compatibility exceptions are intentional:

- Legacy serialized fields retain names such as `world`, `dungeon`, `visited`,
  `chests`, and `position`. Renaming them would break existing JSON. Tests freeze
  this contract. Save schema 2 migrates version 1, preserves exploration and notes,
  and rejects unknown future versions. This import does not change the schema.
- `WorldChangeHooks` preserves Harmony's special parameter names `__result`,
  `__instance`, and `__0`. Narrow, per-method IDE1006 suppressions document that
  injection contract; ordinary parameter naming remains enforced everywhere else.

## Automated verification

The xUnit suite covers walk connectivity, directional optional traversals,
route costs, floor landing and elevation selection, floor labels, viewport fitting,
map state, chest identity/selection, exploration reload, and save migration.
Tests reference the production domain library, not copied implementations.

During the initial import, all 58 checks in the earlier standalone regression
runner also passed against this domain assembly. The repository suite has 59
cases, including an independent serialized-field contract check. Identifiers in
that check are compared using ordinal ordering, independent of the machine culture.

A failed test requires investigation of the application contract first; do not
rewrite assertions simply to get a passing result. Game geometry and UI require
[manual checks](in-game-checklist.md) in addition to these tests.

## Continuous integration

Pull requests to `master` and pushes to `master` classify their changes through
`.github/filters.yml`. Source, tests, dependencies, build/style configuration,
workflows, and unclassified files require Linux and Windows locked restore,
formatting, Release build, and domain tests. Test results are uploaded for each
OS; the Linux job packages only the two project DLLs, player instructions, and license.
CodeQL analyzes the C# solution, including a weekly scheduled scan.

Documentation/metadata-only changes skip heavy jobs. Pull requests are classified
using their entire diff; a later documentation commit cannot skip checks for code
already in that PR. Manual workflows and scheduled CodeQL scans always verify.
Stable **CI result** and **CodeQL result** checks fail if detection or a required
job fails. Skips are explicit; no empty SARIF or fabricated scan is uploaded.

Actions are pinned to commits. Require the stable result checks in the ruleset,
not conditional matrix jobs. GitHub's separate code-scanning rule governs real
CodeQL findings and differs from required checks and Code Quality. Use advanced
CodeQL or default setup, not both. CI does not install into a game, merge a PR,
publish a release, or test gameplay.
