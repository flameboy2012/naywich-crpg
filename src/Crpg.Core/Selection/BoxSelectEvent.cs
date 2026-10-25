namespace Crpg.Core.Selection;

/// <summary>
/// One box select, already resolved by the game side to the party members inside the box, and whether shift was held.
/// </summary>
public readonly record struct BoxSelectEvent(IReadOnlyCollection<PartyMemberId> PartyMembers, bool Shift = false);
