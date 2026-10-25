namespace Crpg.Core.Combat;

/// <summary>The ability an attack rolls with. Finesse uses the higher of STR and DEX.</summary>
public enum AttackAbility
{
    Strength,
    Dexterity,
    Finesse,
    Intelligence,
}

/// <summary>Whether an attack is made in melee reach or from range.</summary>
public enum AttackReach
{
    Melee,
    Ranged,
}

/// <summary>
/// One way a character can attack, such as a weapon or a cantrip. The Core record for the game's WeaponData Resource.
/// </summary>
/// <param name="AddsAbilityModifierToDamage">False for cantrips such as Fire Bolt.</param>
/// <param name="RangeTiles">Range in tiles; melee reach is 1.</param>
public sealed record Attack(
    string Name,
    Dice Damage,
    string DamageType,
    AttackAbility Ability,
    bool AddsAbilityModifierToDamage,
    int RangeTiles,
    AttackReach Reach);
