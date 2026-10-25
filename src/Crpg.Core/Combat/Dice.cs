using System.Globalization;
using System.Text.RegularExpressions;

namespace Crpg.Core.Combat;

/// <summary>A number of dice of one size, written "1d8" or "2d6". Modifiers are not part of the dice.</summary>
public readonly partial record struct Dice(int Count, int Sides)
{
    /// <summary>Parses "NdS" (case and surrounding spaces ignored). Throws <see cref="FormatException"/> otherwise.</summary>
    public static Dice Parse(string text)
    {
        Match match = DicePattern().Match(text.Trim());
        if (!match.Success)
        {
            throw new FormatException($"'{text}' is not a dice string like \"1d8\".");
        }

        int count = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int sides = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        if (count < 1 || sides < 1)
        {
            throw new FormatException($"'{text}' needs at least one die of at least one side.");
        }

        return new Dice(count, sides);
    }

    /// <summary>The same dice, twice as many of them: what a critical hit rolls.</summary>
    public Dice Doubled() => this with { Count = Count * 2 };

    public override string ToString() => $"{Count}d{Sides}";

    [GeneratedRegex(@"^(\d+)[dD](\d+)$")]
    private static partial Regex DicePattern();
}
