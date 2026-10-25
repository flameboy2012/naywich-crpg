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

    public sealed record PartyMember(PartyMemberId Id) : ClickTarget;

    public sealed record Ground(WorldPosition Position) : ClickTarget;
}

/// <summary>One mouse click, already resolved to a target by the game side.</summary>
public readonly record struct ClickEvent(ClickButton Button, ClickTarget Target);
