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

    private static PartyMemberId? Pick(PartyMemberId? current, params TargetCandidate[] partyMembers) =>
        CombatRules.PickEnemyTarget(Enemy, AwarenessRadiusTiles, current, partyMembers);

    [Fact]
    public void Picks_the_nearest_party_member_inside_the_awareness_radius()
    {
        Assert.Equal(Rogue, Pick(null, At(Fighter, 6f), At(Rogue, 3f), At(Wizard, 7.5f)));
    }

    [Fact]
    public void Picks_nobody_when_every_party_member_is_outside_the_awareness_radius()
    {
        Assert.Null(Pick(null, At(Fighter, 8.1f), At(Rogue, 12f)));
    }

    [Fact]
    public void The_awareness_radius_reaches_exactly_its_length_in_grid_steps()
    {
        Assert.Equal(Fighter, Pick(null, At(Fighter, 7.99f)));
        Assert.Null(Pick(null, At(Fighter, 8.01f)));
    }

    [Fact]
    public void Keeps_its_target_while_it_is_up_even_when_another_party_member_comes_closer()
    {
        Assert.Equal(Fighter, Pick(Fighter, At(Fighter, 6f), At(Rogue, 1f)));
    }

    [Fact]
    public void Keeps_its_target_beyond_the_awareness_radius_with_no_leash()
    {
        Assert.Equal(Fighter, Pick(Fighter, At(Fighter, 30f), At(Rogue, 2f)));
    }

    [Fact]
    public void Picks_again_once_its_target_goes_down()
    {
        Assert.Equal(Rogue, Pick(Fighter, At(Fighter, 1f, isDown: true), At(Rogue, 4f), At(Wizard, 6f)));
    }

    [Fact]
    public void Ignores_party_members_who_are_down()
    {
        Assert.Equal(Wizard, Pick(null, At(Fighter, 1f, isDown: true), At(Wizard, 5f)));
        Assert.Null(Pick(null, At(Fighter, 1f, isDown: true)));
    }
}
