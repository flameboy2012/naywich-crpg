namespace Crpg.Core.Combat;

/// <summary>
/// The combat log's lines: the RNG seed first, then one line per attack, keeping the last <see cref="ShownLines"/>
/// with the newest at the bottom.
/// </summary>
public sealed class CombatLog
{
    /// <summary>How many lines the combat log keeps and shows.</summary>
    public const int ShownLines = 8;

    private readonly Queue<string> _lines = new();

    public CombatLog(int seed)
    {
        Add(SeedLine(seed));
    }

    /// <summary>Raised with the lines after every new line.</summary>
    public event Action<IReadOnlyList<string>>? Changed;

    /// <summary>The kept lines, oldest first.</summary>
    public IReadOnlyList<string> Lines => _lines.ToArray();

    /// <summary>The line that records the RNG seed, so a fight can be replayed with <c>--seed</c>.</summary>
    public static string SeedLine(int seed) => $"Seed: {seed}";

    /// <summary>
    /// One attack's line, for example
    /// <c>Fighter → Goblin 1: 14+4=18 vs AC 15 hit, 1d8+2=7 slashing (Goblin 1: 0/7, dead)</c>, with natural 20s
    /// flagged <c>nat 20 crit</c> and natural 1s <c>nat 1 miss</c>. The target's HP is after the attack; at 0 HP an
    /// enemy is dead and a party member is down.
    /// </summary>
    public static string AttackLine(AttackResult result, int targetHp, int targetMaxHp, bool targetIsEnemy)
    {
        string roll = $"{result.D20}{Signed(result.Bonus)}={result.Total} vs AC {result.TargetArmorClass}";
        string outcome = (result.Hit, result.NaturalTwenty, result.NaturalOne) switch
        {
            (true, true, _) => "nat 20 crit",
            (true, false, _) => "hit",
            (false, _, true) => "nat 1 miss",
            _ => "miss",
        };
        string damage = result.Hit
            ? $", {result.DamageDice}{(result.Attack.AddsAbilityModifierToDamage ? Signed(result.DamageModifier) : "")}"
              + $"={result.Damage} {result.DamageType}"
            : "";
        string state = targetHp > 0 ? "" : targetIsEnemy ? ", dead" : ", down";
        return $"{result.Attacker} → {result.Target}: {roll} {outcome}{damage} ({result.Target}: {targetHp}/{targetMaxHp}{state})";
    }

    private static string Signed(int value) => value < 0 ? $"{value}" : $"+{value}";

    public void Add(string line)
    {
        _lines.Enqueue(line);
        while (_lines.Count > ShownLines)
        {
            _lines.Dequeue();
        }

        Changed?.Invoke(Lines);
    }
}
