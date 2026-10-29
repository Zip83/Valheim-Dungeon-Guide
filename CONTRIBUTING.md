# Contributing

Keep changes focused, readable, and practical for a client-side Valheim mod.
Human and AI-assisted contributions follow the same review standards.

## Before changing behavior

Read [AGENTS.md](AGENTS.md) and the [coding conventions](docs/coding-conventions.md).
Check existing issues and the affected code before proposing a new abstraction
or dependency. For larger changes, describe the problem, expected behavior,
compatibility implications, and verification approach in an issue first.

## Code and architecture

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

## Pull requests

Use a short, descriptive title. Explain the concrete problem and resulting
behavior, relevant validation, and remaining limitations. Include screenshots
for visible map or overlay changes. Keep formatting-only refactors separate
from behavioral changes when practical. Do not add generated files, personal
configuration, game saves, or binaries.

AI assistance is welcome. Review generated changes yourself, verify referenced
APIs, and explain the result. Remove fabricated claims, unnecessary scaffolding,
and unrelated edits before submitting.

## Bug reports

Include reproduction steps, expected and actual behavior, game and mod versions,
dungeon type, and other map mods. Attach relevant excerpts from
`BepInEx/LogOutput.log` and screenshots when useful. Remove personal information,
server addresses, and other private data before sharing logs.

## License

Contributions are provided under the project's [MIT License](LICENSE). Only
submit material you have the right to contribute; preserve required third-party
attribution where applicable.
