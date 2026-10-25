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
    }
}
