using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for right-click movement: loads Main, pushes real clicks through the viewport and checks
/// party members' positions, ending with a move order to the whole party. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 4000 --script res://tools/MoveCheck.cs
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

        // ADR 0003: the global combat speed scales movement as well as the attack cooldown.
        var rules = Root.GetNode<Rules>(Rules.Path);
        rules.CombatSpeed = 2f;
        from = PartyMember.GlobalPosition;
        yield return 30;
        walked = PartyMember.GlobalPosition.DistanceTo(from);
        rules.CombatSpeed = Core.Combat.CombatRules.NormalCombatSpeed;
        Expect($"walks one tile in half a second at double combat speed ({walked:0.0} px in 0.5 s)",
            Mathf.Abs(walked - Character.TileLength) < Character.TileLength * 0.05f);

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
        Expect($"arrives at the second point (off by {stopped.DistanceTo(second):0.0} px, limit {UnobstructedArriveTolerance} px)",
            !PartyMember.IsMoving && stopped.DistanceTo(second) <= UnobstructedArriveTolerance);
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

        Expect($"another party member, once selected, walks to the clicked point (off by "
               + $"{last.GlobalPosition.DistanceTo(lastTarget):0.0} px, limit {UnobstructedArriveTolerance} px)",
            !last.IsMoving && last.GlobalPosition.DistanceTo(lastTarget) <= UnobstructedArriveTolerance);
        Expect("the first party member stays put once deselected", PartyMember.GlobalPosition.DistanceTo(stopped) < StillTolerance);

        foreach (int step in OccupiedSlotMove())
        {
            yield return step;
        }

        foreach (int step in SpreadMove(start))
        {
            yield return step;
        }
    }

    /// <summary>
    /// Sends the first party member, alone, to where an idle party member stands: the move can't end on the spot, so the
    /// arrival rule finishes it once they are close and have stopped making progress, instead of jittering forever.
    /// </summary>
    private IEnumerable<int> OccupiedSlotMove()
    {
        Character mover = PartyMember;
        Character idle = PartyMembers[1];
        Vector2 occupied = idle.GlobalPosition;
        Vector2 moverStart = mover.GlobalPosition;
        Vector2 idleStart = idle.GlobalPosition;

        Expect("the arrival distance and no-progress timeout are exported", new[]
        {
            nameof(Character.ArrivalDistanceTiles), nameof(Character.NoProgressTimeoutSeconds),
        }.All(name => mover.GetPropertyList().Any(property =>
            (string)property["name"] == name
            && ((PropertyUsageFlags)(int)property["usage"]).HasFlag(PropertyUsageFlags.Editor))));

        ClickPartyMember(MouseButton.Left, mover);
        yield return 2;
        Click(MouseButton.Right, occupied);
        yield return 1;
        Expect("a move onto an idle party member starts", mover.IsMoving);

        // Walking time at full speed, plus the no-progress timeout, plus two seconds of margin for steering.
        float walk = mover.GlobalPosition.DistanceTo(occupied) / mover.SpeedPixelsPerSecond;
        int limit = Mathf.CeilToInt((walk + mover.NoProgressTimeoutSeconds + 2f) * 60);
        int frames = 0;
        int close = -1;
        for (; frames < limit && mover.IsMoving; frames++)
        {
            if (close < 0 && mover.GlobalPosition.DistanceTo(occupied) <= mover.ArrivalDistancePixels)
            {
                close = frames;
            }

            yield return 1;
        }

        // Once within the arrival distance, the move ends within the timeout plus a small margin for the last approach.
        int closeLimit = Mathf.CeilToInt((mover.NoProgressTimeoutSeconds + 0.75f) * 60);
        Vector2 stopped = mover.GlobalPosition;
        float off = stopped.DistanceTo(occupied);
        Expect($"a move onto an occupied spot finishes in time ({frames} of {limit} frames, "
            + $"{frames - close} of {closeLimit} once within the arrival distance)",
            !mover.IsMoving && close >= 0 && frames - close <= closeLimit);
        Expect($"and ends within the arrival distance (off by {off:0.0} px, limit {mover.ArrivalDistancePixels:0.0} px)",
            off <= mover.ArrivalDistancePixels + 1f);
        Expect("the idle party member was never pushed", idle.GlobalPosition.DistanceTo(idleStart) < StillTolerance);
        yield return 60;
        Expect("stays still after giving up (no jitter)",
            !mover.IsMoving && mover.GlobalPosition.DistanceTo(stopped) < StillTolerance && mover.Velocity == Vector2.Zero);

        // Walk back, so the two party members' capsules don't overlap for the clicks that follow.
        Click(MouseButton.Right, moverStart);
        yield return 1;
        for (int i = 0; i < limit && mover.IsMoving; i++)
        {
            yield return 1;
        }

        Expect("walks back off the occupied spot", !mover.IsMoving && mover.GlobalPosition.DistanceTo(moverStart) < ArriveTolerance);
    }

    /// <summary>Selects the whole party and right-clicks one point: everyone ends at their own spread slot.</summary>
    private IEnumerable<int> SpreadMove(Vector2 target)
    {
        Expect("navigation avoidance is on for every party member", PartyMembers.All(partyMember => partyMember.AvoidanceEnabled));
        Expect("party members' bodies don't collide with each other", PartyMembers.All(partyMember =>
            PartyMembers.All(other => (partyMember.CollisionMask & other.CollisionLayer) == 0)));

        foreach (int step in SelectWholeParty())
        {
            yield return step;
        }

        Click(MouseButton.Right, target);
        yield return 2;
        Expect("a right-click with the whole party selected sets every party member moving",
            PartyMembers.All(partyMember => partyMember.IsMoving));
        for (int i = 0; i < 1200 && PartyMembers.Any(partyMember => partyMember.IsMoving); i++)
        {
            yield return 1;
        }

        Expect("every party member arrives", PartyMembers.All(partyMember => !partyMember.IsMoving));
        Vector2[] ends = PartyMembers.Select(partyMember => partyMember.GlobalPosition).ToArray();
        float closest = ends.SelectMany((end, i) => ends.Skip(i + 1).Select(other => end.DistanceTo(other))).Min();
        float nearestToClick = ends.Min(end => end.DistanceTo(target));
        Expect($"the whole party ends with each party member at a distinct point (closest pair {closest:0.0} px apart)",
            closest > 2 * PartyMember.FeetRadius);
        Expect($"one party member ends on the clicked point (off by {nearestToClick:0.0} px)", nearestToClick < ArriveTolerance);
    }
}
