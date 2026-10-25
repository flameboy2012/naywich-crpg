using System.Globalization;

namespace Crpg.Core.Combat;

/// <summary>The <c>--seed N</c> command-line argument that replays a fight from its RNG seed.</summary>
public static class SeedArgument
{
    private const string Flag = "--seed";

    /// <summary>
    /// The seed given as <c>--seed N</c> or <c>--seed=N</c>, or null when there is none. Throws
    /// <see cref="FormatException"/> when the flag has no whole number.
    /// </summary>
    public static int? Find(IReadOnlyList<string> args)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (args[i] == Flag)
            {
                return Parse(i + 1 < args.Count ? args[i + 1] : "");
            }

            if (args[i].StartsWith(Flag + "=", StringComparison.Ordinal))
            {
                return Parse(args[i][(Flag.Length + 1)..]);
            }
        }

        return null;
    }

    private static int Parse(string value) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int seed)
            ? seed
            : throw new FormatException($"{Flag} needs a whole number, not '{value}'.");
}
