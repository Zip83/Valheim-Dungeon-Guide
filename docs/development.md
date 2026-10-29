# Development

## Compatibility and verification

The current development target was checked against a local Windows installation
on **2026-10-29**:

| Component | Recorded version |
| --- | --- |
| Valheim | **1.0.21** |
| Steam build ID | `26249040` |
| BepInExPack Valheim | `5.4.2351` |
| BepInEx loader | `5.4.23.5` |
| Unity runtime | `6000.0.75.2503836` |

The game, loader, pack, and Unity versions were read from the installed game's
`BepInEx/LogOutput.log`; the Steam build ID came from `appmanifest_892970.acf`.
This records the intended runtime environment. The bootstrap has passed CI
compilation and CodeQL analysis, but has not yet been load-tested in this game.
Other game versions are unverified.

The compile-time package references are `BepInEx.BaseLib 5.4.21` and
`UnityEngine.Modules 2022.3.50`, not copies of the installed game assemblies.
They provide the small shared API surface used by the bootstrap; they do not
mean it was built against the exact Unity 6 assemblies above. Revisit the
reference baseline when introducing game-specific integration APIs.

For each release, record the target Valheim version and the runtime environment
actually checked, including the date and verification method. Update the README
target summary together with this table. Report compilation, load checks, and
gameplay checks separately rather than inferring support from a successful build.

## Build

Install the .NET SDK selected in [`global.json`](../global.json), then run from
the repository root:

```sh
dotnet restore src/DungeonGuide/DungeonGuide.csproj --locked-mode
dotnet format src/DungeonGuide/DungeonGuide.csproj --no-restore --verify-no-changes --severity warn
dotnet build src/DungeonGuide/DungeonGuide.csproj --configuration Release --no-restore
```

The output is `src/DungeonGuide/bin/Release/netstandard2.1/DungeonGuide.dll`.
The plugin targets .NET Standard 2.1 and C# 10; the build SDK does not change
the game's runtime requirements. No local Valheim installation is needed to build.

Dependencies are compile-only references from the BepInEx package feed, with
source mapping and a committed NuGet lock file. `BepInEx.BaseLib` supplies the
BepInEx 5 plugin API; Harmony is not needed by the bootstrap. Unity reference
assemblies provide the stable component APIs used here. Neither package is copied
into the plugin artifact. These references do not establish compatibility with
every Valheim version or authorize redistribution of game assemblies.

When deliberately updating a dependency, change its version, run
`dotnet restore src/DungeonGuide/DungeonGuide.csproj --force-evaluate`, and review
the resulting `packages.lock.json` alongside the code change.

## Structure

`src/DungeonGuide/GuidePlugin.cs` is the BepInEx entry point. It currently only
logs startup. `PluginInfo.cs` owns the stable identity and version used by the
plugin attribute. Keep its version aligned with the project version.

Add focused domain, application, integration, presentation, and storage units
when their behavior is introduced. Keep domain rules independent of Unity and
BepInEx; do not create empty layers or placeholder service implementations.
Add a separate test project with the first testable behavior.

## Continuous integration

Pull requests targeting `master` and pushes to `master` run:

- Locked dependency restore, formatting checks, and Release compilation on Linux
  and Windows. Compiler warnings fail the build.
- An artifact containing only the plugin DLL, README, and license. It is a
  bootstrap build, not a released dungeon mapper.
- CodeQL analysis of the C# build, with a weekly scheduled scan as well.

Workflows use commit-pinned actions. CI does not install into a game, publish a
release, or test gameplay. Build and formatting checks are not gameplay tests.

After the checks have run, repository administrators can make `Build
(ubuntu-latest)` and `Build (windows-latest)` required checks in the ruleset.
CodeQL results are separate from GitHub's Code Quality requirement; if the latter
is enabled, it also needs to be configured in repository settings. Use either
the supplied advanced CodeQL workflow or GitHub's default CodeQL setup, not both.

## Manual load check

With Valheim closed, copy the built DLL into
`BepInEx/plugins/DungeonGuide/` in a BepInEx 5 installation. Start the game and
look for `Valheim Dungeon Guide 0.1.0 loaded.` in `BepInEx/LogOutput.log`.
The bootstrap adds no map, controls, patches, or saved data. Test it in a separate
profile if a working Dungeon Guide installation already exists; both use the
same plugin GUID and must not be loaded together.
