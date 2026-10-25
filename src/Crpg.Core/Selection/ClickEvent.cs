namespace Crpg.Core.Selection;

public enum ClickButton
{
    Left,
    Right,
}

/// <summary>What a click landed on: a specific party member, a living enemy, or ground at a world position.</summary>
public abstract record ClickTarget
{
    private ClickTarget()
    {
    }

    /// <summary>A party member, with the world position they stand at.</summary>
    public sealed record PartyMember(PartyMemberId Id, WorldPosition Position) : ClickTarget;

    /// <summary>A living enemy, with the world position it stands at. Dead enemies can't be clicked.</summary>
    public sealed record Enemy(EnemyId Id, WorldPosition Position) : ClickTarget;

    public sealed record Ground(WorldPosition Position) : ClickTarget;
}

/// <summary>One mouse click, already resolved to a target by the game side, and whether shift was held.</summary>
public readonly record struct ClickEvent(ClickButton Button, ClickTarget Target, bool Shift = false)
{
    private static readonly IReadOnlyDictionary<PartyMemberId, WorldPosition> NobodyStanding =
        new Dictionary<PartyMemberId, WorldPosition>();

    /// <summary>
    /// Where each party member currently stands. A right-click uses it to give the selection their spread slots;
    /// a selected party member missing from it counts as standing at the clicked point.
    /// </summary>
    public IReadOnlyDictionary<PartyMemberId, WorldPosition> PartyMemberPositions { get; init; } = NobodyStanding;
}
