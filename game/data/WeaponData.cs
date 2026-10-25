using Crpg.Core.Combat;
using Godot;

namespace Crpg.Data;

/// <summary>
/// The data for one attack, such as a weapon or a cantrip (Fire Bolt is a WeaponData too). Converts to a Core
/// <see cref="Attack"/> on load (ADR 0004).
/// </summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export]
    public string Name { get; set; } = "";

    /// <summary>Damage dice, written like "1d8".</summary>
    [Export]
    public string DamageDice { get; set; } = "1d4";

    [Export]
    public string DamageType { get; set; } = "bludgeoning";

    /// <summary>The ability the attack rolls with. Finesse uses the higher of STR and DEX.</summary>
    [Export]
    public AttackAbility Ability { get; set; } = AttackAbility.Strength;

    /// <summary>Whether the ability modifier adds to damage. False for cantrips.</summary>
    [Export]
    public bool AddsAbilityModifierToDamage { get; set; } = true;

    /// <summary>Range in tiles. Melee reach is 1.</summary>
    [Export(PropertyHint.Range, "1,60,1,or_greater")]
    public int RangeTiles { get; set; } = 1;

    public Attack ToCore() =>
        new(Name, Dice.Parse(DamageDice), DamageType, Ability, AddsAbilityModifierToDamage, RangeTiles);
}
