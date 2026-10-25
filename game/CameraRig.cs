using Crpg.Core.Camera;
using Godot;

namespace Crpg;

/// <summary>
/// Owns the camera and all panning: keyboard pan (WASD and the arrows), edge pan and middle-mouse drag, all off while a
/// box select is being dragged. Every camera move goes through <see cref="CentreOn"/>, which keeps the view inside the
/// zone bounds with the Core camera clamp. The Camera2D child sits at the rig's origin; moving the rig moves the view.
/// </summary>
public partial class CameraRig : Node2D
{
    /// <summary>
    /// How far (px) the zone bounds reach past the ground tiles, left and right (X) and top and bottom (Y). Starts at
    /// one tile's screen size, 64x32, split as 64 px a side and 32 px top and bottom. Provisional until the play-test.
    /// </summary>
    [Export]
    public Vector2 ZoneBoundsMargin { get; set; } = new(64, 32);

    /// <summary>How fast (px/s) keyboard pan and edge pan move the camera. Provisional until the play-test.</summary>
    [Export]
    public float PanSpeed { get; set; } = 1000f;

    /// <summary>How close (px) to a window edge the mouse must be to edge pan. Provisional until the play-test.</summary>
    [Export]
    public float EdgeWidth { get; set; } = 8f;

    /// <summary>The selection controller, asked whether a box select is being dragged: all panning is off then.</summary>
    [Export]
    public SelectionController Selection { get; set; } = null!;

    private ZoneBounds _bounds;

    /// <summary>
    /// True while the real mouse is inside the window. Set from the operating system's window notifications, not the
    /// Window's mouse signals, which input pushed through the viewport also fires.
    /// </summary>
    private bool _mouseInside;

    /// <summary>
    /// A headless run has no real mouse, yet reports a focused window with the mouse inside it at (0, 0), which would
    /// edge pan towards the top-left corner and move the camera under the headless checks.
    /// </summary>
    private static readonly bool Headless = DisplayServer.GetName() == "headless";

    /// <summary>True while the middle button is held for a middle-mouse drag.</summary>
    private bool _middleDragging;

    public override void _Notification(int what)
    {
        if (what == NotificationWMMouseEnter)
        {
            _mouseInside = true;
        }
        else if (what == NotificationWMMouseExit)
        {
            _mouseInside = false;
        }
    }

    /// <summary>Keyboard pan and edge pan, combined, at the pan speed.</summary>
    public override void _Process(double delta)
    {
        if (Selection.BoxVisible)
        {
            return;
        }

        PanDirection direction = PanRule.Combine(KeyboardDirection(), EdgeDirection());
        if (direction != PanDirection.None)
        {
            CentreOn(GlobalPosition + new Vector2(direction.X, direction.Y) * PanSpeed * (float)delta);
        }
    }

    /// <summary>Middle-mouse drag moves the camera against the mouse, so the ground stays under the cursor.</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } middle:
                _middleDragging = middle.Pressed;
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseMotion motion when _middleDragging && !Selection.BoxVisible:
                // There is no zoom, so a screen pixel is a world pixel.
                CentreOn(GlobalPosition - motion.Relative);
                break;
        }
    }

    /// <summary>Sets the zone bounds from a zone's ground layer: its tiles' bounding rectangle, grown by the margin.</summary>
    public void SetZone(TileMapLayer ground)
    {
        Rect2 rect = GroundRect(ground).GrowIndividual(
            ZoneBoundsMargin.X, ZoneBoundsMargin.Y, ZoneBoundsMargin.X, ZoneBoundsMargin.Y);
        _bounds = new ZoneBounds(rect.Position.X, rect.Position.Y, rect.End.X, rect.End.Y);
    }

    /// <summary>Centres the camera on a world point, as near as the zone bounds allow.</summary>
    public void CentreOn(Vector2 point)
    {
        Vector2 viewSize = GetViewport().GetVisibleRect().Size;
        GlobalPosition = CameraClamp.Clamp(point.ToWorldPosition(), new ViewSize(viewSize.X, viewSize.Y), _bounds)
            .ToVector2();
    }

    /// <summary>The pan keys' direction: WASD and the arrows. Off while Ctrl is held, so Ctrl+A never pans.</summary>
    private static PanDirection KeyboardDirection() =>
        Input.IsKeyPressed(Key.Ctrl)
            ? PanDirection.None
            : new PanDirection(
                Input.GetAxis("pan_left", "pan_right"), Input.GetAxis("pan_up", "pan_down"));

    /// <summary>The edge pan direction, only while the window has focus and the mouse is inside it.</summary>
    private PanDirection EdgeDirection()
    {
        if (Headless || !_mouseInside || !GetWindow().HasFocus())
        {
            return PanDirection.None;
        }

        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 viewSize = GetViewport().GetVisibleRect().Size;
        return PanRule.EdgeDirection(mouse.X, mouse.Y, new ViewSize(viewSize.X, viewSize.Y), EdgeWidth);
    }

    /// <summary>The bounding rectangle of the ground tiles' diamonds, in world pixels.</summary>
    private static Rect2 GroundRect(TileMapLayer ground)
    {
        Vector2 halfTile = ground.TileSet.TileSize / 2;
        Rect2? rect = null;
        foreach (Vector2I cell in ground.GetUsedCells())
        {
            Vector2 centre = ground.ToGlobal(ground.MapToLocal(cell));
            var tile = new Rect2(centre - halfTile, halfTile * 2);
            rect = rect?.Merge(tile) ?? tile;
        }

        return rect ?? new Rect2(ground.GlobalPosition, Vector2.Zero);
    }
}
