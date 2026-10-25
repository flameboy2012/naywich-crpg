using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for move markers: loads Main, pushes real clicks through the viewport and checks the markers each
/// right-click shows: one per order, at each party member's navigation path end, in their tint, gone after the fade,
/// and replaced by the next right-click. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 1200 --script res://tools/MarkerCheck.cs
/// Prints "MarkerCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class MarkerCheck : HeadlessCheck
{
    /// <summary>The middle tile of an obstacle block, unreachable (as in ObstacleCheck).</summary>
    private static readonly Vector2I InsideObstacleTile = new(17, 5);

    /// <summary>Distance (px) under which a marker counts as sitting on a point.</summary>
    private const float OnPointTolerance = 0.5f;

    /// <summary>Frames after which every marker must be gone: the fade time plus a few frames.</summary>
    private static int FadeFrames => Mathf.CeilToInt(MoveMarker.FadeSeconds * 60) + 5;

    private List<MoveMarker> Markers => GetNodesInGroup(MoveMarker.Group).OfType<MoveMarker>().ToList();

    protected override IEnumerator<int> Run()
    {
        foreach (int step in WaitForNavigationSync())
        {
            yield return step;
        }

        TileMapLayer ground = Ground;
        Rid map = NavigationMap;
        Vector2 target = PartyMember.GlobalPosition + new Vector2(150, -60);
        Click(MouseButton.Right, target);
        yield return 2;
        Expect("right-click with nothing selected shows no marker", Markers.Count == 0);

        // A single party member sent into an obstacle: the marker shows where they will really stop.
        Vector2 inside = ground.ToGlobal(ground.MapToLocal(InsideObstacleTile));
        Vector2 reachable = NavigationServer2D.MapGetClosestPoint(map, inside);
        ClickPartyMember(MouseButton.Left);
        yield return 2;
        Click(MouseButton.Right, inside);
        yield return 2;
        List<MoveMarker> markers = Markers;
        Expect($"right-click with one selected shows one marker ({markers.Count})", markers.Count == 1);
        Expect($"a click inside an obstacle puts the marker at the closest reachable point, not the click "
               + $"({(markers.Count > 0 ? markers[0].GlobalPosition : Vector2.Inf)} vs {reachable})",
            markers.Count == 1 && markers[0].GlobalPosition.DistanceTo(reachable) < OnPointTolerance
                               && inside.DistanceTo(reachable) > OnPointTolerance);
        Expect("the marker uses the party member's tint", markers.Count == 1 && markers[0].Tint == PartyMember.Tint);

        yield return 20;
        markers = Markers;
        Expect($"the marker fades ({(markers.Count > 0 ? markers[0].Opacity : -1):0.00})",
            markers.Count == 1 && markers[0].Opacity is > 0f and < 1f);
        yield return FadeFrames;
        Expect("the marker is gone after the fade time", Markers.Count == 0);

        // The whole party: one marker per party member, at their own path end and in their own tint.
        foreach (int step in SelectWholeParty())
        {
            yield return step;
        }

        Click(MouseButton.Right, target);
        yield return 2;
        ExpectMarkerPerPartyMember("first right-click with the whole party selected");

        yield return 10;
        Vector2 second = target + new Vector2(-120, 90);
        Click(MouseButton.Right, second);
        yield return 2;
        ExpectMarkerPerPartyMember("second right-click replaces the first click's markers");

        yield return FadeFrames;
        Expect("every marker is gone after the fade time", Markers.Count == 0);
    }

    /// <summary>Exactly one marker per party member, each at that party member's path end and in their tint.</summary>
    private void ExpectMarkerPerPartyMember(string name)
    {
        List<MoveMarker> markers = Markers;
        Expect($"{name}: one marker per selected party member ({markers.Count})", markers.Count == PartyMembers.Count);
        foreach (Character partyMember in PartyMembers)
        {
            Expect($"{name}: {partyMember.Name} has a marker in their tint at their path end ({partyMember.PathEnd})",
                markers.Count(marker => marker.Tint == partyMember.Tint
                                        && marker.GlobalPosition.DistanceTo(partyMember.PathEnd) < OnPointTolerance) == 1);
        }
    }
}
