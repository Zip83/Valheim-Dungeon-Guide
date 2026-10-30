# Repository rules for AI coding assistants

These rules apply throughout the repository. Read this file, `CONTRIBUTING.md`,
and `docs/coding-conventions.md` before making changes. Read any more specific
`AGENTS.md` in the area being changed. Explicit user instructions take precedence.

## Scope and working process

- Inspect the current files, Git status, and relevant contracts before editing.
- Complete the requested task with the smallest coherent change. Preserve user
  edits and avoid unrelated refactoring, formatting churn, or dependency upgrades.
- Do not import implementation code, add frameworks, or expand product scope
  unless the task calls for it.
- Prefer established local patterns. Introduce abstractions only when they
  separate real responsibilities or remove demonstrated complexity.
- Keep repository documentation, identifiers, and code comments in English.
- Use UTF-8 without BOM. Never commit secrets, local game files, save data,
  third-party reference binaries, or generated build outputs.
- Review the final diff against the request before committing. Do not rewrite
  shared history or force-push without explicit authorization.

## Strict single responsibility principle

- Every class, method, file, and test must have one clear responsibility.
- Use one top-level type per source file, named after that type.
- Keep parsing, normalization, validation, domain decisions, persistence, and
  presentation in separate focused units when they have different reasons to change.
- Names are contracts: a method called `Validate` validates; it does not also
  load data, normalize it, save it, and display a notification.
- A small orchestrator may sequence collaborators. It must not absorb their
  scanning, routing, rendering, or persistence implementations.
- Split by responsibility, not by an arbitrary line count. Avoid generic
  `Utils`, `Helpers`, or manager classes that accumulate unrelated work.
- Before implementing a method, identify its single responsibility and the
  independent decisions or phases it delegates. Keep discovery, candidate
  filtering, geometry queries, selection, graph mutation, and presentation
  separate when their rules can change independently.
- Extract a named private method for a distinct step within the same responsibility;
  extract a focused class when that step owns an independent policy, state, or
  lifecycle. Moving an entire mixed body into one helper does not resolve SRP.
- Keep orchestration at one level of abstraction. A method such as `Locate`,
  `Snap`, `Candidate`, or `Connect` may coordinate steps, but must delegate their
  algorithms rather than embed every search, filter, query, and mutation inline.
- Use structured control flow; `goto` is prohibited. Preserve coroutine yields,
  shared frame budgets, cancellation, and query ordering when separating phases.
  Performance concerns do not waive SRP. Add `AggressiveInlining` only with
  measured evidence; it is not a substitute for a clear design.

### Mandatory review before committing code

- Inspect every added or changed method, including private helpers, and each
  affected class. Be able to state its responsibility and reason to change in
  one concrete sentence. If that explanation combines independent tasks, split
  them before committing or presenting the change for review.
- Read the complete changed methods and their callers, not only added lines.
  Check delegation boundaries, descriptive names, control flow, XML contracts,
  and blank lines between members and logical steps.
- Resolve responsibility and readability violations found in this review before
  handing off the change. Build, tests, and formatter success do not establish
  SRP. Complete the code-review checkbox in the PR template only after this
  manual review; describe any check that could not be completed.

## C# readability

- Leave nullable reference types disabled; do not add `Nullable=enable`,
  `#nullable enable`, or nullable reference annotations.
- Keep machine-enforceable style rules in `.editorconfig`, not only in prose.
  Use error severity for required rules. `Directory.Build.props` applies the
  nullable and analyzer policy to every project; do not override it in a project.
- Verify formatting enforcement with ordinary `dotnet build`, including a
  deliberate invalid-code probe when changing the enforcement configuration.
  Restore probe changes before committing; do not auto-format source during builds.
- Follow `.editorconfig` and `docs/coding-conventions.md`.
- Document public types and members with meaningful XML documentation, including
  constructors, methods, properties, fields, and events. Document lifecycle
  entry points as well. Explain purpose, contracts, inputs, results, and relevant
  exceptions rather than repeating names or filling templates with empty prose.
- Use `<inheritdoc />` for genuine overrides and interface implementations when
  their inherited contract is sufficient. Unity message callbacks need their
  own documentation. Do not suppress missing-public-documentation errors or
  disable XML generation to make a build pass.
- Prefer `var` for local variables whenever inference preserves the intended
  static type. Use an explicit type when inference is unavailable or would
  change the intended type, conversion, or overload behavior.
- Use the configured naming conventions, `readonly` for private fields that
  are only assigned during initialization, and omit unnecessary `this.`.
- Leave exactly one blank line between constructors, methods, and distinct
  property declarations. Never stack method declarations without separation.
- Inside methods, separate meaningful logical steps with one blank line, such
  as a guard clause, preparation, processing, and returning the result.
- Keep tightly related statements together. Do not insert a blank line after
  every statement or use multiple consecutive empty lines.
- Use structured control flow and early returns or `yield break`; do not use `goto`.
- Use four spaces, Allman braces, and braces for all control-flow bodies.
- Expand nontrivial expressions and long conditions instead of compressing code.
- Review whitespace manually: the standard formatter does not enforce every
  member-separation or logical-grouping rule.

## Game integration boundaries

- Keep domain rules independent of Unity, BepInEx, Harmony, and filesystem APIs.
- Keep game scanning and game API access at the integration boundary; keep
  rendering separate from navigation rules and persistence.
- Keep plugin startup focused on composition and lifecycle management.
- Use narrow Harmony patches. Clean up patches, event handlers, UI objects, and
  owned resources during teardown.
- Access Unity objects on the game thread. Bound scanning work per frame and
  avoid expensive scene searches or repeated allocations in hot paths.
- Keep scan work cooperative across every expensive phase. Never synchronously
  wait for workers on the game thread. Background analysis may access only an
  unpublished, exclusively owned graph; cancel it on session close and publish
  results on the game thread. A time budget cannot preempt a Unity engine call.
- Inspect actual referenced game APIs before relying on private members.
  Handle unsupported versions explicitly; do not silently report success.
- Keep user-facing text at a presentation/localization boundary, and visual
  constants in a focused style definition rather than scattering them through logic.
- Store translations in `.resx` catalogs resolved by .NET `ResourceManager`.
  Keep translated copy out of C# language conditionals. Verify matching resource
  keys and package the generated language satellite assemblies.
- Default player-facing text to English. Select Czech only from Valheim's Czech
  language setting; other languages fall back to English. Do not use OS culture
  for this decision or translate saved user names and notes in place.
- Preserve native world-map interaction and multiplayer behavior. Do not add
  network ownership changes or world mutations as a shortcut for inspection.

## Persistence and compatibility

- Treat saved field names and identifiers as compatibility contracts.
- Describe format changes and migration behavior before implementing them.
  Preserve old saves and test migrations with representative data.
- Write each logical save atomically. Do not overwrite corrupt or unsupported
  data with an empty state; surface a useful diagnostic instead.
- Keep proprietary game references external and locally configurable.
- Use only language/runtime features supported by the actual Unity/BepInEx
  target. A newer .NET SDK does not make newer runtime APIs available in-game.

## Verification and handoff

- Use `Valheim.DungeonGuide.slnx` for repository-wide restore, formatting, and
  builds. Add every new production or test project to this solution so CI
  includes it; do not maintain a duplicate legacy `.sln`.
- Keep the release prefix in `Version.props`; do not duplicate version literals
  in plugin source or project files. Use numbered development builds for review
  and gameplay feedback, recording the exact source commit. Do not bump a release
  prefix for every CI run, publish a stable release, or install into a running game
  automatically. Follow the versioning policy in `CONTRIBUTING.md`.
- Always state the target Valheim version in the README and release notes.
  Keep the environment, verification date, and evidence in
  `docs/development.md#compatibility-and-verification` current when changing the
  target. Distinguish compile-time reference versions from the installed game,
  and compilation from actual load/gameplay verification. Do not claim support
  for untested versions.
- Run the relevant build, domain tests, and formatting checks for implementation
  changes, using the commands actually present in the repository.
- Do not invent projects, commands, passing checks, or compatibility claims.
- Add focused regression coverage for meaningful logic changes. Each test
  verifies one behavior; use data-driven variants where practical. Keep tests
  outside production files and avoid Arrange/Act/Assert scaffolding comments.
- Add tests when they protect meaningful behavior, invariants, boundary cases,
  or a regression. Do not add tests that only repeat the implementation or
  assert trivial scaffolding to increase a test count.
- When a test fails, reproduce it and compare the expected result with the
  intended contract before editing anything. Investigate the application code
  first: the test may have exposed a real bug. Fix production code when its
  behavior violates the contract, preserving the regression assertion.
- Change a test expectation only when evidence shows the test is incorrect or
  an intentional requirement change makes it obsolete; explain that reason in
  the PR. Never weaken, skip, delete, or rewrite a failing test merely to get a
  passing run. Rerun the failing case and the relevant suite after the fix.
- Do not suppress diagnostics or disable checks merely to obtain a passing run.
  Document a narrowly justified exception when compatibility requires one.
- Documentation-only changes need link, consistency, and diff checks rather
  than an unrelated game build.
- Distinguish automated tests, static API inspection, and actual in-game tests.
  Never present one as proof of the others.
- Report what changed, what was verified, and any material remaining limitation.
  If a check cannot run, state the command, blocker, and practical risk.
