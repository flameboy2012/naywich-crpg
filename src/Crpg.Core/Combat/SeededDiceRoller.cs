namespace Crpg.Core.Combat;

/// <summary>
/// Rolls dice from one seeded generator, so the same seed always gives the same sequence of rolls and a fight can be
/// replayed from its seed.
/// </summary>
public sealed class SeededDiceRoller(int seed) : IDiceRoller
{
    // A seeded System.Random uses the same algorithm on every platform and .NET version, so seeds stay replayable.
    private readonly Random _random = new(seed);

    public int Seed { get; } = seed;

    public int Roll(int sides)
    {
        if (sides < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sides), sides, "A die needs at least one side.");
        }

        return _random.Next(1, sides + 1);
    }
}
