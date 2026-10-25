using System.Collections.Immutable;

namespace Crpg.Core.Selection;

/// <summary>
/// Holds the selection (a set of party members) and applies the control rules to click events.
/// Knows nothing about Godot: the game side resolves what a click hit and applies the result.
/// </summary>
public sealed class SelectionModel
{
    private static readonly IReadOnlyList<Order> NoOrders = Array.Empty<Order>();

    private readonly IReadOnlyList<PartyMemberId> _portraitSlots;

    private ImmutableHashSet<PartyMemberId> _selection = ImmutableHashSet<PartyMemberId>.Empty;

    /// <summary>A model for a party whose party members are given in portrait slot order: slot 1 first.</summary>
    public SelectionModel(IReadOnlyList<PartyMemberId> portraitSlots)
    {
        _portraitSlots = portraitSlots;
    }

    public IReadOnlySet<PartyMemberId> Selection => _selection;

    /// <summary>Applies one click and returns the selection after it plus any orders it issued.</summary>
    public ClickResult Click(ClickEvent click) => click.Button switch
    {
        ClickButton.Left when click.Shift => ShiftLeftClick(click.Target),
        ClickButton.Left => LeftClick(click.Target),
        ClickButton.Right => RightClick(click.Target, click.PartyMemberPositions),
        _ => CurrentResult(),
    };

    /// <summary>
    /// Right-click on a living enemy orders every selected party member to attack it. Right-click on ground, or on a
    /// party member's spot, sends every selected party member there, each to their own spread slot. The selection is
    /// unchanged.
    /// </summary>
    private ClickResult RightClick(ClickTarget target, IReadOnlyDictionary<PartyMemberId, WorldPosition> standing)
    {
        if (target is ClickTarget.Enemy enemy)
        {
            Order[] attacks = _selection.Select(partyMember => (Order)new AttackOrder(partyMember, enemy.Id)).ToArray();
            return new ClickResult(_selection, attacks);
        }

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

        Order[] orders = SpreadSlots.Assign(_selection, position, standing)
            .Select(slot => (Order)new MoveOrder(slot.PartyMember, slot.Destination))
            .ToArray();
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

    /// <summary>
    /// Shift+left-click on a party member toggles them in or out of the selection; on ground it changes nothing.
    /// </summary>
    private ClickResult ShiftLeftClick(ClickTarget target)
    {
        if (target is ClickTarget.PartyMember partyMember)
        {
            _selection = _selection.Contains(partyMember.Id)
                ? _selection.Remove(partyMember.Id)
                : _selection.Add(partyMember.Id);
        }

        return CurrentResult();
    }

    /// <summary>
    /// Applies one box select and returns the selection after it. Without shift the selection becomes exactly the
    /// party members in the box; with shift they are added to it. Box select never issues orders.
    /// </summary>
    public ClickResult BoxSelect(BoxSelectEvent box)
    {
        _selection = box.Shift
            ? _selection.Union(box.PartyMembers)
            : box.PartyMembers.ToImmutableHashSet();
        return CurrentResult();
    }

    /// <summary>
    /// Applies one selection by portrait slot and returns the selection after it. Without shift the selection becomes
    /// only the party member in that slot; with shift that party member is toggled in or out of it. Slot events never
    /// issue orders. A slot with no party member changes nothing.
    /// </summary>
    public ClickResult SlotSelect(SlotSelectEvent slot)
    {
        if (slot.Slot < 1 || slot.Slot > _portraitSlots.Count)
        {
            return CurrentResult();
        }

        PartyMemberId partyMember = _portraitSlots[slot.Slot - 1];
        if (!slot.Shift)
        {
            _selection = ImmutableHashSet.Create(partyMember);
        }
        else
        {
            _selection = _selection.Contains(partyMember) ? _selection.Remove(partyMember) : _selection.Add(partyMember);
        }

        return CurrentResult();
    }

    /// <summary>Selects every party member in a portrait slot and returns the selection after it. Issues no orders.</summary>
    public ClickResult SelectAll()
    {
        _selection = _portraitSlots.ToImmutableHashSet();
        return CurrentResult();
    }

    private ClickResult CurrentResult() => new(_selection, NoOrders);
}
