using System.Collections.Immutable;

namespace Crpg.Core.Selection;

/// <summary>
/// Holds the selection (a set of party members) and applies the control rules to click events.
/// Knows nothing about Godot: the game side resolves what a click hit and applies the result.
/// </summary>
public sealed class SelectionModel
{
    private static readonly IReadOnlyList<Order> NoOrders = Array.Empty<Order>();

    private ImmutableHashSet<PartyMemberId> _selection = ImmutableHashSet<PartyMemberId>.Empty;

    public IReadOnlySet<PartyMemberId> Selection => _selection;

    /// <summary>Applies one click and returns the selection after it plus any orders it issued.</summary>
    public ClickResult Click(ClickEvent click) => click.Button switch
    {
        ClickButton.Left => LeftClick(click.Target),
        ClickButton.Right => RightClick(click.Target),
        _ => CurrentResult(),
    };

    /// <summary>
    /// Right-click on ground, or on a party member's spot, sends every selected party member there; the selection
    /// is unchanged.
    /// </summary>
    private ClickResult RightClick(ClickTarget target)
    {
        WorldPosition? destination = target switch
        {
            ClickTarget.Ground ground => ground.Position,
            ClickTarget.PartyMember partyMember => partyMember.Position,
            _ => null,
        };
        if (destination is not { } position)
        {
            return CurrentResult();
        }

        Order[] orders = _selection.Select(id => (Order)new MoveOrder(id, position)).ToArray();
        return new ClickResult(_selection, orders);
    }

    private ClickResult LeftClick(ClickTarget target)
    {
        _selection = target switch
        {
            ClickTarget.PartyMember partyMember => ImmutableHashSet.Create(partyMember.Id),
            ClickTarget.Ground => ImmutableHashSet<PartyMemberId>.Empty,
            _ => _selection,
        };
        return CurrentResult();
    }

    private ClickResult CurrentResult() => new(_selection, NoOrders);
}
