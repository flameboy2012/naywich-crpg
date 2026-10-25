using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class CombatEndTests
{
    private static readonly EnemyAwareness AwareAndAlive = new(IsDead: false, IsAware: true);
    private static readonly EnemyAwareness Unaware = new(IsDead: false, IsAware: false);
    private static readonly EnemyAwareness DeadAfterBeingAware = new(IsDead: true, IsAware: true);

    private static readonly bool[] OneDown = { true, false, false, false };

    [Fact]
    public void Combat_has_not_ended_while_any_living_enemy_is_aware_of_the_party()
    {
        Assert.Equal(CombatState.Fighting,
            CombatRules.AssessCombat(new[] { DeadAfterBeingAware, AwareAndAlive, Unaware }, OneDown));
    }

    [Fact]
    public void Combat_ends_once_every_aware_enemy_is_dead_even_with_unaware_enemies_left()
    {
        Assert.Equal(CombatState.Ended,
            CombatRules.AssessCombat(new[] { DeadAfterBeingAware, DeadAfterBeingAware, Unaware }, OneDown));
    }

    [Fact]
    public void Every_party_member_down_is_defeat_while_enemies_are_still_aware()
    {
        Assert.Equal(CombatState.Defeat,
            CombatRules.AssessCombat(new[] { AwareAndAlive, DeadAfterBeingAware }, new[] { true, true, true, true }));
    }

    [Fact]
    public void A_downed_party_member_gets_up_at_1_HP_at_combat_end()
    {
        Assert.Equal(1, CombatRules.HpAtCombatEnd(0));
    }

    [Fact]
    public void A_party_member_who_is_up_keeps_their_HP_at_combat_end()
    {
        Assert.Equal(5, CombatRules.HpAtCombatEnd(5));
    }
}
