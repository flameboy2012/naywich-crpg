# Game data is Godot Resources that convert to plain Core records

Stats, weapons and monsters are authored as Godot Resources (`.tres`) in `game/data/`, so content is edited in the inspector, not in code. `Crpg.Core` has no Godot reference, so each Resource converts to a plain Core record (for example `CharacterStats` to `Stats`, `WeaponData` to `Attack`) when it loads, and all rules code takes only Core records. We rejected JSON parsed in Core because it loses inspector editing. We also rejected hardcoded values in C# because content would need code changes.

## Consequences

- Every new Resource type needs a matching Core record and a conversion. Keep the two shapes as close as possible.
- Rules stay unit-testable in xUnit without Godot.
- Derived values (attack bonus, AC) are computed from the Core record, never stored in the Resource.
