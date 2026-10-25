using Crpg.Core.Selection;
using Godot;

namespace Crpg;

/// <summary>
/// Control component that makes its parent Character an enemy, the sibling type of <see cref="PlayerControl"/>. Its
/// click area moves to the enemy click layer, so a right-click on its body orders the selection to attack it. It does
/// not fight back yet; enemy target picking (#29) will live here.
/// </summary>
public partial class AiControl : Node
{
    public const string EnemiesGroup = "enemies";

    private Character _character = null!;

    /// <summary>The enemy's identity in the selection model: the Character's node name, unique in its zone.</summary>
    public EnemyId EnemyId { get; private set; }

    /// <summary>The character this component controls.</summary>
    public Character Character => _character;

    public override void _EnterTree()
    {
        _character = GetParent<Character>();
        _character.IsEnemy = true;
        EnemyId = new EnemyId(_character.Name);
        AddToGroup(EnemiesGroup);
    }

    /// <summary>True if the area is this enemy's click area.</summary>
    public bool OwnsClickArea(Area2D area) => area.GetParent() == _character;
}
