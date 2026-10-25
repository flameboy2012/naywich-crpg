using System.Collections.Generic;
using System.Linq;
using Crpg.Core.Selection;
using Godot;
using GodotMouseButton = Godot.MouseButton;

namespace Crpg;

/// <summary>
/// Turns mouse clicks into selection-model click events and applies the result to the scene.
/// Holds no control rules: those live in <see cref="SelectionModel"/> in Crpg.Core.
/// </summary>
public partial class SelectionController : Node
{
    /// <summary>Physics layer 3: each Character's ClickArea, which covers the whole capsule sprite.</summary>
    private const uint ClickAreaLayerMask = 1u << 2;

    private readonly SelectionModel _model = new();

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouse)
        {
            return;
        }

        ClickButton? button = mouse.ButtonIndex switch
        {
            GodotMouseButton.Left => ClickButton.Left,
            GodotMouseButton.Right => ClickButton.Right,
            _ => null,
        };
        if (button is null)
        {
            return;
        }

        Vector2 world = GetViewport().GetCanvasTransform().AffineInverse() * mouse.Position;
        Dictionary<PartyMemberId, PlayerControl> partyMembers = PartyMembers();
        ClickTarget target = PartyMemberAt(world, partyMembers.Values) is { } hit
            ? new ClickTarget.PartyMember(hit.PartyMemberId, hit.GlobalPosition.ToWorldPosition())
            : new ClickTarget.Ground(world.ToWorldPosition());

        ClickResult result = _model.Click(
            new ClickEvent(button.Value, target, mouse.ShiftPressed) { PartyMemberPositions = Positions(partyMembers) });
        Apply(result, partyMembers);
        GetViewport().SetInputAsHandled();
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

    private static void Apply(ClickResult result, Dictionary<PartyMemberId, PlayerControl> partyMembers)
    {
        foreach ((PartyMemberId id, PlayerControl partyMember) in partyMembers)
        {
            partyMember.ShowSelected(result.Selection.Contains(id));
        }

        foreach (Order order in result.Orders)
        {
            if (order is MoveOrder move && partyMembers.TryGetValue(move.PartyMember, out PlayerControl? partyMember))
            {
                partyMember.MoveTo(move.Destination.ToVector2());
            }
        }
    }
}
