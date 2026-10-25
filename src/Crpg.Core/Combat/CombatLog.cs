namespace Crpg.Core.Combat;

/// <summary>
/// The combat log's lines: the RNG seed first, then one line per attack, keeping the last <see cref="ShownLines"/>
/// with the newest at the bottom.
/// </summary>
public sealed class CombatLog
{
    /// <summary>How many lines the combat log keeps and shows.</summary>
    public const int ShownLines = 8;

    private readonly Queue<string> _lines = new();

    public CombatLog(int seed)
    {
        Add(SeedLine(seed));
    }

    /// <summary>Raised with the lines after every new line.</summary>
    public event Action<IReadOnlyList<string>>? Changed;

    /// <summary>The kept lines, oldest first.</summary>
    public IReadOnlyList<string> Lines => _lines.ToArray();

    /// <summary>The line that records the RNG seed, so a fight can be replayed with <c>--seed</c>.</summary>
    public static string SeedLine(int seed) => $"Seed: {seed}";

    public void Add(string line)
    {
        _lines.Enqueue(line);
        while (_lines.Count > ShownLines)
        {
            _lines.Dequeue();
        }

        Changed?.Invoke(Lines);
    }
}
