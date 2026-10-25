using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class AttackResolutionTests
{
    private static AttackResult Resolve(Stats attacker, Stats target, params int[] rolls) =>
        new CombatRules(new FixedRolls(rolls)).ResolveAttack(attacker, target);

    [Fact]
    public void A_total_equal_to_the_armour_class_hits()
    {
        // Fighter +4 vs goblin AC 15: an 11 totals 15.
        AttackResult result = Resolve(TestCharacters.Fighter(), TestCharacters.Goblin(), 11, 5);

        Assert.Equal(11, result.D20);
        Assert.Equal(4, result.Bonus);
        Assert.Equal(15, result.Total);
        Assert.Equal(15, result.TargetArmorClass);
        Assert.True(result.Hit);
    }

    [Fact]
    public void A_total_below_the_armour_class_misses_and_rolls_no_damage()
    {
        var rolls = new FixedRolls(10);
        AttackResult result = new CombatRules(rolls).ResolveAttack(TestCharacters.Fighter(), TestCharacters.Goblin());

        Assert.Equal(14, result.Total);
        Assert.False(result.Hit);
        Assert.Equal(0, result.Damage);
        Assert.Equal([20], rolls.SidesAsked);
    }

    [Fact]
    public void A_hit_rolls_the_attack_dice_and_adds_the_ability_modifier()
    {
        AttackResult result = Resolve(TestCharacters.Fighter(), TestCharacters.Goblin(), 15, 5);

        Assert.Equal(Dice.Parse("1d8"), result.DamageDice);
        Assert.Equal(5, result.DamageRoll);
        Assert.Equal(2, result.DamageModifier);
        Assert.Equal(7, result.Damage);
        Assert.Equal("slashing", result.DamageType);
    }

    [Fact]
    public void Fire_Bolt_adds_no_ability_modifier_to_damage()
    {
        AttackResult result = Resolve(TestCharacters.Wizard(), TestCharacters.Goblin(), 15, 6);

        Assert.Equal(4, result.Bonus); // INT still adds to the attack roll
        Assert.Equal(Dice.Parse("1d10"), result.DamageDice);
        Assert.Equal(0, result.DamageModifier);
        Assert.Equal(6, result.Damage);
        Assert.Equal("fire", result.DamageType);
    }

    [Fact]
    public void A_finesse_attack_adds_the_higher_of_strength_and_dexterity_to_damage()
    {
        AttackResult result = Resolve(TestCharacters.Goblin(), TestCharacters.Rogue(), 15, 3);

        Assert.Equal(2, result.DamageModifier); // DEX 14, not STR 8
        Assert.Equal(5, result.Damage);
    }

    [Fact]
    public void A_natural_20_hits_whatever_the_armour_class_and_doubles_the_damage_dice()
    {
        Stats untouchable = TestCharacters.Goblin() with { Armor = new Armor("Plate of legend", 40, 0) };

        AttackResult result = Resolve(TestCharacters.Fighter(), untouchable, 20, 3, 6);

        Assert.True(result.NaturalTwenty);
        Assert.True(result.Hit);
        Assert.Equal(Dice.Parse("2d8"), result.DamageDice);
        Assert.Equal(9, result.DamageRoll);
        Assert.Equal(11, result.Damage); // the modifier is added once
    }

    [Fact]
    public void A_natural_1_misses_whatever_the_bonus()
    {
        Stats mighty = TestCharacters.Fighter() with { ProficiencyBonus = 30 };

        AttackResult result = Resolve(mighty, TestCharacters.Goblin(), 1);

        Assert.True(result.NaturalOne);
        Assert.False(result.Hit);
        Assert.Equal(0, result.Damage);
    }

    [Fact]
    public void Damage_is_never_below_zero()
    {
        Stats feeble = TestCharacters.Fighter() with
        {
            Abilities = TestCharacters.Fighter().Abilities with { Strength = 1 }, ProficiencyBonus = 20,
        };

        AttackResult result = Resolve(feeble, TestCharacters.Goblin(), 15, 1);

        Assert.Equal(-5, result.DamageModifier);
        Assert.Equal(0, result.Damage);
    }

    [Fact]
    public void The_result_names_the_attacker_target_and_attack()
    {
        AttackResult result = Resolve(TestCharacters.Fighter(), TestCharacters.Goblin("Goblin 2"), 2);

        Assert.Equal("Fighter", result.Attacker);
        Assert.Equal("Goblin 2", result.Target);
        Assert.Equal("Longsword", result.Attack.Name);
    }

    [Fact]
    public void The_same_seed_gives_the_same_sequence_of_rolls()
    {
        var first = new CombatRules(new SeededDiceRoller(1234));
        var second = new CombatRules(new SeededDiceRoller(1234));
        var other = new CombatRules(new SeededDiceRoller(4321));

        AttackResult[] Fight(CombatRules rules) => Enumerable.Range(0, 50)
            .Select(_ => rules.ResolveAttack(TestCharacters.Fighter(), TestCharacters.Goblin()))
            .ToArray();

        AttackResult[] firstFight = Fight(first);
        Assert.Equal(firstFight, Fight(second));
        Assert.NotEqual(firstFight, Fight(other));
    }
}
