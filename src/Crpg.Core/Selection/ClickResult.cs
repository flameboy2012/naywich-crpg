namespace Crpg.Core.Selection;

/// <summary>The outcome of one click: the selection after it, and the orders it issued (possibly none).</summary>
public sealed record ClickResult(IReadOnlySet<PartyMemberId> Selection, IReadOnlyList<Order> Orders);
