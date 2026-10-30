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
dotnet restore Valheim.DungeonGuide.slnx --locked-mode
dotnet format Valheim.DungeonGuide.slnx --no-restore --verify-no-changes --severity warn
dotnet build Valheim.DungeonGuide.slnx --configuration Release --no-restore
```

The output is `src/DungeonGuide/bin/Release/netstandard2.1/DungeonGuide.dll`.
The plugin targets .NET Standard 2.1 and C# 10; the build SDK does not change
the game's runtime requirements. No local Valheim installation is needed to build.

`Directory.Build.props` keeps nullable reference types disabled and enables
build-time style analysis for all projects. Required style rules live in
`.editorconfig` with error severity: invalid indentation, brace placement,
missing control-flow braces, and the configured code-style violations fail
ordinary `dotnet build`. Fix source with your editor or `dotnet format` and rerun
the build. Builds never auto-format files. Logical blank-line grouping still
requires review as described in the coding conventions.

`Directory.Build.targets` also runs the read-only style formatter before
compilation. This covers IDE-only rules such as unnecessary `this.` that are
not reported by the compiler's build-time analyzers. Design-time builds skip
the check so the formatter can load projects without recursively invoking itself.

Dependencies are compile-only references from the BepInEx package feed, with
source mapping and a committed NuGet lock file. `BepInEx.BaseLib` supplies the
BepInEx 5 plugin API; Harmony is not needed by the bootstrap. Unity reference
assemblies provide the stable component APIs used here. Neither package is copied
into the plugin artifact. These references do not establish compatibility with
every Valheim version or authorize redistribution of game assemblies.

When deliberately updating a dependency, change its version, run
`dotnet restore Valheim.DungeonGuide.slnx --force-evaluate`, and review
the resulting `packages.lock.json` alongside the code change.

## Structure

[`Valheim.DungeonGuide.slnx`](../Valheim.DungeonGuide.slnx) is the repository's
solution and the entry point for IDEs, local builds, formatting, and CI. The
repository's .NET 10 SDK supports this XML solution format. Keep one solution
and register each new production or test project with
`dotnet sln Valheim.DungeonGuide.slnx add path/to/project.csproj` so repository-wide
checks include it.

`src/DungeonGuide/DungeonGuide.cs` is the BepInEx entry point. It currently only
logs startup. `PluginInfo.cs` owns the stable identity and version used by the
plugin attribute. Keep its version aligned with the project version.

`Awake` is a Unity lifecycle message, not a virtual method overridden from
`BaseUnityPlugin`. Its XML documentation describes the callback explicitly.
The build emits `DungeonGuide.xml` alongside the DLL for documentation tools.

XML documentation generation is enabled for all projects in
`Directory.Build.props`. Missing documentation on externally visible types and
members fails the build with CS1591. Public members inside internal types are
also documented by convention; the compiler's CS1591 check only covers the
externally visible API. Review documentation quality alongside the code.

Add focused domain, application, integration, presentation, and storage units
when their behavior is introduced. Keep domain rules independent of Unity and
BepInEx; do not create empty layers or placeholder service implementations.
Add a separate test project with the first testable behavior.

## Continuous integration

Pull requests targeting `master` and pushes to `master` first classify changed
files using the shared `.github/filters.yml`. Changes outside Markdown,
`docs/`, the license, `.gitignore`, CODEOWNERS, and issue templates require:

- Locked dependency restore, formatting checks, and Release compilation on Linux
  and Windows. Compiler warnings fail the build.
- An artifact containing only the plugin DLL, README, and license. It is a
  bootstrap build, not a released dungeon mapper.
- CodeQL analysis of the C# build, with a weekly scheduled scan as well.

Documentation/metadata-only changes skip compilation, formatting, artifact
creation, and CodeQL analysis. Lightweight detection and result jobs still run,
so required status checks complete instead of staying pending. Any source,
test, solution, dependency, build configuration, analyzer setting, workflow,
filter, or otherwise unclassified file keeps verification enabled. Scheduled
CodeQL scans and manually dispatched workflows always perform verification.

Pull requests are classified using their entire diff against the base branch;
adding a documentation-only commit to a PR that already changes code still
requires verification. Pushes to `master` use the changes in that push.

`CI result` and `CodeQL result` fail when detection fails, a required job fails,
or the expected result is missing. A successful documentation-only result
explicitly records that the heavy job was skipped. No empty SARIF or fabricated
analysis result is uploaded.

Workflows use commit-pinned actions. CI does not install into a game, publish a
release, or test gameplay. Build and formatting checks are not gameplay tests.

After the checks have run, make **CI result** and **CodeQL result** the required
status checks in the ruleset. Do not require individual matrix job names or the
tool-generated **CodeQL** status for every PR: those may be absent when work is
skipped. The separate **Require code scanning results** rule governs real
CodeQL findings; it is distinct from required status checks.
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

## Translation catalogs

Player-facing strings use typed `GuideText` keys backed by .NET `ResourceManager`.
Keep English in `src/DungeonGuide.Domain/Content/Strings.resx` and Czech in
`Strings.cs.resx`, with matching keys and `xml:space="preserve"` for significant
whitespace. Do not put translated copy in C# language conditionals.

The injected Valheim language setting selects Czech only for `Czech`; every
other setting uses English regardless of operating-system culture. Resource
catalog tests cover key parity, fallback, and live language changes.

The build generates `cs/DungeonGuide.Domain.resources.dll`. Preserve this
subdirectory alongside the plugin and domain DLLs when copying build outputs;
CI includes it in the plugin artifact. No additional localization dependency
or distributed game asset is needed.
