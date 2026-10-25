using System.Collections.Generic;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for selection: loads Main, pushes real left-clicks through the viewport and checks
/// the selection ring. Run from game/ after dotnet build:
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
    }
}
