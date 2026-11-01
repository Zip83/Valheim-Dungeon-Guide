# Thunderstore packaging

CI produces one ZIP artifact for manual installation and manual Thunderstore
upload. It does not publish a release, upload to Thunderstore, or install into a game.
The existing single Ubuntu build creates the package; no additional OS build is needed.

## Package contents

The root contains `README.md`, `icon.png` (256 x 256 PNG), `manifest.json`, `LICENSE`,
and `build-info.json`. Plugin files remain under `BepInEx/plugins/DungeonGuide/`,
including the `cs/` satellite directory. English strings are embedded in
`DungeonGuide.Domain.dll`; an `en/` directory is not required.

`packaging/manifest.json` owns package identity, description, website and dependency
metadata. CI inserts `version_number` from `Version.props`; the source template is
not an uploadable manifest until that field is supplied. The dependency is the
recorded BepInExPack Valheim 5.4.2351 baseline, not a claim that a newer loader was tested.
`packaging/icon.svg` is the editable source of the original package icon.

CI checks required files, icon dimensions, metadata constraints and UTF-8 before
uploading the artifact. It includes only the two plugin assemblies and the Czech
satellite, not game reference assemblies. Target: Valheim 1.0.21; compilation
does not establish in-game or mod-manager compatibility.

## Versions

Thunderstore requires `major.minor.patch`. Its package version is the release
prefix in `Version.props`, while DLLs and `build-info.json` retain the exact numbered
development identity. For example, a CI build of the 0.1.1 milestone can use
`0.1.1.673-dev.1` as its build identity but `0.1.1` in the Thunderstore manifest.
The README identifies CI artifacts as development builds.

Choose a reviewed, tested build for publication. Publish each release prefix only
once; subsequent public updates require a version increment under the existing
versioning policy. Re-running CI does not produce a new Thunderstore release
version. Do not upload every development artifact as the same public version.

## Manual upload

1. Download the ZIP artifact from a successful CI run. Required files must appear
   directly in the archive root, without an extra containing folder.
2. Review the included README and exact `build-info.json` commit. Record the game
   verification actually performed; do not assume every CI artifact was tested in-game.
3. Sign in at [Upload package](https://thunderstore.io/package/create/), create or
   select your team, choose the Valheim community and appropriate categories.
4. Upload the ZIP and submit when ready. This publishes the selected build.

For screenshots in the README, use publicly hosted image URLs. Local desktop paths
and images merely included in the ZIP are not public image links. Screenshots can
be added in a separate presentation change after they are hosted.

Official references: [package requirements](https://wiki.thunderstore.io/mods/creating-a-package)
and [BepInEx directory rules](https://wiki.thunderstore.io/mods/packaging-your-mods).
