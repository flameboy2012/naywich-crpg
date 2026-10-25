namespace Crpg.Core.Selection;

/// <summary>Order for one party member to walk to a point on the ground.</summary>
public sealed record MoveOrder(PartyMemberId PartyMember, WorldPosition Destination) : Order(PartyMember);
