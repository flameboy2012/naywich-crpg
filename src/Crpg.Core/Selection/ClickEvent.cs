namespace Crpg.Core.Selection;

public enum ClickButton
{
    Left,
    Right,
}

/// <summary>What a click landed on: a specific party member, or ground at a world position.</summary>
public abstract record ClickTarget
{
    private ClickTarget()
    {
    }

    /// <summary>A party member, with the world position they stand at.</summary>
    public sealed record PartyMember(PartyMemberId Id, WorldPosition Position) : ClickTarget;

    public sealed record Ground(WorldPosition Position) : ClickTarget;
}

/// <summary>One mouse click, already resolved to a target by the game side, and whether shift was held.</summary>
public readonly record struct ClickEvent(ClickButton Button, ClickTarget Target, bool Shift = false);
