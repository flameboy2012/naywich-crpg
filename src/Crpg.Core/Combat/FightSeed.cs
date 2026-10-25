namespace Crpg.Core.Combat;

/// <summary>The RNG seed for one fight, chosen at start-up and again when a defeat restarts the zone.</summary>
public static class FightSeed
{
    /// <summary>
    /// The seed given with <c>--seed</c>, so every fight in the run replays the same way; without one, a new random
    /// seed for each fight.
    /// </summary>
    public static int Choose(int? commandLineSeed, Func<int> randomSeed) => commandLineSeed ?? randomSeed();
}
