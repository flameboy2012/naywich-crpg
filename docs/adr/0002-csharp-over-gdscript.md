---
status: proposed
---

# Game code is written in C#, using the Godot .NET build

The design doc originally assumed GDScript. We use C# instead because the developer already knows it, and because the game is mostly data-heavy rules logic (5e resolution, state machines, saves, quest flags) where C#'s type system and tooling help most. Pure rules code lives in a plain .NET library with no Godot dependency, so it can be tested quickly with `dotnet test`.

This decision is provisional. Review it at the end of M1.5, once there is real experience of the Godot C# API. Switching back is cheap at that point because the codebase is small, and the .NET editor also runs GDScript.

## Considered Options

- **GDScript**: first-class in Godot, no build step, supports web export, and most tutorials and addons use it. We rejected it for now because it is a new language for the developer and has weaker refactoring and testing tools.

## Consequences

- No web export. C# projects in Godot 4 build for desktop only.
- Most tutorials and addons use GDScript, so examples need translating to C#.
- Custom Resources need the `[GlobalClass]` attribute to show up in the editor.
