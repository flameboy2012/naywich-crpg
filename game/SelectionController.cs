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
        List<PlayerControl> partyMembers = PartyMembers();
        ClickTarget target = PartyMemberAt(world, partyMembers) is { } hit
            ? new ClickTarget.PartyMember(hit.PartyMemberId)
            : new ClickTarget.Ground(new WorldPosition(world.X, world.Y));

        ClickResult result = _model.Click(new ClickEvent(button.Value, target));
        Apply(result, partyMembers);
        GetViewport().SetInputAsHandled();
    }

    private List<PlayerControl> PartyMembers() =>
        GetTree().GetNodesInGroup(PlayerControl.PartyMembersGroup).OfType<PlayerControl>().ToList();

    /// <summary>The party member whose click area contains the world point, if any.</summary>
    private PlayerControl? PartyMemberAt(Vector2 world, List<PlayerControl> partyMembers)
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
            if (hit["collider"].AsGodotObject() is Area2D area && area.GetParent() is Character character
                && partyMembers.FirstOrDefault(p => p.Character == character) is { } partyMember)
            {
                return partyMember;
            }
        }

        return null;
    }

    private static void Apply(ClickResult result, List<PlayerControl> partyMembers)
    {
        foreach (PlayerControl partyMember in partyMembers)
        {
            partyMember.Character.SelectionRingVisible = result.Selection.Contains(partyMember.PartyMemberId);
        }
    }
}
