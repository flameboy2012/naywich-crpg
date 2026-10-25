namespace Crpg.Core.Combat;

/// <summary>Everything one attack rolled and decided, enough to apply it and to write its combat log line.</summary>
/// <param name="D20">The natural d20 roll.</param>
/// <param name="Bonus">The attacker's attack bonus.</param>
/// <param name="Total">D20 plus bonus.</param>
/// <param name="DamageDice">The dice rolled for damage: doubled on a natural 20. Null on a miss.</param>
/// <param name="DamageRoll">The sum of the damage dice; 0 on a miss.</param>
/// <param name="DamageModifier">The ability modifier added to damage; 0 for attacks that add none, and on a miss.</param>
/// <param name="Damage">Damage dealt: roll plus modifier, never below 0; 0 on a miss.</param>
public sealed record AttackResult(
    string Attacker,
    string Target,
    Attack Attack,
    int D20,
    int Bonus,
    int Total,
    int TargetArmorClass,
    bool Hit,
    Dice? DamageDice,
    int DamageRoll,
    int DamageModifier,
    int Damage)
{
    public bool NaturalTwenty => D20 == CombatRules.NaturalTwenty;

    public bool NaturalOne => D20 == CombatRules.NaturalOne;

    public string DamageType => Attack.DamageType;
}
