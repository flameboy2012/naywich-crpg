using System.Collections.Generic;
using System.Linq;
using Crpg.Core.Selection;
using Godot;
using GodotMouseButton = Godot.MouseButton;

namespace Crpg;

/// <summary>
/// Turns mouse input into selection-model events and applies the result to the scene. A left press, drag and
/// release becomes either a click or a box select; right-click is a click on press.
/// Holds no control rules: those live in <see cref="SelectionModel"/> in Crpg.Core.
/// </summary>
public partial class SelectionController : Node
{
    /// <summary>Physics layer 3: each Character's ClickArea, which covers the whole capsule sprite.</summary>
    private const uint ClickAreaLayerMask = 1u << 2;

    /// <summary>How far (screen px) a left press must move before it is a box select rather than a click.</summary>
    [Export]
    public float DragThreshold { get; set; } = 6f;

    /// <summary>Where move markers are added: a world-space node drawn above the zone.</summary>
    [Export]
    public Node2D MoveMarkers { get; set; } = null!;

    private readonly SelectionModel _model = new();

    private Panel _box = null!;

    /// <summary>Where the left button went down (screen px), while it is held.</summary>
    private Vector2? _pressedAt;

    private bool _dragging;

    /// <summary>True while the box select rectangle is drawn on screen.</summary>
    public bool BoxVisible => _box.Visible;

    public override void _Ready()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.55f, 0.85f, 0.55f, 0.12f),
            BorderColor = new Color(0.55f, 0.85f, 0.55f, 0.9f),
        };
        style.SetBorderWidthAll(1);
        _box = new Panel { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _box.AddThemeStyleboxOverride("panel", style);
        var layer = new CanvasLayer { Name = "BoxSelectLayer" };
        layer.AddChild(_box);
        AddChild(layer);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: GodotMouseButton.Left } left:
                OnLeftButton(left);
                break;
            case InputEventMouseMotion motion when _pressedAt is { } pressedAt:
                OnDrag(pressedAt, motion.Position);
                break;
            case InputEventMouseButton { ButtonIndex: GodotMouseButton.Right, Pressed: true } right:
                Click(ClickButton.Right, right.Position, right.ShiftPressed);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    /// <summary>A left press starts a possible drag; the release decides whether it was a click or a box select.</summary>
    private void OnLeftButton(InputEventMouseButton left)
    {
        if (left.Pressed)
        {
            _pressedAt = left.Position;
            _dragging = false;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_pressedAt is not { } pressedAt)
        {
            return;
        }

        OnDrag(pressedAt, left.Position);
        if (_dragging)
        {
            BoxSelect(new Rect2(pressedAt, left.Position - pressedAt).Abs(), left.ShiftPressed);
        }
        else
        {
            Click(ClickButton.Left, pressedAt, left.ShiftPressed);
        }

        _pressedAt = null;
        _dragging = false;
        _box.Visible = false;
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Once the mouse has moved past the threshold, the press is a drag and the box follows the mouse.</summary>
    private void OnDrag(Vector2 pressedAt, Vector2 mouse)
    {
        _dragging |= pressedAt.DistanceTo(mouse) >= DragThreshold;
        if (!_dragging)
        {
            return;
        }

        Rect2 rect = new Rect2(pressedAt, mouse - pressedAt).Abs();
        _box.Position = rect.Position;
        _box.Size = rect.Size;
        _box.Visible = true;
    }

    /// <summary>Sends a click at a screen point to the model, resolved to a party member or ground.</summary>
    private void Click(ClickButton button, Vector2 screen, bool shift)
    {
        Vector2 world = GetViewport().GetCanvasTransform().AffineInverse() * screen;
        Dictionary<PartyMemberId, PlayerControl> partyMembers = PartyMembers();
        ClickTarget target = PartyMemberAt(world, partyMembers.Values) is { } hit
            ? new ClickTarget.PartyMember(hit.PartyMemberId, hit.GlobalPosition.ToWorldPosition())
            : new ClickTarget.Ground(world.ToWorldPosition());

        ClickResult result = _model.Click(
            new ClickEvent(button, target, shift) { PartyMemberPositions = Positions(partyMembers) });
        Apply(result, partyMembers);
    }

    /// <summary>Box selects the party members whose feet, projected to the screen, are inside the rectangle.</summary>
    private void BoxSelect(Rect2 screenRect, bool shift)
    {
        Transform2D canvas = GetViewport().GetCanvasTransform();
        Dictionary<PartyMemberId, PlayerControl> partyMembers = PartyMembers();
        PartyMemberId[] inside = partyMembers.Values
            .Where(partyMember => screenRect.HasPoint(canvas * partyMember.GlobalPosition))
            .Select(partyMember => partyMember.PartyMemberId)
            .ToArray();

        Apply(_model.BoxSelect(new BoxSelectEvent(inside, shift)), partyMembers);
    }

    private Dictionary<PartyMemberId, PlayerControl> PartyMembers() =>
        GetTree().GetNodesInGroup(PlayerControl.PartyMembersGroup).OfType<PlayerControl>()
            .ToDictionary(partyMember => partyMember.PartyMemberId);

    /// <summary>Where each party member stands now, for the model to give the selection their spread slots.</summary>
    private static Dictionary<PartyMemberId, WorldPosition> Positions(Dictionary<PartyMemberId, PlayerControl> partyMembers) =>
        partyMembers.ToDictionary(entry => entry.Key, entry => entry.Value.GlobalPosition.ToWorldPosition());

    /// <summary>The party member whose click area contains the world point, if any.</summary>
    private PlayerControl? PartyMemberAt(Vector2 world, IEnumerable<PlayerControl> partyMembers)
    {
        var query = new PhysicsPointQueryParameters2D
        {
            Position = world,
            CollideWithAreas = true,
            CollideWithBodies = false,
            CollisionMask = ClickAreaLayerMask,
        };
        foreach (var hit in GetViewport().World2D.DirectSpaceState.IntersectPoint(query))
        {
            if (hit["collider"].AsGodotObject() is Area2D area
                && partyMembers.FirstOrDefault(partyMember => partyMember.OwnsClickArea(area)) is { } partyMember)
            {
                return partyMember;
            }
        }

        return null;
    }

    private void Apply(ClickResult result, Dictionary<PartyMemberId, PlayerControl> partyMembers)
    {
        foreach ((PartyMemberId id, PlayerControl partyMember) in partyMembers)
        {
            partyMember.ShowSelected(result.Selection.Contains(id));
        }

        List<PlayerControl> moved = new();
        foreach (Order order in result.Orders)
        {
            if (order is MoveOrder move && partyMembers.TryGetValue(move.PartyMember, out PlayerControl? partyMember))
            {
                partyMember.MoveTo(move.Destination.ToVector2());
                moved.Add(partyMember);
            }
        }

        if (moved.Count > 0)
        {
            ShowMoveMarkers(moved);
        }
    }

    /// <summary>Replaces every move marker with one per moved party member, at the real end of their path.</summary>
    private void ShowMoveMarkers(IEnumerable<PlayerControl> moved)
    {
        foreach (Node old in MoveMarkers.GetChildren())
        {
            MoveMarkers.RemoveChild(old);
            old.QueueFree();
        }

        foreach (PlayerControl partyMember in moved)
        {
            MoveMarkers.AddChild(new MoveMarker { Tint = partyMember.Tint, Position = MoveMarkers.ToLocal(partyMember.PathEnd) });
        }
    }
}
