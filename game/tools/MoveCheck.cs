using System.Collections.Generic;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for right-click movement: loads Main, pushes real clicks through the viewport and checks
/// the party member's position. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 1200 --script res://tools/MoveCheck.cs
/// Prints "MoveCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class MoveCheck : SceneTree
{
    /// <summary>How close (px) counts as "arrived": the agent's target distance plus one physics step.</summary>
    private const float ArriveTolerance = 6f;

    private Character _partyMember = null!;
    private IEnumerator<int> _script = null!;
    private int _wait;
    private bool _failed;

    public override void _Initialize()
    {
        Root.AddChild(GD.Load<PackedScene>("res://Main.tscn").Instantiate());
        _script = Run();
        _wait = 5;   // let Main load the zone and the navigation map sync
    }

    public override bool _PhysicsProcess(double delta)
    {
        if (--_wait > 0)
        {
            return false;
        }

        if (_script.MoveNext())
        {
            _wait = _script.Current;
        }
        else
        {
            GD.Print(_failed ? "MoveCheck: FAILED" : "MoveCheck: PASSED");
            Quit(_failed ? 1 : 0);
        }

        return false;
    }

    /// <summary>The check, as steps. Each yield is the number of physics frames to wait before the next step.</summary>
    private IEnumerator<int> Run()
    {
        _partyMember = Root.GetNode<Character>("Main/ZoneContainer/TestZone/PartyMember1");
        Vector2 start = _partyMember.GlobalPosition;
        Vector2 first = start + new Vector2(200, 0);
        Vector2 second = start + new Vector2(-100, 150);

        Click(MouseButton.Right, first);
        yield return 30;
        Expect("right-click with nothing selected leaves them still",
            _partyMember.GlobalPosition.DistanceTo(start) < 0.01f && !_partyMember.IsMoving);

        Click(MouseButton.Left, start + new Vector2(0, -24));
        yield return 2;
        Expect("left-click selects them", _partyMember.SelectionRingVisible);

        Click(MouseButton.Right, first);
        yield return 1;
        Vector2 from = _partyMember.GlobalPosition;
        yield return 60;
        float walked = _partyMember.GlobalPosition.DistanceTo(from);
        Expect($"walks about one tile per second ({walked:0.0} px in 1 s, tile = {Character.TileLength:0.00} px)",
            Mathf.Abs(walked - Character.TileLength) < Character.TileLength * 0.05f);
        Expect("walks towards the clicked point", _partyMember.GlobalPosition.DistanceTo(first) < from.DistanceTo(first));

        Click(MouseButton.Right, second);
        yield return 2;
        Expect("ring stays after right-click", _partyMember.SelectionRingVisible);
        float before = _partyMember.GlobalPosition.DistanceTo(second);
        yield return 30;
        Expect("second right-click mid-walk redirects them",
            _partyMember.GlobalPosition.DistanceTo(second) < before - 10f);

        for (int i = 0; i < 900 && _partyMember.IsMoving; i++)
        {
            yield return 1;
        }

        Vector2 stopped = _partyMember.GlobalPosition;
        Expect($"arrives at the second point (off by {stopped.DistanceTo(second):0.0} px)",
            !_partyMember.IsMoving && stopped.DistanceTo(second) < ArriveTolerance);
        yield return 60;
        Expect("stays still after arriving (no jitter)", _partyMember.GlobalPosition.DistanceTo(stopped) < 0.01f);
    }

    private void Click(MouseButton button, Vector2 world)
    {
        Vector2 screen = Root.GetCanvasTransform() * world;
        Root.PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = screen }, true);
        Root.PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = screen }, true);
    }

    private void Expect(string name, bool ok)
    {
        if (ok)
        {
            GD.Print($"ok: {name}");
        }
        else
        {
            _failed = true;
            GD.PushError($"MoveCheck: {name}: failed at {_partyMember.GlobalPosition}");
        }
    }
}
