using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for right-click movement: loads Main, pushes real clicks through the viewport and checks
/// the party member's position. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 1800--script res://tools/MoveCheck.cs
/// Prints "MoveCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class MoveCheck : HeadlessCheck
{
    protected override IEnumerator<int> Run()
    {
        Vector2 start = PartyMember.GlobalPosition;
        Vector2 first = start + new Vector2(200, 0);
        Vector2 second = start + new Vector2(-100, 150);
        Dictionary<Character, Vector2> others = PartyMembers.Skip(1)
            .ToDictionary(other => other, other => other.GlobalPosition);

        Click(MouseButton.Right, first);
        yield return 30;
        Expect("right-click with nothing selected leaves them still",
            PartyMember.GlobalPosition.DistanceTo(start) < StillTolerance && !PartyMember.IsMoving);

        ClickPartyMember(MouseButton.Left);
        yield return 2;
        Expect("left-click selects them", PartyMember.SelectionRingVisible);

        Click(MouseButton.Right, first);
        yield return 1;
        Vector2 from = PartyMember.GlobalPosition;
        yield return 60;
        float walked = PartyMember.GlobalPosition.DistanceTo(from);
        Expect($"walks about one tile per second ({walked:0.0} px in 1 s, tile = {Character.TileLength:0.00} px)",
            Mathf.Abs(walked - Character.TileLength) < Character.TileLength * 0.05f);
        Expect("walks towards the clicked point", PartyMember.GlobalPosition.DistanceTo(first) < from.DistanceTo(first));

        Click(MouseButton.Right, second);
        yield return 2;
        Expect("ring stays after right-click", PartyMember.SelectionRingVisible);
        float before = PartyMember.GlobalPosition.DistanceTo(second);
        yield return 30;
        Expect("second right-click mid-walk redirects them",
            PartyMember.GlobalPosition.DistanceTo(second) < before - 10f);

        for (int i = 0; i < 900 && PartyMember.IsMoving; i++)
        {
            yield return 1;
        }

        Vector2 stopped = PartyMember.GlobalPosition;
        Expect($"arrives at the second point (off by {stopped.DistanceTo(second):0.0} px)",
            !PartyMember.IsMoving && stopped.DistanceTo(second) < ArriveTolerance);
        yield return 60;
        Expect("stays still after arriving (no jitter)", PartyMember.GlobalPosition.DistanceTo(stopped) < StillTolerance);
        Expect("the unselected party members never moved", others.All(other =>
            !other.Key.IsMoving && other.Key.GlobalPosition.DistanceTo(other.Value) < StillTolerance));

        Character last = PartyMembers[^1];
        Vector2 lastTarget = last.GlobalPosition + new Vector2(-64, 32);
        ClickPartyMember(MouseButton.Left, last);
        yield return 2;
        Click(MouseButton.Right, lastTarget);
        yield return 1;
        for (int i = 0; i < 300 && last.IsMoving; i++)
        {
            yield return 1;
        }

        Expect($"another party member, once selected, walks to the clicked point (off by {last.GlobalPosition.DistanceTo(lastTarget):0.0} px)",
            !last.IsMoving && last.GlobalPosition.DistanceTo(lastTarget) < ArriveTolerance);
        Expect("the first party member stays put once deselected", PartyMember.GlobalPosition.DistanceTo(stopped) < StillTolerance);
    }
}
