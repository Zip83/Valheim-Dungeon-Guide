# Contributing

Keep changes focused, readable, and practical for a client-side Valheim mod.
Human and AI-assisted contributions follow the same review standards.

## Before changing behavior

Read [AGENTS.md](AGENTS.md) and the [coding conventions](docs/coding-conventions.md).
Check existing issues and the affected code before proposing a new abstraction
or dependency. For larger changes, describe the problem, expected behavior,
compatibility implications, and verification approach in an issue first.

## Code and architecture

Use meaningful XML documentation for public types and members, including
constructors, methods, properties, fields, and events, and for lifecycle entry
points. Describe the purpose and contract; document parameters, return values,
and expected exceptions where applicable. Use `<inheritdoc />` when a real
inherited contract applies. Avoid comments that merely repeat a member's name.
Missing documentation on externally visible API fails the build through CS1591.
Documentation quality and completeness of contract details also require review.

Nullable reference types are not used. The shared `Directory.Build.props`
disables them for every project. Do not enable them in project files, add
`#nullable enable`, or introduce nullable reference annotations. This policy
does not prohibit nullable value types such as `int?` when the domain needs them.

Put machine-enforceable formatting and code-style preferences in `.editorconfig`.
Required rules have error severity and are enforced by ordinary `dotnet build`
through the shared analyzer settings. Do not override these settings or rely
only on editor preferences. `Directory.Build.targets` additionally verifies
IDE-only style rules with the formatter before compilation. A build checks
source; it must not rewrite it.
When adjusting enforcement, confirm that representative violations fail the
build, restore the deliberate violations, and confirm a clean build succeeds.

Use strict SRP at class and method level. Keep domain decisions, game API access,
presentation, and persistence separate. Keep the plugin entry point small.
Use one top-level type per file and meaningful names that match actual behavior.

Before committing or requesting review, manually inspect every changed method,
including private helpers, and affected class against the
[single responsibility review](docs/coding-conventions.md#single-responsibility-review).
State one responsibility and reason to change for each; separate independently
changing policies and keep orchestrators focused on delegation. Resolve violations
before handoff. Build, test, and formatter success do not replace this review.

Prefer `var` for local variables when inference preserves the intended static
type. Keep explicit types when required by inference or intended conversions.
Follow the configured naming conventions, mark initialization-only private
fields `readonly`, and omit unnecessary `this.` qualifiers.

Readability is part of correctness. Leave a blank line between methods and
between meaningful logical steps within a method. Use Allman braces and avoid
compressed control flow. Run formatting tools and then inspect the result;
automatic formatting alone does not establish readable grouping.

Use the language and runtime versions declared by the projects. Reference game
and BepInEx assemblies from a local installation; do not upload those assemblies.
Avoid unrelated dependency upgrades or broad formatting changes in behavior PRs.

## Player-facing translations

Add text as a typed `GuideText` key with matching entries in the neutral English
`Content/Strings.resx` and Czech `Content/Strings.cs.resx` catalogs in the domain
project. Preserve meaningful whitespace with `xml:space="preserve"`. Keep
translated copy out of C# language conditionals; use .NET `ResourceManager`.

Select Czech only from Valheim's `Czech` setting, with English as the fallback
for every other setting. Operating-system culture must not select the UI language.
Preserve saved player names and notes when the language changes.

Run the localization tests after changing catalog keys, fallback, or language
selection. Check both exact key sets so English fallback cannot hide a missing
Czech translation. Preserve the generated `cs/` satellite subdirectory when
packaging or copying the plugin outputs.

## Verification

- Test behavior where coverage provides value: domain decisions, invariants,
  edge cases, and regressions. Avoid tests that mirror implementation details
  or only assert trivial scaffolding.
- A failing test is evidence to investigate, not an instruction to rewrite the
  test. Reproduce the failure, check the intended contract, and investigate
  application code first. If the application is wrong, fix it and keep the
  regression assertion. Change a test only when its expectation is demonstrably
  incorrect or requirements intentionally changed, and explain why in the PR.
  Never weaken, skip, or remove a test solely to make the suite pass.
- Run the build, relevant tests, and formatting checks provided by the affected
  project. Record the exact commands and results in the pull request.
- Add regression tests for routing, floor detection, exploration, and persistence
  changes where they can be verified independently of the game.
- For UI, game hooks, or traversal changes, also describe the in-game scenario
  checked, including the Valheim and BepInEx versions and dungeon type.
- Clearly label in-game checks that were not performed. A successful build does
  not establish correct rendering, multiplayer behavior, or route safety.
- For documentation and repository configuration, check links, formatting,
  ignore patterns, and the diff; a game build is unnecessary.

## Versions and test builds

`Version.props` is the single source of the release version prefix. Change it
when preparing a release milestone: use a patch increment for fixes and a minor
increment for added features. Keep the documented release version consistent.
Do not bump the release prefix merely for another CI run or review edit.
Keep the literal release prefix in CI's `run-name` synchronized with `Version.props`.

CI identifies every development build as `<prefix>.<run>-dev.<attempt>` and
records the exact checkout commit. BepInEx and assembly file metadata receive
`<prefix>.<run>` because the loader needs a numeric version. Startup diagnostics
include the full development version, attempt, and commit. Each artifact includes
`build-info.json` with the source, PR, workflow run, and target Valheim version.
Local builds use `<prefix>.0-dev.local` unless build identity properties are supplied.
A CI artifact is a test build, not a published stable release.

After a meaningful change, verify the build and tests, provide the numbered
artifact for gameplay checks, and record the tested version in bug reports.
Preserve the satellite translations when installing it. Do not publish releases
or replace an installed plugin automatically as part of a build.

## Pull requests

CI reads the XML `.trx` results directly and publishes test counts and failure
details in the workflow summary and `Tests (ubuntu-latest)` / `Tests (windows-latest)`
checks. Reports also run after test failures. Fork PRs receive the workflow summary
without requiring write access to checks. Test result files are not uploaded as
artifacts; the plugin package remains available as a downloadable artifact.

Use a short, descriptive title. Explain the concrete problem and resulting
behavior, relevant validation, and remaining limitations. Include screenshots
for visible map or overlay changes. Keep formatting-only refactors separate
from behavioral changes when practical. Do not add generated files, personal
configuration, game saves, or binaries.

AI assistance is welcome. Review generated changes yourself, verify referenced
APIs, and explain the result. Remove fabricated claims, unnecessary scaffolding,
and unrelated edits before submitting.

## Stacked pull requests

Split large changes into focused, buildable pull requests. Target each child PR
at its immediate predecessor so Files changed shows only that step. Keep
feature logic and its regression tests together where dependencies allow it.
CI and CodeQL run for pull requests targeting any branch.

This repository uses squash merges. Review and merge from the bottom of the
stack upward. After merging a parent, retarget its immediate child to `master`
and merge the updated `origin/master` into that child. Then merge each updated
parent into its next remaining child, in dependency order, and push normally.
This updates the whole remaining stack without rewriting shared history;
merge commits stay on feature branches while `master` continues to use squash
merges. Check that each PR still shows only its own changes and rerun checks.

Retargeting alone after a squash merge can bring the parent's diff back into the
child PR. If rebasing instead of merging the updated bases, restack every
remaining descendant and obtain explicit authorization before rewriting shared
branches. Do not merge a child PR into an unmerged feature branch or delete a
parent branch before its child is retargeted.

## Bug reports

Include reproduction steps, expected and actual behavior, game and mod versions,
dungeon type, and other map mods. Attach relevant excerpts from
`BepInEx/LogOutput.log` and screenshots when useful. Remove personal information,
server addresses, and other private data before sharing logs.

## License

Contributions are provided under the project's [MIT License](LICENSE). Only
submit material you have the right to contribute; preserve required third-party
attribution where applicable.
