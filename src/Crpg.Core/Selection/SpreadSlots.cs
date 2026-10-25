namespace Crpg.Core.Selection;

/// <summary>
/// Gives each party member in a group move their own spread slot around the clicked point, so they don't stack.
/// The pattern is fixed and world-aligned: it never turns with the direction of the move.
/// </summary>
public static class SpreadSlots
{
    /// <summary>
    /// Distance in world pixels from the clicked point to every other slot. About one tile: one isometric grid step,
    /// (32, 16), about 35.78 px (ADR 0001). Tunable.
    /// </summary>
    public const float Spacing = 35.78f;

    /// <summary>
    /// Directions from the clicked point to slots 1 onwards, as unit vectors in world space (y points down the screen):
    /// up, down-right and down-left, 120 degrees apart, so those slots are also well over a tile from each other.
    /// </summary>
    private static readonly (float X, float Y)[] Directions =
    {
        (0f, -1f),
        (0.8660254f, 0.5f),
        (-0.8660254f, 0.5f),
    };

    /// <summary>The most party members one group move can spread out: slot 0 plus one per direction.</summary>
    public static int Count => Directions.Length + 1;

    /// <summary>
    /// The destination of each party member sent to <paramref name="clicked"/>, in ID order. A lone party member goes
    /// exactly to the clicked point. With several, the first slots of the pattern are shared out by trying every
    /// assignment (at most 4! = 24) and taking the one with the shortest total walking distance. Ties go to the first
    /// such assignment in a fixed order (party members sorted by ID, slots taken lowest first), so the same input
    /// always gives the same result. A party member missing from <paramref name="standing"/> counts as standing at
    /// the clicked point.
    /// </summary>
    internal static IEnumerable<(PartyMemberId PartyMember, WorldPosition Destination)> Assign(
        IEnumerable<PartyMemberId> partyMembers, WorldPosition clicked,
        IReadOnlyDictionary<PartyMemberId, WorldPosition> standing)
    {
        PartyMemberId[] ordered = partyMembers.OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();
        if (ordered.Length > Count)
        {
            throw new InvalidOperationException(
                $"A group move can spread out at most {Count} party members, not {ordered.Length}.");
        }

        WorldPosition[] slots = Enumerable.Range(0, ordered.Length).Select(slot => Slot(clicked, slot)).ToArray();
        WorldPosition[] from = ordered
            .Select(id => standing.TryGetValue(id, out WorldPosition position) ? position : clicked)
            .ToArray();

        int[] best = Array.Empty<int>();
        float bestTotal = float.PositiveInfinity;
        foreach (int[] assignment in Permutations(ordered.Length))
        {
            float total = 0f;
            for (int i = 0; i < assignment.Length; i++)
            {
                total += Distance(from[i], slots[assignment[i]]);
            }

            if (total < bestTotal)
            {
                bestTotal = total;
                best = assignment;
            }
        }

        return ordered.Select((id, i) => (id, slots[best[i]]));
    }

    /// <summary>Every ordering of 0..n-1, in lexicographic order.</summary>
    private static IEnumerable<int[]> Permutations(int n)
    {
        if (n == 0)
        {
            yield return Array.Empty<int>();
            yield break;
        }

        for (int first = 0; first < n; first++)
        {
            foreach (int[] rest in Permutations(n - 1))
            {
                yield return rest.Select(slot => slot >= first ? slot + 1 : slot).Prepend(first).ToArray();
            }
        }
    }

    private static float Distance(WorldPosition a, WorldPosition b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static WorldPosition Slot(WorldPosition clicked, int slot)
    {
        if (slot == 0)
        {
            return clicked;
        }

        (float x, float y) = Directions[slot - 1];
        return new WorldPosition(clicked.X + x * Spacing, clicked.Y + y * Spacing);
    }
}
