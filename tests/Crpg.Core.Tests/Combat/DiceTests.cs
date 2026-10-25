using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class DiceTests
{
    [Theory]
    [InlineData("1d8", 1, 8)]
    [InlineData("2d6", 2, 6)]
    [InlineData("1d10", 1, 10)]
    [InlineData(" 3D4 ", 3, 4)]
    public void A_dice_string_parses_into_a_count_and_sides(string text, int count, int sides)
    {
        Dice dice = Dice.Parse(text);

        Assert.Equal(count, dice.Count);
        Assert.Equal(sides, dice.Sides);
    }

    [Theory]
    [InlineData("")]
    [InlineData("d8")]
    [InlineData("1d")]
    [InlineData("0d6")]
    [InlineData("1d0")]
    [InlineData("1d8+2")]
    [InlineData("eight")]
    public void A_malformed_dice_string_is_rejected(string text)
    {
        Assert.Throws<FormatException>(() => Dice.Parse(text));
    }

    [Fact]
    public void Dice_print_the_way_they_are_written()
    {
        Assert.Equal("2d6", Dice.Parse("2d6").ToString());
    }

    [Fact]
    public void Rolling_dice_sums_one_roll_per_die()
    {
        var rolls = new FixedRolls(4, 6);

        int total = new CombatRules(rolls).Roll(Dice.Parse("2d6"));

        Assert.Equal(10, total);
        Assert.Equal([6, 6], rolls.SidesAsked);
    }

    [Fact]
    public void Seeded_rolls_always_land_between_the_dice_minimum_and_maximum()
    {
        var rules = new CombatRules(new SeededDiceRoller(7));
        Dice dice = Dice.Parse("2d6");

        int[] totals = Enumerable.Range(0, 1000).Select(_ => rules.Roll(dice)).ToArray();

        Assert.All(totals, total => Assert.InRange(total, 2, 12));
        Assert.Contains(2, totals);
        Assert.Contains(12, totals);
    }

    [Fact]
    public void A_crit_doubles_the_number_of_dice()
    {
        Assert.Equal(Dice.Parse("2d8"), Dice.Parse("1d8").Doubled());
    }
}
