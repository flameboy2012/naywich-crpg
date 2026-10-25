using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class SeedArgumentTests
{
    [Fact]
    public void Seed_followed_by_a_number_sets_the_seed()
    {
        Assert.Equal(42, SeedArgument.Find(["--headless", "--seed", "42"]));
    }

    [Fact]
    public void Seed_with_an_equals_sign_sets_the_seed()
    {
        Assert.Equal(-7, SeedArgument.Find(["--seed=-7"]));
    }

    [Fact]
    public void Without_a_seed_argument_there_is_no_seed()
    {
        Assert.Null(SeedArgument.Find(["--headless", "--path", "."]));
    }

    [Theory]
    [InlineData("--seed")]
    [InlineData("--seed", "many")]
    [InlineData("--seed=")]
    public void A_seed_argument_without_a_whole_number_is_rejected(params string[] args)
    {
        Assert.Throws<FormatException>(() => SeedArgument.Find(args));
    }
}
