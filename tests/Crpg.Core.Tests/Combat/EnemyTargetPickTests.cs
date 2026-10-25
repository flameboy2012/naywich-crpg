using Crpg.Core.Combat;
using Crpg.Core.Selection;

namespace Crpg.Core.Tests.Combat;

public class EnemyTargetPickTests
{
    /// <summary>One tile of distance: one isometric grid step, (32, 16) in world pixels (ADR 0001).</summary>
    private static readonly float Tile = MathF.Sqrt(32f * 32f + 16f * 16f);

    private const int AwarenessRadiusTiles = 8;

    private static readonly WorldPosition Enemy = new(1000f, 1000f);

    private static readonly PartyMemberId Fighter = new("Fighter");
    private static readonly PartyMemberId Rogue = new("Rogue");
    private static readonly PartyMemberId Wizard = new("Wizard");

    private static TargetCandidate At(PartyMemberId id, float tiles, bool isDown = false) =>
        new(id, new WorldPosition(Enemy.X + tiles * Tile, Enemy.Y), isDown);

    /// <summary>The pick of an enemy not yet aware of the party.</summary>
    private static EnemyTargetPick Unaware(params TargetCandidate[] partyMembers) =>
        CombatRules.PickEnemyTarget(Enemy, AwarenessRadiusTiles, isAware: false, current: null, partyMembers);

    /// <summary>The pick of an enemy already aware of the party.</summary>
    private static EnemyTargetPick Aware(PartyMemberId? current, params TargetCandidate[] partyMembers) =>
        CombatRules.PickEnemyTarget(Enemy, AwarenessRadiusTiles, isAware: true, current, partyMembers);

    [Fact]
    public void An_unaware_enemy_becomes_aware_and_picks_the_nearest_party_member_inside_the_awareness_radius()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Rogue),
            Unaware(At(Fighter, 6f), At(Rogue, 3f), At(Wizard, 7.5f)));
    }

    [Fact]
    public void An_unaware_enemy_stays_unaware_and_picks_nobody_while_every_party_member_is_outside_the_radius()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: false, null), Unaware(At(Fighter, 8.1f), At(Rogue, 12f)));
    }

    [Fact]
    public void The_awareness_radius_reaches_exactly_its_length_in_grid_steps()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Fighter), Unaware(At(Fighter, 7.99f)));
        Assert.False(Unaware(At(Fighter, 8.01f)).IsAware);
    }

    [Fact]
    public void A_downed_party_member_inside_the_radius_does_not_make_an_enemy_aware()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: false, null), Unaware(At(Fighter, 1f, isDown: true), At(Rogue, 12f)));
    }

    [Fact]
    public void An_unaware_enemy_ignores_party_members_who_are_down()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Wizard), Unaware(At(Fighter, 1f, isDown: true), At(Wizard, 5f)));
    }

    [Fact]
    public void Keeps_its_target_while_it_is_up_even_when_another_party_member_comes_closer()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Fighter), Aware(Fighter, At(Fighter, 6f), At(Rogue, 1f)));
    }

    [Fact]
    public void Keeps_its_target_beyond_the_awareness_radius_with_no_leash()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Fighter), Aware(Fighter, At(Fighter, 30f), At(Rogue, 2f)));
    }

    [Fact]
    public void Picks_the_nearest_party_member_still_up_once_its_target_goes_down()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Rogue),
            Aware(Fighter, At(Fighter, 1f, isDown: true), At(Rogue, 4f), At(Wizard, 6f)));
    }

    [Fact]
    public void An_aware_enemy_picks_the_nearest_party_member_still_up_at_any_distance()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, Wizard),
            Aware(Fighter, At(Fighter, 1f, isDown: true), At(Rogue, 40f), At(Wizard, 20f)));
    }

    [Fact]
    public void An_aware_enemy_stays_aware_with_nobody_to_pick_once_every_party_member_is_down()
    {
        Assert.Equal(new EnemyTargetPick(IsAware: true, null),
            Aware(Fighter, At(Fighter, 1f, isDown: true), At(Rogue, 3f, isDown: true)));
    }
}
