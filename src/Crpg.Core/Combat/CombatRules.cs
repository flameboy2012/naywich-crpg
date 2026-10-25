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

    /// <summary>
    /// The default global combat speed (ADR 0003): attack cooldowns and movement run in plain real seconds.
    /// </summary>
    public const float NormalCombatSpeed = 1f;

    /// <summary>
    /// The awareness radius, in tiles, of a character whose data doesn't set one: the default for every monster.
    /// Provisional until the M2 play-test.
    /// </summary>
    public const int DefaultAwarenessRadiusTiles = 8;

    /// <summary>
    /// Walking speed in world pixels per real second (ADR 0003): the speed in tiles per second times the grid-step
    /// length, scaled by the global combat speed like the attack cooldown.
    /// </summary>
    public static float MoveSpeedPixelsPerSecond(float speedTilesPerSecond, float combatSpeed) =>
        speedTilesPerSecond * TileScale.GridStepLength * combatSpeed;

    /// <summary>Melee reach in tiles: the range of every melee attack.</summary>
    public const int MeleeReachTiles = 1;

    /// <summary>
    /// The farthest apart, in world pixels, two characters' feet may be for an attack of this range to reach: the
    /// range in tiles times the grid-step length plus both feet radii.
    /// </summary>
    public static float MaxAttackDistance(int rangeTiles, float attackerFeetRadius, float targetFeetRadius) =>
        rangeTiles * TileScale.GridStepLength + attackerFeetRadius + targetFeetRadius;

    /// <summary>
    /// True when the target is within the attack's range: the distance between the two characters' feet is at most
    /// <see cref="MaxAttackDistance"/>, so bodies that touch are always in melee reach.
    /// </summary>
    public static bool InRange(WorldPosition attackerFeet, float attackerFeetRadius, WorldPosition targetFeet,
        float targetFeetRadius, Attack attack) =>
        attackerFeet.DistanceTo(targetFeet)
        <= MaxAttackDistance(attack.RangeTiles, attackerFeetRadius, targetFeetRadius);

    /// <summary>
    /// An enemy's target pick. An enemy becomes aware of the party when a party member who is not down comes within
    /// its awareness radius (in tiles of one grid-step length, feet to feet), and stays aware until it is dead: once
    /// aware, there is no leash. An aware enemy keeps its current target while that party member is up, however far
    /// away they are; otherwise it picks the nearest party member who is not down, at any distance. An unaware enemy,
    /// or an aware one with every party member down, picks nobody.
    /// </summary>
    public static EnemyTargetPick PickEnemyTarget(WorldPosition enemyFeet, int awarenessRadiusTiles, bool isAware,
        PartyMemberId? current, IReadOnlyList<TargetCandidate> partyMembers)
    {
        TargetCandidate[] up = partyMembers.Where(candidate => !candidate.IsDown).ToArray();
        float radius = awarenessRadiusTiles * TileScale.GridStepLength;
        bool aware = isAware || up.Any(candidate => enemyFeet.DistanceTo(candidate.Feet) <= radius);
        if (!aware)
        {
            return new EnemyTargetPick(IsAware: false, null);
        }

        if (current is { } kept && up.Any(candidate => candidate.Id == kept))
        {
            return new EnemyTargetPick(IsAware: true, kept);
        }

        PartyMemberId? nearest = up.Length == 0
            ? null
            : up.MinBy(candidate => enemyFeet.DistanceTo(candidate.Feet)).Id;
        return new EnemyTargetPick(IsAware: true, nearest);
    }

    /// <summary>The HP a downed party member gets up with at combat end.</summary>
    public const int GetUpHp = 1;

    /// <summary>
    /// Where combat stands, given every enemy in the zone and whether each party member is down. Every party member
    /// down is defeat. Otherwise combat has ended when no living enemy is aware of the party.
    /// </summary>
    public static CombatState AssessCombat(IEnumerable<EnemyAwareness> enemies, IReadOnlyCollection<bool> partyMembersDown)
    {
        if (partyMembersDown.Count > 0 && partyMembersDown.All(isDown => isDown))
        {
            return CombatState.Defeat;
        }

        return enemies.Any(enemy => !enemy.IsDead && enemy.IsAware) ? CombatState.Fighting : CombatState.Ended;
    }

    /// <summary>
    /// A party member's HP after combat end: a downed party member (0 HP) gets up at 1 HP; others keep theirs.
    /// </summary>
    public static int HpAtCombatEnd(int hp) => hp > 0 ? hp : GetUpHp;

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
