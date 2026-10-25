using Crpg.Core.Selection;

namespace Crpg.Core.Combat;

/// <summary>
/// The 5e combat rules, behind one small interface. Every roll goes through the injected <see cref="IDiceRoller"/>,
/// so one seed replays a whole fight. The game calls this module; it never calls the game.
/// </summary>
public sealed class CombatRules(IDiceRoller dice)
{
    public const int NaturalTwenty = 20;
    public const int NaturalOne = 1;

    /// <summary>The default global combat speed (ADR 0003): attack cooldowns and movement run in plain real seconds.</summary>
    public const float NormalCombatSpeed = 1f;

    /// <summary>
    /// Walking speed in world pixels per real second (ADR 0003): the speed in tiles per second times the grid-step
    /// length, scaled by the global combat speed like the attack cooldown.
    /// </summary>
    public static float MoveSpeedPixelsPerSecond(float speedTilesPerSecond, float combatSpeed) =>
        speedTilesPerSecond * TileScale.GridStepLength * combatSpeed;

    /// <summary>
    /// True when the target is within the attack's range: the distance between the two characters' feet is at most the
    /// range in tiles times the grid-step length plus both feet radii, so bodies that touch are always in melee reach.
    /// </summary>
    public static bool InRange(WorldPosition attackerFeet, float attackerFeetRadius, WorldPosition targetFeet,
        float targetFeetRadius, Attack attack) =>
        attackerFeet.DistanceTo(targetFeet)
        <= attack.RangeTiles * TileScale.GridStepLength + attackerFeetRadius + targetFeetRadius;

    /// <summary>Rolls the dice and sums them.</summary>
    public int Roll(Dice toRoll)
    {
        int total = 0;
        for (int i = 0; i < toRoll.Count; i++)
        {
            total += dice.Roll(toRoll.Sides);
        }

        return total;
    }

    /// <summary>
    /// Resolves one attack with the attacker's equipped attack: d20 + attack bonus against the target's AC. A natural
    /// 20 always hits and doubles the damage dice; a natural 1 always misses. A hit adds the attack ability modifier
    /// to damage unless the attack says not. Applying the damage is up to the caller.
    /// </summary>
    public AttackResult ResolveAttack(Stats attacker, Stats target)
    {
        Attack attack = attacker.Attack;
        int d20 = dice.Roll(20);
        int bonus = attacker.AttackBonus;
        int total = d20 + bonus;
        int armorClass = target.ArmorClass;
        bool hit = d20 != NaturalOne && (d20 == NaturalTwenty || total >= armorClass);
        if (!hit)
        {
            return new AttackResult(attacker.Name, target.Name, attack, d20, bonus, total, armorClass, Hit: false,
                DamageDice: null, DamageRoll: 0, DamageModifier: 0, Damage: 0);
        }

        Dice damageDice = d20 == NaturalTwenty ? attack.Damage.Doubled() : attack.Damage;
        int damageRoll = Roll(damageDice);
        int modifier = attack.AddsAbilityModifierToDamage ? attacker.AttackAbilityModifier : 0;
        return new AttackResult(attacker.Name, target.Name, attack, d20, bonus, total, armorClass, Hit: true,
            damageDice, damageRoll, modifier, Math.Max(0, damageRoll + modifier));
    }
}
