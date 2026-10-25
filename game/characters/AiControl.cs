using System.Collections.Generic;
using System.Linq;
using Crpg.Core.Combat;
using Crpg.Core.Selection;
using Godot;

namespace Crpg;

/// <summary>
/// Control component that makes its parent Character an enemy, the sibling type of <see cref="PlayerControl"/>. Its
/// click area moves to the enemy click layer, so a right-click on its body orders the selection to attack it. Each
/// physics frame it runs the Core enemy target pick, keeping whether the enemy is aware of the party, and gives the
/// character an attack order on the picked party member, so the enemy fights with the same move-into-range and
/// attack-cooldown behaviour as party members. Downed party members are never picked; with every party member down
/// the enemy stops.
/// </summary>
public partial class AiControl : Node
{
    public const string EnemiesGroup = "enemies";

    private Character _character = null!;

    /// <summary>The enemy's identity in the selection model: the Character's node name, unique in its zone.</summary>
    public EnemyId EnemyId { get; private set; }

    /// <summary>The character this component controls.</summary>
    public Character Character => _character;

    /// <summary>
    /// True once a party member who is not down has come within this enemy's awareness radius. It stays true until
    /// the enemy is dead (no leash); combat end is when no living enemy is aware.
    /// </summary>
    public bool IsAware { get; private set; }

    /// <summary>
    /// How far, in tiles, this enemy notices a party member: from its MonsterData, else
    /// <see cref="CombatRules.DefaultAwarenessRadiusTiles"/>.
    /// </summary>
    public int AwarenessRadiusTiles =>
        _character.CharacterStats is Data.MonsterData monster
            ? monster.AwarenessRadiusTiles
            : CombatRules.DefaultAwarenessRadiusTiles;

    public override void _EnterTree()
    {
        _character = GetParent<Character>();
        _character.IsEnemy = true;
        EnemyId = new EnemyId(_character.Name);
        AddToGroup(EnemiesGroup);
    }

    /// <summary>True if the area is this enemy's click area.</summary>
    public bool OwnsClickArea(Area2D area) => area.GetParent() == _character;

    // Physics processing obeys ProcessMode, so pausing the tree (PauseManager) freezes enemy AI too.
    public override void _PhysicsProcess(double delta)
    {
        if (_character.IsDead)
        {
            return;
        }

        List<PlayerControl> partyMembers = GetTree().GetNodesInGroup(PlayerControl.PartyMembersGroup)
            .OfType<PlayerControl>().ToList();

        TargetCandidate[] candidates = partyMembers
            .Select(control => new TargetCandidate(control.PartyMemberId, control.GlobalPosition.ToWorldPosition(),
                control.Character.IsDown))
            .ToArray();
        PartyMemberId? current = partyMembers.FirstOrDefault(control => control.Character == _character.AttackTarget)
            ?.PartyMemberId;
        EnemyTargetPick pick = CombatRules.PickEnemyTarget(_character.GlobalPosition.ToWorldPosition(),
            AwarenessRadiusTiles, IsAware, current, candidates);
        IsAware = pick.IsAware;

        if (pick.Target != current
            && partyMembers.FirstOrDefault(control => control.PartyMemberId == pick.Target) is { } picked)
        {
            _character.Attack(picked.Character);
        }
    }
}
