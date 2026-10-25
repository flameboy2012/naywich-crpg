using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for pause: loads Main, presses Space through the input singleton and checks that the pause banner
/// shows only while paused, that a moving party member stops, and that while paused selection, keyboard pan and move
/// orders still work: a move order given while paused shows its path line and move marker at once, and the party
/// member starts walking on resume.
/// Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 900 --script res://tools/PauseCheck.cs
/// Prints "PauseCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class PauseCheck : HeadlessCheck
{
    private PauseManager PauseManager => Root.GetNode<PauseManager>("PauseManager");

    private Control Banner => Root.GetNode<Control>("Main/UI/PauseBanner");

    private Node2D CameraRig => Root.GetNode<Node2D>("Main/CameraRig");

    protected override IEnumerator<int> Run()
    {
        foreach (int step in WaitForNavigationSync())
        {
            yield return step;
        }

        Expect("the game starts unpaused", !Paused && !PauseManager.IsPaused);
        Expect("the pause banner is hidden at start", !Banner.Visible);

        // The first party member, walking.
        ClickPartyMember(MouseButton.Left);
        yield return 2;
        Click(MouseButton.Right, PartyMember.GlobalPosition + new Vector2(200, -80));
        yield return 20;
        Expect("the first party member is walking before the pause", PartyMember.IsMoving);

        foreach (int step in PressKey(Key.Space))
        {
            yield return step;
        }

        Expect("Space pauses the scene tree", Paused && PauseManager.IsPaused);
        Expect("the pause banner shows while paused", Banner.Visible && Banner.IsVisibleInTree());

        Vector2 stoppedAt = PartyMember.GlobalPosition;
        yield return 30;
        Expect($"a moving party member stays still while paused ({stoppedAt} then {PartyMember.GlobalPosition})",
            PartyMember.GlobalPosition.DistanceTo(stoppedAt) < StillTolerance);
        Expect("a party member stopped by the pause still has their order", PartyMember.IsMoving);

        // Selection while paused: a click on the second party member selects only them.
        Character second = PartyMembers[1];
        ClickPartyMember(MouseButton.Left, second);
        yield return 2;
        Expect("a click selects a party member while paused",
            second.SelectionRingVisible && !PartyMember.SelectionRingVisible);

        // A move order while paused shows its path line and move marker at once, but nobody walks.
        Vector2 secondStart = second.GlobalPosition;
        Click(MouseButton.Right, secondStart + new Vector2(-180, 60));
        yield return 2;
        Expect("a move order given while paused is taken", second.IsMoving);
        PathLine? line = GetNodesInGroup(PathLine.Group).OfType<PathLine>()
            .FirstOrDefault(pathLine => pathLine.Source.GetParent() == second && pathLine.Visible);
        Expect("a move order given while paused shows a path line at once", line is { Points.Length: >= 2 });
        if (line is { Points.Length: >= 2 })
        {
            Vector2 lineEnd = line.ToGlobal(line.Points[^1]);
            Expect($"the paused order's path line ends at the path end ({lineEnd} vs {second.PathEnd})",
                lineEnd.DistanceTo(second.PathEnd) < 1.5f && second.PathEnd.DistanceTo(secondStart) > 50f);
        }

        List<MoveMarker> markers = GetNodesInGroup(MoveMarker.Group).OfType<MoveMarker>().ToList();
        Expect("a move order given while paused shows a move marker at its path end",
            markers.Count == 1 && markers[0].GlobalPosition.DistanceTo(second.PathEnd) < 1.5f);
        yield return 20;
        Expect("the move marker fades while paused",
            markers.Count == 1 && IsInstanceValid(markers[0]) && markers[0].Opacity < 0.9f);
        Expect("a party member given a move order while paused stays still",
            second.GlobalPosition.DistanceTo(secondStart) < StillTolerance);

        // Keyboard pan while paused.
        Vector2 cameraBefore = CameraRig.GlobalPosition;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Keycode = Key.D, Pressed = true });
        yield return 10;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Keycode = Key.D, Pressed = false });
        yield return 3;
        Expect($"keyboard pan moves the camera while paused ({cameraBefore} then {CameraRig.GlobalPosition})",
            CameraRig.GlobalPosition.X > cameraBefore.X + 10f);

        // Space again resumes: both orders carry on.
        foreach (int step in PressKey(Key.Space))
        {
            yield return step;
        }

        Expect("Space again resumes the scene tree", !Paused && !PauseManager.IsPaused);
        Expect("the pause banner hides on resume", !Banner.Visible);
        Vector2 firstResumedAt = PartyMember.GlobalPosition;
        yield return 30;
        Expect("the first party member walks on after resume",
            PartyMember.GlobalPosition.DistanceTo(firstResumedAt) > 10f);
        Expect("the move order given while paused starts on resume",
            second.GlobalPosition.DistanceTo(secondStart) > 10f);
    }

    /// <summary>Presses and releases a key through the input singleton, waiting for the release to land.</summary>
    private static IEnumerable<int> PressKey(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
        yield return 1;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
        yield return 3;
    }
}
