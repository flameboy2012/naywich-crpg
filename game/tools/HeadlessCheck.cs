using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Shared harness for the headless engine checks (SelectionCheck, MoveCheck, ObstacleCheck, MarkerCheck). Loads Main, waits for
/// the zone and navigation map to settle, then runs the check's steps, pushing real clicks through the viewport.
/// Prints "&lt;Check&gt;: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public abstract partial class HeadlessCheck : SceneTree
{
    /// <summary>How close (px) counts as "arrived": the agent's target distance plus one physics step.</summary>
    protected const float ArriveTolerance = 6f;

    /// <summary>Distance (px) under which a party member counts as not having moved.</summary>
    protected const float StillTolerance = 0.01f;

    /// <summary>Physics frames to wait after loading Main, so the zone is loaded and the navigation map synced.</summary>
    private const int SettleFrames = 5;

    private const string ZonePath = "Main/ZoneContainer/TestZone";

    /// <summary>Where to click, relative to the party member's feet, to hit them: the middle of the capsule sprite.</summary>
    private static readonly Vector2 PartyMemberClickOffset = new(0, -24);

    private IEnumerator<int> _steps = null!;
    private int _wait;
    private bool _failed;

    /// <summary>Every party member in the zone, in spawn order (PartyMember1 first). Set before the first step runs.</summary>
    protected IReadOnlyList<Character> PartyMembers { get; private set; } = null!;

    /// <summary>The first party member: the one single-character checks drive.</summary>
    protected Character PartyMember => PartyMembers[0];

    public override void _Initialize()
    {
        Root.AddChild(GD.Load<PackedScene>("res://Main.tscn").Instantiate());
        _steps = Steps();
        _wait = SettleFrames;
    }

    public override bool _PhysicsProcess(double delta)
    {
        EachPhysicsFrame();
        if (--_wait > 0)
        {
            return false;
        }

        if (_steps.MoveNext())
        {
            _wait = _steps.Current;
        }
        else
        {
            string name = GetType().Name;
            GD.Print(_failed ? $"{name}: FAILED" : $"{name}: PASSED");
            Quit(_failed ? 1 : 0);
        }

        return false;
    }

    /// <summary>The check, as steps. Each yield is the number of physics frames to wait before the next step.</summary>
    protected abstract IEnumerator<int> Run();

    /// <summary>Called on every physics frame, before the step runner; for checks that track something throughout.</summary>
    protected virtual void EachPhysicsFrame()
    {
    }

    /// <summary>Presses and releases a mouse button at a world point, through the viewport, optionally with shift held.</summary>
    protected void Click(MouseButton button, Vector2 world, bool shift = false)
    {
        Vector2 screen = Root.GetCanvasTransform() * world;
        Root.PushInput(
            new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = screen, ShiftPressed = shift },
            true);
        Root.PushInput(
            new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = screen, ShiftPressed = shift },
            true);
    }

    /// <summary>Presses the left button at a world point and moves the mouse to another, without releasing.</summary>
    protected void BeginDrag(Vector2 fromWorld, Vector2 toWorld, bool shift = false)
    {
        Vector2 from = Root.GetCanvasTransform() * fromWorld;
        Vector2 to = Root.GetCanvasTransform() * toWorld;
        Root.PushInput(
            new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left, Pressed = true, Position = from, ShiftPressed = shift,
                ButtonMask = MouseButtonMask.Left,
            },
            true);
        Root.PushInput(
            new InputEventMouseMotion
            {
                Position = to, Relative = to - from, ShiftPressed = shift, ButtonMask = MouseButtonMask.Left,
            },
            true);
    }

    /// <summary>Releases the left button at a world point, ending a drag begun with <see cref="BeginDrag"/>.</summary>
    protected void EndDrag(Vector2 atWorld, bool shift = false)
    {
        Vector2 at = Root.GetCanvasTransform() * atWorld;
        Root.PushInput(
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at, ShiftPressed = shift },
            true);
    }

    /// <summary>Presses the left button at one world point, drags to another and releases there.</summary>
    protected void Drag(Vector2 fromWorld, Vector2 toWorld, bool shift = false)
    {
        BeginDrag(fromWorld, toWorld, shift);
        EndDrag(toWorld, shift);
    }

    /// <summary>Clicks on the first party member's capsule.</summary>
    protected void ClickPartyMember(MouseButton button) => ClickPartyMember(button, PartyMember);

    /// <summary>Clicks on the given party member's capsule, optionally with shift held.</summary>
    protected void ClickPartyMember(MouseButton button, Character partyMember, bool shift = false) =>
        Click(button, partyMember.GlobalPosition + PartyMemberClickOffset, shift);

    protected void Expect(string name, bool ok)
    {
        if (ok)
        {
            GD.Print($"ok: {name}");
        }
        else
        {
            _failed = true;
            GD.PushError($"{GetType().Name}: {name}: failed at {PartyMember.GlobalPosition}");
        }
    }

    private IEnumerator<int> Steps()
    {
        Node zone = Root.GetNode(ZonePath);
        PartyMembers = GetNodesInGroup(PlayerControl.PartyMembersGroup).OfType<PlayerControl>()
            .Select(control => control.GetParent<Character>())
            .Where(character => character.GetParent() == zone)
            .ToList();
        IEnumerator<int> run = Run();
        while (run.MoveNext())
        {
            yield return run.Current;
        }
    }
}
