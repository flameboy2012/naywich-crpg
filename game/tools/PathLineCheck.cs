using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for path lines: loads Main, pushes real clicks through the viewport and checks that each selected,
/// moving party member shows a line in their tint from where they stand along the rest of their path, that it shrinks
/// as they walk, hides on deselection and arrival, and that a moving party member who isn't selected shows none.
/// Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 1500 --script res://tools/PathLineCheck.cs
/// Prints "PathLineCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class PathLineCheck : HeadlessCheck
{
    /// <summary>
    /// Distance (px) under which a line point counts as sitting on a party member's feet. Lines update once per frame,
    /// so they may trail the body by one frame of walking (about 0.6 px).
    /// </summary>
    private const float OnPointTolerance = 1.5f;

    private List<PathLine> Lines => GetNodesInGroup(PathLine.Group).OfType<PathLine>().ToList();

    protected override IEnumerator<int> Run()
    {
        var ground = Root.GetNode<TileMapLayer>("Main/ZoneContainer/TestZone/NavigationRegion2D/Ground");
        Rid map = ground.GetParent<NavigationRegion2D>().GetNavigationMap();
        Vector2 groundPoint = PartyMember.GlobalPosition;
        for (int i = 0; i < 120 && NavigationServer2D.MapGetClosestPoint(map, groundPoint).DistanceTo(groundPoint) > 0.5f; i++)
        {
            yield return 1;   // map queries return (0, 0) until the map has synced with the zone's region
        }

        Expect("no party member shows a path line before any move", PartyMembers.All(member => LineOf(member) is null));

        // The whole party, sent a few tiles away.
        ClickPartyMember(MouseButton.Left, PartyMembers[0]);
        foreach (Character member in PartyMembers.Skip(1))
        {
            yield return 2;
            ClickPartyMember(MouseButton.Left, member, shift: true);
        }

        yield return 2;
        Expect("the whole party is selected", PartyMembers.All(member => member.SelectionRingVisible));
        Vector2 target = PartyMember.GlobalPosition + new Vector2(150, -60);
        Click(MouseButton.Right, target);
        yield return 3;
        Expect("a group right-click sets every party member moving", PartyMembers.All(member => member.IsMoving));
        foreach (Character member in PartyMembers)
        {
            ExpectLine(member, "after a group right-click");
        }

        Dictionary<Character, float> lengths = PartyMembers.ToDictionary(member => member, member => Length(LineOf(member)));
        yield return 30;
        foreach (Character member in PartyMembers)
        {
            float now = Length(LineOf(member));
            Expect($"{member.Name}'s line shrinks as they walk ({lengths[member]:0.0} px, then {now:0.0} px)",
                now < lengths[member] - 5f);
        }

        // Deselecting a moving party member hides their line; reselecting them while they still move shows it again.
        Character second = PartyMembers[1];
        ClickPartyMember(MouseButton.Left, second, shift: true);
        yield return 2;
        Expect("shift-click deselects a moving party member", !second.SelectionRingVisible && second.IsMoving);
        Expect("a deselected moving party member shows no line", LineOf(second) is null);
        Expect("the others still show their lines", PartyMembers.Except(new[] { second }).All(member => LineOf(member) is not null));
        ClickPartyMember(MouseButton.Left, second, shift: true);
        yield return 2;
        Expect("shift-click reselects them while they still move", second.SelectionRingVisible && second.IsMoving);
        ExpectLine(second, "after reselecting");

        // A plain click on one party member leaves the rest moving but unselected: they show no line.
        ClickPartyMember(MouseButton.Left, PartyMember);
        yield return 2;
        Expect("the others keep walking once deselected", PartyMembers.Skip(1).All(member => member.IsMoving));
        Expect("moving party members who aren't selected show no line", PartyMembers.Skip(1).All(member => LineOf(member) is null));
        ExpectLine(PartyMember, "while still selected");

        // Arrival (by reaching the destination or by the no-progress rule) hides the line.
        for (int i = 0; i < 900 && PartyMembers.Any(member => member.IsMoving); i++)
        {
            yield return 1;
        }

        yield return 2;
        Expect("every party member arrives", PartyMembers.All(member => !member.IsMoving));
        Expect("the selected party member still has their ring", PartyMember.SelectionRingVisible);
        Expect("the line hides on arrival", LineOf(PartyMember) is null);

        // Selecting idle party members shows no lines.
        foreach (Character member in PartyMembers.Skip(1))
        {
            ClickPartyMember(MouseButton.Left, member, shift: true);
            yield return 2;
        }

        Expect("selected idle party members show no line", PartyMembers.All(member => LineOf(member) is null));
    }

    /// <summary>The party member's visible path line, if any.</summary>
    private PathLine? LineOf(Character member)
    {
        List<PathLine> lines = Lines.Where(line => line.PartyMember == member && line.Visible).ToList();
        if (lines.Count > 1)
        {
            Expect($"{member.Name} has at most one visible path line ({lines.Count})", false);
        }

        return lines.FirstOrDefault();
    }

    /// <summary>One visible line, in the party member's tint, from their feet to the end of their path.</summary>
    private void ExpectLine(Character member, string when)
    {
        PathLine? line = LineOf(member);
        Expect($"{when}: {member.Name} shows a path line", line is not null);
        if (line is null)
        {
            return;
        }

        Vector2[] points = line.Points.Select(point => line.ToGlobal(point)).ToArray();
        Expect($"{when}: {member.Name}'s line is in their tint", line.DefaultColor == member.Tint);
        Expect($"{when}: {member.Name}'s line starts at their feet ({(points.Length > 0 ? points[0] : Vector2.Inf)} vs "
               + $"{member.GlobalPosition})",
            points.Length >= 2 && points[0].DistanceTo(member.GlobalPosition) < OnPointTolerance);
        Expect($"{when}: {member.Name}'s line ends at their path end",
            points.Length >= 2 && points[^1].DistanceTo(member.PathEnd) < OnPointTolerance);
    }

    private static float Length(PathLine? line)
    {
        if (line is null)
        {
            return 0f;
        }

        Vector2[] points = line.Points;
        return points.Zip(points.Skip(1), (a, b) => a.DistanceTo(b)).Sum();
    }
}
