using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

/// <summary>The spec's party and goblin, as Core records, for the rules tests.</summary>
internal static class TestCharacters
{
    public static readonly Armor ChainMail = new("Chain mail", BaseArmorClass: 16, MaxDexterityBonus: 0);
    public static readonly Armor Leather = new("Leather", BaseArmorClass: 11, MaxDexterityBonus: null);
    public static readonly Armor LeatherAndShield = new("Leather and shield", BaseArmorClass: 13, MaxDexterityBonus: null);

    public static readonly Attack Longsword =
        new("Longsword", Dice.Parse("1d8"), "slashing", AttackAbility.Strength, AddsAbilityModifierToDamage: true,
            RangeTiles: 1);

    public static readonly Attack Shortbow =
        new("Shortbow", Dice.Parse("1d6"), "piercing", AttackAbility.Dexterity, AddsAbilityModifierToDamage: true,
            RangeTiles: 16);

    public static readonly Attack FireBolt =
        new("Fire Bolt", Dice.Parse("1d10"), "fire", AttackAbility.Intelligence, AddsAbilityModifierToDamage: false,
            RangeTiles: 24);

    public static readonly Attack Scimitar =
        new("Scimitar", Dice.Parse("1d6"), "slashing", AttackAbility.Finesse, AddsAbilityModifierToDamage: true,
            RangeTiles: 1);

    public static Stats Fighter() =>
        new("Fighter", new AbilityScores(15, 13, 14, 8, 12, 10), MaxHp: 12, SpeedFeet: 30, ProficiencyBonus: 2,
            Longsword, ChainMail);

    public static Stats Rogue() =>
        new("Rogue", new AbilityScores(8, 15, 14, 12, 10, 13), MaxHp: 10, SpeedFeet: 30, ProficiencyBonus: 2,
            Shortbow, Leather);

    public static Stats Wizard() =>
        new("Wizard", new AbilityScores(8, 14, 13, 15, 12, 10), MaxHp: 7, SpeedFeet: 30, ProficiencyBonus: 2,
            FireBolt, Armor: null);

    public static Stats Goblin(string name = "Goblin 1") =>
        new(name, new AbilityScores(8, 14, 10, 10, 8, 8), MaxHp: 7, SpeedFeet: 30, ProficiencyBonus: 2,
            Scimitar, LeatherAndShield);
}
