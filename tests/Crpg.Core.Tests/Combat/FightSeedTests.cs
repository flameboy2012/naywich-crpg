using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class FightSeedTests
{
    [Fact]
    public void Every_fight_uses_the_command_line_seed_when_one_was_given()
    {
        Func<int> noRandom = () => throw new InvalidOperationException("No random seed is needed.");

        Assert.Equal(42, FightSeed.Choose(42, noRandom));
        Assert.Equal(42, FightSeed.Choose(42, noRandom));
    }

    [Fact]
    public void Without_a_command_line_seed_each_fight_draws_a_new_random_seed()
    {
        var randomSeeds = new Queue<int>([5, 9]);

        Assert.Equal(5, FightSeed.Choose(null, randomSeeds.Dequeue));
        Assert.Equal(9, FightSeed.Choose(null, randomSeeds.Dequeue));
    }

    [Fact]
    public void A_fight_restarted_with_the_same_seed_replays_the_same_rolls()
    {
        IEnumerable<AttackResult> Fight(int seed)
        {
            var rules = new CombatRules(new SeededDiceRoller(seed));
            return Enumerable.Range(0, 10)
                .Select(_ => rules.ResolveAttack(TestCharacters.Fighter(), TestCharacters.Goblin()))
                .ToList();
        }

        Assert.Equal(Fight(1234), Fight(1234));
    }
}
