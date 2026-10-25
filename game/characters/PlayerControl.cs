using Crpg.Core.Selection;
using Godot;

namespace Crpg;

/// <summary>
/// Control component that makes its parent Character a party member: the player gives it orders
/// through the SelectionController. AI control (M2) will be a sibling component type.
/// </summary>
public partial class PlayerControl : Node
{
    public const string PartyMembersGroup = "party_members";

    private Character _character = null!;

    /// <summary>The party member's identity in the selection model. M1 uses the Character's node name.</summary>
    public PartyMemberId PartyMemberId { get; private set; }

    /// <summary>Where the party member stands (their feet), in global coordinates.</summary>
    public Vector2 GlobalPosition => _character.GlobalPosition;

    public override void _EnterTree()
    {
        _character = GetParent<Character>();
        PartyMemberId = new PartyMemberId(_character.Name);
        AddToGroup(PartyMembersGroup);
    }

    /// <summary>True if the area is this party member's click area.</summary>
    public bool OwnsClickArea(Area2D area) => area.GetParent() == _character;

    /// <summary>Shows whether the party member is in the selection.</summary>
    public void ShowSelected(bool selected) => _character.SelectionRingVisible = selected;

    /// <summary>Carries out a move order.</summary>
    public void MoveTo(Vector2 globalPosition) => _character.MoveTo(globalPosition);
}
