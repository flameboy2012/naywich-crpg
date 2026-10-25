namespace Crpg.Core.Selection;

/// <summary>
/// An order for one party member, issued by a click. Concrete orders (move, later attack and interact)
/// derive from this.
/// </summary>
public abstract record Order(PartyMemberId PartyMember);
