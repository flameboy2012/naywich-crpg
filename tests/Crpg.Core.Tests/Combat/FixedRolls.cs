using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

/// <summary>A die roller that hands out the given rolls in order, so a test decides every d20 and damage die.</summary>
internal sealed class FixedRolls(params int[] rolls) : IDiceRoller
{
    private readonly Queue<int> _rolls = new(rolls);

    public List<int> SidesAsked { get; } = new();

    public int Roll(int sides)
    {
        SidesAsked.Add(sides);
        int roll = _rolls.Dequeue();
        if (roll < 1 || roll > sides)
        {
            throw new InvalidOperationException($"Fixed roll {roll} is not on a d{sides}.");
        }

        return roll;
    }
}
