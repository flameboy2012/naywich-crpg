namespace Crpg.Core.Combat;

/// <summary>A suit of armour (with shield, if any). The Core record for the game's ArmorData Resource.</summary>
/// <param name="MaxDexterityBonus">
/// The most DEX modifier the armour allows; null means no limit. 0 is heavy armour: DEX is ignored entirely, even a
/// negative modifier.
/// </param>
public sealed record Armor(string Name, int BaseArmorClass, int? MaxDexterityBonus);
