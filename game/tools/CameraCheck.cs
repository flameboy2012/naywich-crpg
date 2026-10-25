using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for the camera: loads Main and checks where the camera starts and what it shows, then pans it with
/// held keys and a middle-mouse drag, and checks Ctrl+A and a box select don't pan. Edge pan needs a focused window
/// with the mouse inside, which a headless run never has, so the play-test covers it. Run from game/ after dotnet
/// build:
///   godot --headless --path . --fixed-fps 60 --quit-after 600 --script res://tools/CameraCheck.cs
/// Prints "CameraCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class CameraCheck : HeadlessCheck
{
    /// <summary>The zone bounds margin outside the ground tiles: one tile's screen size, split between the sides.</summary>
    private static readonly Vector2 ZoneBoundsMargin = new(64, 32);

    /// <summary>How close (px) counts as the same camera position.</summary>
    private const float PositionTolerance = 0.5f;

    /// <summary>Frames to hold a pan key for a long pan: 2 s at about 1000 px/s, further than the zone bounds allow.</summary>
    private const int LongPanFrames = 120;

    /// <summary>Frames to hold a pan key for a short pan, well inside the zone bounds.</summary>
    private const int ShortPanFrames = 20;

    /// <summary>How far (screen px) the middle-mouse drag moves the mouse: up and to the left.</summary>
    private static readonly Vector2 MiddleDrag = new(200, 100);

    /// <summary>Frames to wait after releasing a key, for the release to reach the camera rig.</summary>
    private const int SettleFrames = 2;

    protected override IEnumerator<int> Run()
    {
        Camera2D camera = Root.GetNode<Camera2D>("Main/CameraRig/Camera2D");
        Node2D rig = camera.GetParent<Node2D>();
        Rect2 view = VisibleWorldRect(camera);
        Rect2 bounds = GroundRect().GrowIndividual(
            ZoneBoundsMargin.X, ZoneBoundsMargin.Y, ZoneBoundsMargin.X, ZoneBoundsMargin.Y);
        Vector2 partyCentre = PartyMembers.Aggregate(Vector2.Zero, (sum, partyMember) => sum + partyMember.GlobalPosition)
                              / PartyMembers.Count;
        GD.Print($"view {view}, zone bounds {bounds}, party centre {partyCentre}");

        Expect("the zone is 48x48 tiles", Ground.GetUsedRect() == new Rect2I(0, 0, 48, 48));
        Expect("the zone bounds are larger than the view on both axes",
            bounds.Size.X > view.Size.X && bounds.Size.Y > view.Size.Y);
        Expect("the view starts inside the zone bounds", bounds.Encloses(view));
        Expect("the camera starts centred on the party horizontally",
            Mathf.Abs(view.GetCenter().X - partyCentre.X) < PositionTolerance);

        // The party starts near the top of the zone, closer to it than half a view, so the clamp pulls the view down
        // until its top touches the zone bounds.
        Expect("the party starts closer to the top of the zone bounds than half a view",
            partyCentre.Y - bounds.Position.Y < view.Size.Y / 2);
        Expect("the camera start is clamped: the view's top touches the zone bounds",
            Mathf.Abs(view.Position.Y - bounds.Position.Y) < PositionTolerance);

        Expect("the area outside the zone is black",
            ProjectSettings.GetSetting("rendering/environment/defaults/default_clear_color").AsColor() == Colors.Black);

        // A long keyboard pan right, then left with WASD, stops with the view touching the zone bounds.
        HoldKey(Key.Right, true);
        yield return LongPanFrames;
        HoldKey(Key.Right, false);
        view = VisibleWorldRect(camera);
        Expect("a long arrow key pan right stops at the zone bounds",
            Mathf.Abs(view.End.X - bounds.End.X) < PositionTolerance && bounds.Encloses(view));

        HoldKey(Key.A, true);
        yield return LongPanFrames;
        HoldKey(Key.A, false);
        view = VisibleWorldRect(camera);
        Expect("a long WASD pan left stops at the zone bounds",
            Mathf.Abs(view.Position.X - bounds.Position.X) < PositionTolerance && bounds.Encloses(view));

        // Two held directions pan diagonally, the same distance on both axes.
        Vector2 before = rig.GlobalPosition;
        HoldKey(Key.D, true);
        HoldKey(Key.S, true);
        yield return ShortPanFrames;
        HoldKey(Key.D, false);
        HoldKey(Key.S, false);
        yield return SettleFrames;
        Vector2 diagonal = rig.GlobalPosition - before;
        GD.Print($"diagonal pan {diagonal}");
        Expect("D+S pans right and down by the same distance",
            diagonal.X > 0 && Mathf.Abs(diagonal.X - diagonal.Y) < PositionTolerance);

        // Middle-mouse drag moves the camera by the drag distance, so the ground stays under the cursor.
        Vector2 dragFrom = new(1200, 700);
        Vector2 dragTo = dragFrom - MiddleDrag;
        Vector2 groundUnderCursor = ScreenToWorld(dragFrom);
        before = rig.GlobalPosition;
        MiddleDragOnScreen(dragFrom, dragTo);
        yield return 2;
        Vector2 dragged = rig.GlobalPosition - before;
        GD.Print($"middle-mouse drag moved the camera {dragged}");
        Expect("middle-mouse drag moves the camera by the drag distance",
            dragged.DistanceTo(MiddleDrag) < PositionTolerance);
        Expect("the ground stays under the cursor",
            ScreenToWorld(dragTo).DistanceTo(groundUnderCursor) < PositionTolerance);

        // Holding Ctrl+A selects the whole party and doesn't pan left, though A alone would.
        before = rig.GlobalPosition;
        HoldKey(Key.Ctrl, true);
        HoldKey(Key.A, true, ctrl: true);
        yield return ShortPanFrames;
        HoldKey(Key.A, false, ctrl: true);
        HoldKey(Key.Ctrl, false);
        yield return SettleFrames;
        GD.Print($"holding Ctrl+A moved the camera {rig.GlobalPosition - before}");
        Expect("Ctrl+A selects every party member",
            PartyMembers.All(partyMember => partyMember.SelectionRingVisible));
        Expect("holding Ctrl+A doesn't pan the camera",
            rig.GlobalPosition.DistanceTo(before) < PositionTolerance);

        // No panning happens while a box select is being dragged.
        Vector2 boxFrom = ScreenToWorld(new Vector2(900, 500));
        BeginDrag(boxFrom, boxFrom + new Vector2(120, 80));
        yield return 1;
        before = rig.GlobalPosition;
        HoldKey(Key.Left, true);
        yield return ShortPanFrames;
        Root.PushInput(
            new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Middle, Pressed = true, Position = new Vector2(1000, 600),
                ButtonMask = MouseButtonMask.Left | MouseButtonMask.Middle,
            },
            true);
        Root.PushInput(
            new InputEventMouseMotion
            {
                Position = new Vector2(900, 500), Relative = new Vector2(-100, -100),
                ButtonMask = MouseButtonMask.Left | MouseButtonMask.Middle,
            },
            true);
        yield return 1;
        Expect("no keyboard or middle-mouse pan while a box select is being dragged",
            rig.GlobalPosition.DistanceTo(before) < PositionTolerance);
        Root.PushInput(
            new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Middle, Pressed = false, Position = new Vector2(900, 500),
                ButtonMask = MouseButtonMask.Left,
            },
            true);
        HoldKey(Key.Left, false);
        EndDrag(boxFrom + new Vector2(120, 80));
        yield return 1;
    }

    /// <summary>
    /// Presses or releases a key through Input, so held-key state updates as for a real key, optionally with Ctrl
    /// held.
    /// </summary>
    private static void HoldKey(Key key, bool pressed, bool ctrl = false) =>
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = key, PhysicalKeycode = key, Pressed = pressed, CtrlPressed = ctrl,
        });

    /// <summary>Presses the middle button at one screen point, moves the mouse to another and releases there.</summary>
    private void MiddleDragOnScreen(Vector2 from, Vector2 to)
    {
        Root.PushInput(
            new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Middle, Pressed = true, Position = from, ButtonMask = MouseButtonMask.Middle,
            },
            true);
        Root.PushInput(
            new InputEventMouseMotion { Position = to, Relative = to - from, ButtonMask = MouseButtonMask.Middle },
            true);
        Root.PushInput(
            new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = false, Position = to }, true);
    }

    /// <summary>The world point under a screen point.</summary>
    private Vector2 ScreenToWorld(Vector2 screen) => Root.GetCanvasTransform().AffineInverse() * screen;

    /// <summary>The world rectangle the camera shows: the viewport's visible rect seen through the canvas transform.</summary>
    private Rect2 VisibleWorldRect(Camera2D camera)
    {
        Rect2 screen = camera.GetViewport().GetVisibleRect();
        return Root.GetCanvasTransform().AffineInverse() * screen;
    }

    /// <summary>The bounding rectangle of the ground tiles' diamonds, in world pixels.</summary>
    private Rect2 GroundRect()
    {
        Rect2I used = Ground.GetUsedRect();
        Vector2 halfTile = Ground.TileSet.TileSize / 2;
        var rect = new Rect2(Ground.ToGlobal(Ground.MapToLocal(used.Position)), Vector2.Zero);
        foreach (Vector2I cell in Ground.GetUsedCells())
        {
            Vector2 centre = Ground.ToGlobal(Ground.MapToLocal(cell));
            rect = rect.Expand(centre - halfTile).Expand(centre + halfTile);
        }

        return rect;
    }
}
