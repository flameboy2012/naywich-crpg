using System;
using System.Collections.Generic;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for selection: loads Main, pushes real left-clicks through the viewport and checks
/// the selection ring. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 300 --script res://tools/SelectionCheck.cs
/// Prints "SelectionCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class SelectionCheck : SceneTree
{
    private readonly List<(string Name, Func<Vector2> ClickAt, bool RingExpected)> _steps = new();
    private Character _partyMember = null!;
    private int _frame;
    private int _step;
    private bool _failed;

    public override void _Initialize()
    {
        Root.AddChild(GD.Load<PackedScene>("res://Main.tscn").Instantiate());
        Func<Vector2> onPartyMember = () => _partyMember.GlobalPosition + new Vector2(0, -24);
        Func<Vector2> onGround = () => _partyMember.GlobalPosition + new Vector2(200, 60);
        _steps.Add(("left-click party member shows ring", onPartyMember, true));
        _steps.Add(("left-click selected party member keeps ring", onPartyMember, true));
        _steps.Add(("left-click ground hides ring", onGround, false));
        _steps.Add(("left-click party member again shows ring", onPartyMember, true));
    }

    public override bool _PhysicsProcess(double delta)
    {
        _frame++;
        if (_frame == 5)
        {
            _partyMember = Root.GetNode<Character>("Main/ZoneContainer/TestZone/PartyMember1");
            Expect("ring hidden at start", false);
        }

        // From frame 6: on even frames click, on odd frames check the step just clicked.
        if (_frame >= 6 && _step < _steps.Count)
        {
            var step = _steps[_step];
            if (_frame % 2 == 0)
            {
                LeftClick(step.ClickAt());
            }
            else
            {
                Expect(step.Name, step.RingExpected);
                _step++;
            }
        }
        else if (_step == _steps.Count)
        {
            GD.Print(_failed ? "SelectionCheck: FAILED" : "SelectionCheck: PASSED");
            Quit(_failed ? 1 : 0);
        }

        return false;
    }

    private void LeftClick(Vector2 world)
    {
        Vector2 screen = Root.GetCanvasTransform() * world;
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = screen }, true);
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = screen }, true);
    }

    private void Expect(string name, bool ringVisible)
    {
        if (_partyMember.SelectionRingVisible == ringVisible)
        {
            GD.Print($"ok: {name}");
        }
        else
        {
            _failed = true;
            GD.PushError($"SelectionCheck: {name}: ring visible = {_partyMember.SelectionRingVisible}, expected {ringVisible}");
        }
    }
}
