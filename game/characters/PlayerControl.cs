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

    public Character Character { get; private set; } = null!;

    /// <summary>The party member's identity in the selection model. M1 uses the Character's node name.</summary>
    public PartyMemberId PartyMemberId { get; private set; }

    public override void _EnterTree()
    {
        Character = GetParent<Character>();
        PartyMemberId = new PartyMemberId(Character.Name);
        AddToGroup(PartyMembersGroup);
    }
}
