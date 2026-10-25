namespace Crpg.Core.Selection;

/// <summary>Identity of one party member, as the selection model sees it.</summary>
public readonly record struct PartyMemberId(string Value)
{
    public override string ToString() => Value;
}
