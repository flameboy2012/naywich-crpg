namespace Crpg.Core.Combat;

/// <summary>
/// A character's stats and equipment: the Core record for the game's CharacterStats Resource (ADR 0004). Attack bonus
/// and armour class are derived here, never stored.
/// </summary>
/// <param name="SpeedFeet">Walking speed in feet per 6-second round, as in 5e.</param>
/// <param name="Armor">The equipped armour, or null for none.</param>
public sealed record Stats(
    string Name,
    AbilityScores Abilities,
    int MaxHp,
    int SpeedFeet,
    int ProficiencyBonus,
    Attack Attack,
    Armor? Armor)
{
    /// <summary>The 5e speed that moves one tile per real second (ADR 0001, ADR 0003).</summary>
    private const float FeetPerTilePerSecond = 30f;

    /// <summary>Movement speed in tiles per real second at normal combat speed: 30 ft is 1 tile per second.</summary>
    public float SpeedTilesPerSecond => SpeedFeet / FeetPerTilePerSecond;

    /// <summary>The modifier the equipped attack rolls with and, unless it says not, adds to damage.</summary>
    public int AttackAbilityModifier => Attack.Ability switch
    {
        AttackAbility.Strength => Abilities.StrengthModifier,
        AttackAbility.Dexterity => Abilities.DexterityModifier,
        AttackAbility.Finesse => Math.Max(Abilities.StrengthModifier, Abilities.DexterityModifier),
        AttackAbility.Intelligence => Abilities.IntelligenceModifier,
        _ => throw new InvalidOperationException($"Unknown attack ability {Attack.Ability}."),
    };

    /// <summary>Proficiency bonus plus the attack ability modifier.</summary>
    public int AttackBonus => ProficiencyBonus + AttackAbilityModifier;

    /// <summary>Armour base plus the DEX modifier up to the armour's cap; 10 + DEX with no armour.</summary>
    public int ArmorClass
    {
        get
        {
            int dexterity = Abilities.DexterityModifier;
            if (Armor is null)
            {
                return 10 + dexterity;
            }

            return Armor.MaxDexterityBonus switch
            {
                null => Armor.BaseArmorClass + dexterity,
                0 => Armor.BaseArmorClass,
                int cap => Armor.BaseArmorClass + Math.Min(dexterity, cap),
            };
        }
    }
}
