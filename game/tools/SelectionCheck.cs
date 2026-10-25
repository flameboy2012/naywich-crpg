using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for selection: loads Main, pushes real left-clicks through the viewport and checks
/// every party member's selection ring. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 300 --script res://tools/SelectionCheck.cs
/// Prints "SelectionCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class SelectionCheck : HeadlessCheck
{
    protected override IEnumerator<int> Run()
    {
        Vector2 onGround = PartyMember.GlobalPosition + new Vector2(200, 60);
        Expect("ring hidden at start", !PartyMember.SelectionRingVisible);

        ClickPartyMember(MouseButton.Left);
        yield return 1;
        Expect("left-click party member shows ring", PartyMember.SelectionRingVisible);

        ClickPartyMember(MouseButton.Left);
        yield return 1;
        Expect("left-click selected party member keeps ring", PartyMember.SelectionRingVisible);

        Click(MouseButton.Left, onGround);
        yield return 1;
        Expect("left-click ground hides ring", !PartyMember.SelectionRingVisible);

        ClickPartyMember(MouseButton.Left);
        yield return 1;
        Expect("left-click party member again shows ring", PartyMember.SelectionRingVisible);

        Expect($"the party is full ({PartyMembers.Count} party members, cap {Core.Party.Cap})",
            PartyMembers.Count == Core.Party.Cap);
        Expect("every party member has a different tint",
            PartyMembers.Select(partyMember => partyMember.Tint).Distinct().Count() == PartyMembers.Count);
        foreach (Character partyMember in PartyMembers)
        {
            ClickPartyMember(MouseButton.Left, partyMember);
            yield return 1;
            Expect($"left-click {partyMember.Name} shows only their ring",
                PartyMembers.All(other => other.SelectionRingVisible == (other == partyMember)));
        }

        Click(MouseButton.Left, onGround);
        yield return 1;
        Expect("left-click ground hides every ring", PartyMembers.All(partyMember => !partyMember.SelectionRingVisible));

        Character first = PartyMembers[0];
        Character second = PartyMembers[1];
        ClickPartyMember(MouseButton.Left, first);
        ClickPartyMember(MouseButton.Left, second, shift: true);
        yield return 1;
        Expect("shift-click adds a party member", RingsShow(first, second));

        Click(MouseButton.Left, onGround, shift: true);
        yield return 1;
        Expect("shift-click ground keeps the selection", RingsShow(first, second));

        ClickPartyMember(MouseButton.Left, first, shift: true);
        yield return 1;
        Expect("shift-click a selected party member removes them", RingsShow(second));

        ClickPartyMember(MouseButton.Left, first);
        yield return 1;
        Expect("left-click without shift selects only the clicked party member", RingsShow(first));

        foreach (int step in BoxSelectSteps(onGround))
        {
            yield return step;
        }
    }

    private IEnumerable<int> BoxSelectSteps(Vector2 onGround)
    {
        SelectionController controller = Root.GetNode<SelectionController>("Main/SelectionController");
        Character first = PartyMembers[0];
        Character second = PartyMembers[1];
        Character third = PartyMembers[2];
        (Vector2 boxFrom, Vector2 boxTo) = BoxAround(first, second);
        Expect("only the first two party members' feet are inside the test box",
            PartyMembers.All(partyMember =>
                Inside(partyMember.GlobalPosition, boxFrom, boxTo) == (partyMember == first || partyMember == second)));

        Click(MouseButton.Left, onGround);
        yield return 1;
        Expect("box hidden before dragging", !controller.BoxVisible);

        BeginDrag(boxFrom, boxTo);
        yield return 1;
        Expect("box shown while dragging", controller.BoxVisible);

        EndDrag(boxTo);
        yield return 1;
        Expect("box hidden after release", !controller.BoxVisible);
        Expect("drag around two party members selects exactly those two", RingsShow(first, second));

        ClickPartyMember(MouseButton.Left, third);
        Drag(boxFrom, boxTo, shift: true);
        yield return 1;
        Expect("shift+drag adds the party members in the box", RingsShow(first, second, third));

        Drag(onGround, onGround + new Vector2(40, 30));
        yield return 1;
        Expect("an empty box clears the selection", RingsShow());

        ClickPartyMember(MouseButton.Left, first);
        Vector2 onThird = third.GlobalPosition + new Vector2(0, -24);
        BeginDrag(onThird, onThird + new Vector2(3, 2));
        yield return 1;
        Expect("box hidden for a drag under the threshold", !controller.BoxVisible);
        EndDrag(onThird + new Vector2(3, 2));
        yield return 1;
        Expect("a drag under the threshold counts as a click", RingsShow(third));
    }

    /// <summary>World corners of a box just around two party members' feet.</summary>
    private static (Vector2 From, Vector2 To) BoxAround(Character a, Character b)
    {
        var margin = new Vector2(8, 8);
        return (a.GlobalPosition.Min(b.GlobalPosition) - margin, a.GlobalPosition.Max(b.GlobalPosition) + margin);
    }

    private static bool Inside(Vector2 point, Vector2 from, Vector2 to) =>
        new Rect2(from, to - from).Abs().HasPoint(point);

    /// <summary>True when exactly the given party members' rings are visible and every other ring is hidden.</summary>
    private bool RingsShow(params Character[] selected) =>
        PartyMembers.All(partyMember => partyMember.SelectionRingVisible == selected.Contains(partyMember));
}
