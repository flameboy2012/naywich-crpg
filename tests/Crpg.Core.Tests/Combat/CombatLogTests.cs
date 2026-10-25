using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class CombatLogTests
{
    [Fact]
    public void The_log_starts_with_the_seed_line()
    {
        var log = new CombatLog(seed: 42);

        Assert.Equal(["Seed: 42"], log.Lines);
    }

    [Fact]
    public void New_lines_go_at_the_bottom()
    {
        var log = new CombatLog(seed: 42);

        log.Add("first");
        log.Add("second");

        Assert.Equal(["Seed: 42", "first", "second"], log.Lines);
    }

    [Fact]
    public void The_log_keeps_only_the_last_8_lines()
    {
        var log = new CombatLog(seed: 42);

        for (int i = 1; i <= 10; i++)
        {
            log.Add($"line {i}");
        }

        Assert.Equal(8, CombatLog.ShownLines);
        Assert.Equal(Enumerable.Range(3, 8).Select(i => $"line {i}"), log.Lines);
    }

    private static string FighterAttacksGoblin(int goblinHpBefore, params int[] rolls)
    {
        AttackResult result = new CombatRules(new FixedRolls(rolls))
            .ResolveAttack(TestCharacters.Fighter(), TestCharacters.Goblin());
        int hpAfter = Math.Max(0, goblinHpBefore - result.Damage);
        return CombatLog.AttackLine(result, hpAfter, targetMaxHp: 7, targetIsEnemy: true);
    }

    [Fact]
    public void A_killing_hit_shows_the_roll_the_damage_and_the_dead_target()
    {
        Assert.Equal("Fighter → Goblin 1: 14+4=18 vs AC 15 hit, 1d8+2=7 slashing (Goblin 1: 0/7, dead)",
            FighterAttacksGoblin(7, 14, 5));
    }

    [Fact]
    public void A_hit_that_leaves_the_target_standing_shows_their_hp()
    {
        Assert.Equal("Fighter → Goblin 1: 15+4=19 vs AC 15 hit, 1d8+2=3 slashing (Goblin 1: 4/7)",
            FighterAttacksGoblin(7, 15, 1));
    }

    [Fact]
    public void A_miss_shows_the_roll_and_no_damage()
    {
        Assert.Equal("Fighter → Goblin 1: 9+4=13 vs AC 15 miss (Goblin 1: 7/7)", FighterAttacksGoblin(7, 9));
    }

    [Fact]
    public void A_natural_20_is_flagged_as_a_crit_with_doubled_dice()
    {
        Assert.Equal("Fighter → Goblin 1: 20+4=24 vs AC 15 nat 20 crit, 2d8+2=9 slashing (Goblin 1: 0/7, dead)",
            FighterAttacksGoblin(7, 20, 3, 4));
    }

    [Fact]
    public void A_natural_1_is_flagged_as_a_miss()
    {
        Assert.Equal("Fighter → Goblin 1: 1+4=5 vs AC 15 nat 1 miss (Goblin 1: 7/7)", FighterAttacksGoblin(7, 1));
    }

    [Fact]
    public void Damage_with_no_ability_modifier_shows_only_the_dice()
    {
        AttackResult result = new CombatRules(new FixedRolls(16, 6))
            .ResolveAttack(TestCharacters.Wizard(), TestCharacters.Goblin());

        Assert.Equal("Wizard → Goblin 1: 16+4=20 vs AC 15 hit, 1d10=6 fire (Goblin 1: 1/7)",
            CombatLog.AttackLine(result, targetHp: 1, targetMaxHp: 7, targetIsEnemy: true));
    }

    [Fact]
    public void A_negative_modifier_shows_with_a_minus_sign()
    {
        Stats weakling = TestCharacters.Fighter() with
        {
            Name = "Weakling", Abilities = new AbilityScores(6, 13, 14, 8, 12, 10), ProficiencyBonus = 2,
        };
        AttackResult result = new CombatRules(new FixedRolls(20, 4, 4)).ResolveAttack(weakling, TestCharacters.Goblin());

        Assert.Equal("Weakling → Goblin 1: 20+0=20 vs AC 15 nat 20 crit, 2d8-2=6 slashing (Goblin 1: 1/7)",
            CombatLog.AttackLine(result, targetHp: 1, targetMaxHp: 7, targetIsEnemy: true));
    }

    [Fact]
    public void A_party_member_at_0_hp_is_down_not_dead()
    {
        AttackResult result = new CombatRules(new FixedRolls(18, 6))
            .ResolveAttack(TestCharacters.Goblin(), TestCharacters.Wizard());

        Assert.Equal("Goblin 1 → Wizard: 18+4=22 vs AC 12 hit, 1d6+2=8 slashing (Wizard: 0/7, down)",
            CombatLog.AttackLine(result, targetHp: 0, targetMaxHp: 7, targetIsEnemy: false));
    }

    [Fact]
    public void Adding_a_line_raises_the_change_with_the_new_lines()
    {
        var log = new CombatLog(seed: 1);
        IReadOnlyList<string>? seen = null;
        log.Changed += lines => seen = lines;

        log.Add("hit");

        Assert.Equal(["Seed: 1", "hit"], seen);
    }
}
