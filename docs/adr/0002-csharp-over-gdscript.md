---
status: accepted
---

# Game code is written in C#, using the Godot .NET build

The design doc originally assumed GDScript. We use C# instead because the developer already knows it, and because the game is mostly data-heavy rules logic (5e resolution, state machines, saves, quest flags) where C#'s type system and tooling help most. Pure rules code lives in a plain .NET library with no Godot dependency, so it can be tested quickly with `dotnet test`.

This decision was provisional until the end of M1.5, when it was reviewed against real experience of the Godot C# API. The friction logged during M1.5 (issue #8) was minor: Godot collections that aren't .NET collections, copied arrays such as `Line2D.Points`, stringly-typed theme names, and exported node paths that need `node_paths` in the scene file. None of it outweighed the type system and testing benefits, so C# was accepted.

## Considered Options

- **GDScript**: first-class in Godot, no build step, supports web export, and most tutorials and addons use it. We rejected it for now because it is a new language for the developer and has weaker refactoring and testing tools.

## Consequences

- No web export. C# projects in Godot 4 build for desktop only.
- Most tutorials and addons use GDScript, so examples need translating to C#.
- Custom Resources need the `[GlobalClass]` attribute to show up in the editor.
