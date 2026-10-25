using System.Linq;
using Crpg.Core.Combat;
using Godot;

namespace Crpg;

/// <summary>
/// Watches where combat stands each physics frame through <see cref="CombatRules.AssessCombat"/>. At combat end (no
/// living enemy aware of the party) every downed party member gets up at 1 HP. When every party member is down, the
/// defeat banner shows and the scene restarts after <see cref="RestartSeconds"/>. Pausable, so a paused game neither
/// recovers nor restarts.
/// </summary>
public partial class CombatMonitor : Node
{
    /// <summary>How long (s) "Party defeated" shows before the scene restarts.</summary>
    [Export]
    public float RestartSeconds { get; set; } = 3f;

    /// <summary>The "Party defeated" banner, hidden until defeat.</summary>
    [Export]
    public Label DefeatBanner { get; set; } = null!;

    /// <summary>True once every party member has been down at the same time.</summary>
    public bool Defeated { get; private set; }

    public override void _Ready()
    {
        DefeatBanner.Text = "Party defeated";
        DefeatBanner.Visible = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Defeated)
        {
            return;
        }

        EnemyAwareness[] enemies = GetTree().GetNodesInGroup(AiControl.EnemiesGroup).OfType<AiControl>()
            .Select(enemy => new EnemyAwareness(enemy.Character.IsDead, enemy.IsAware))
            .ToArray();
        Character[] partyMembers = GetTree().GetNodesInGroup(PlayerControl.PartyMembersGroup).OfType<PlayerControl>()
            .Select(partyMember => partyMember.Character)
            .ToArray();

        switch (CombatRules.AssessCombat(enemies, partyMembers.Select(partyMember => partyMember.IsDown).ToArray()))
        {
            case CombatState.Ended:
                foreach (Character partyMember in partyMembers.Where(partyMember => partyMember.IsDown))
                {
                    partyMember.GetUp();
                }

                break;
            case CombatState.Defeat:
                Defeat();
                break;
        }
    }

    /// <summary>Shows "Party defeated", then restarts the scene once the delay has run unpaused.</summary>
    private void Defeat()
    {
        Defeated = true;
        DefeatBanner.Visible = true;
        GD.Print("CombatMonitor: Party defeated.");
        SceneTreeTimer timer = GetTree().CreateTimer(RestartSeconds, processAlways: false);
        timer.Timeout += Restart;
    }

    private void Restart()
    {
        // Headless checks load Main under the root without making it the current scene; there is nothing to reload.
        if (GetTree().CurrentScene is null)
        {
            GD.Print("CombatMonitor: no current scene to restart.");
            return;
        }

        GetTree().ReloadCurrentScene();
    }
}
