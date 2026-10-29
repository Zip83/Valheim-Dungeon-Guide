# C# coding conventions

Use [Microsoft's C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
as the baseline, adapted below for this Unity/BepInEx project. The project's
declared language and runtime compatibility take precedence over examples that
require newer C# versions or .NET APIs.

## Layout and whitespace

- Four spaces, no tabs; UTF-8 without BOM; LF line endings; final newline.
- Allman braces on separate lines. Always brace `if`, `else`, and loop bodies,
  including single-statement bodies. Use braces for `switch` statement sections
  when they contain statements; switch expressions retain normal C# syntax.
- One statement and one variable declaration per line.
- Exactly one blank line between constructors, methods, and distinct property
  declarations, including expression-bodied members.
- Group related fields together; separate different field groups and the first
  constructor or property with one blank line.
- Inside methods, use one blank line between coherent logical steps. Separate
  guard clauses from the main work, preparation from processing, and a final
  result from preceding multi-step work when this improves scanning.
- Keep directly related statements together. Avoid an empty line after every
  statement, multiple consecutive blank lines, or blank lines immediately inside braces.
- Aim for a 120-character line limit. Wrap long parameter lists, initializers,
  method calls, and conditions consistently. Keep URLs and other indivisible
  literals intact when necessary.
- Use `using` directives outside the namespace, with `System` directives first.
  Prefer file-scoped namespaces when the selected language version supports them.

### Example

```csharp
public sealed class CollectionMarker
{
    private readonly IChestRepository _chests;

    public CollectionMarker(IChestRepository chests)
    {
        _chests = chests;
    }

    public bool MarkCollected(ChestId chestId)
    {
        var chest = _chests.Find(chestId);
        if (chest == null)
        {
            return false;
        }

        chest.MarkCollected();
        _chests.Save(chest);

        return true;
    }

    public bool IsCollected(ChestId chestId)
    {
        var chest = _chests.Find(chestId);

        return chest != null && chest.IsCollected;
    }
}
```

The example illustrates whitespace and orchestration, not a required API or
repository abstraction.

## Naming and expressions

- Nullable reference types are disabled repository-wide. Do not enable them or
  use nullable reference annotations; nullable value types remain available.
- `PascalCase` for types, methods, properties, events, and constants.
- `camelCase` for parameters and local variables; `_camelCase` for private
  instance fields; `s_camelCase` for private static non-constant fields.
- Prefix interfaces with `I`; use an `Async` suffix for asynchronous methods.
- Use explicit accessibility modifiers. Prefer the narrowest useful visibility.
- Use C# keyword types such as `int` and `string`.
- Prefer `var` for local variables when inference preserves the intended static
  type, including built-in types and method results. Keep explicit types when
  inference is unavailable or would change the intended type or conversion,
  such as a specific interface/base type, numeric conversion, or `null` initializer.
- Mark private fields `readonly` when assigned only inline or in constructors.
- Omit unnecessary `this.` qualifiers; keep them when needed for disambiguation.
- Expression-bodied members are appropriate for short, clear expressions.
  Use blocks for branching, side effects, or longer logic; do not compress work
  merely to reduce line count.
- Avoid nested ternaries, obscure abbreviations, and redundant parameter details
  in method names.
- Preserve serialized names and external contracts even when they differ from
  current style. Document compatibility exceptions instead of silently renaming them.

## Responsibilities and errors

Keep one top-level type per file. Separate validation, mapping, routing rules,
game inspection, drawing, and saving when they represent different responsibilities.
An orchestrator delegates those steps; it does not implement all of them inline.
Introduce interfaces at meaningful boundaries rather than for every class.

Prefer guard clauses and explicit outcomes. Catch exceptions where recovery or
useful diagnostics are possible. Avoid empty catches, misleading success messages,
and repeated error logs every frame. Comments should explain constraints or
reasoning rather than narrate the next line.

## Enforcement

`.editorconfig` is the source of machine-enforceable formatting and style rules.
Required rules use error severity. `Directory.Build.props` disables nullable
reference types, enables build-time style analysis, and treats warnings as errors
for every project. Ordinary `dotnet build` must reject supported violations;
`Directory.Build.targets` runs the style formatter before compilation to catch
IDE-only rules as well. CI also runs `dotnet format --verify-no-changes`.
Neither check rewrites source. Standard
`dotnet format` does not reliably enforce blank lines between every member or
the logical groups inside methods. Review both explicitly; do not claim that
a formatter check proves those readability rules were followed.
