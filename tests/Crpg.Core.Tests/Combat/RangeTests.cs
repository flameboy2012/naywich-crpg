using Crpg.Core.Combat;
using Crpg.Core.Selection;

namespace Crpg.Core.Tests.Combat;

public class RangeTests
{
    /// <summary>One tile of distance: one isometric grid step, (32, 16) in world pixels (ADR 0001).</summary>
    private static readonly float Tile = MathF.Sqrt(32f * 32f + 16f * 16f);

    private const float FeetRadius = 8f;

    private static readonly WorldPosition Attacker = new(100f, 200f);

    private static WorldPosition AlongX(float distance) => new(Attacker.X + distance, Attacker.Y);

    [Fact]
    public void Touching_bodies_are_in_melee_reach()
    {
        Assert.True(CombatRules.InRange(Attacker, FeetRadius, AlongX(2 * FeetRadius), FeetRadius,
            TestCharacters.Longsword));
    }

    [Fact]
    public void Melee_reach_is_one_tile_between_the_edges_of_the_feet()
    {
        Assert.True(CombatRules.InRange(Attacker, FeetRadius, AlongX(Tile + 2 * FeetRadius - 0.1f), FeetRadius,
            TestCharacters.Longsword));
    }

    [Fact]
    public void Just_beyond_melee_reach_is_out_of_range()
    {
        Assert.False(CombatRules.InRange(Attacker, FeetRadius, AlongX(Tile + 2 * FeetRadius + 0.1f), FeetRadius,
            TestCharacters.Longsword));
    }

    [Fact]
    public void Both_feet_radii_count()
    {
        Assert.True(CombatRules.InRange(Attacker, 4f, AlongX(Tile + 4f + 12f - 0.1f), 12f, TestCharacters.Longsword));
        Assert.False(CombatRules.InRange(Attacker, 4f, AlongX(Tile + 4f + 12f + 0.1f), 12f, TestCharacters.Longsword));
    }

    [Fact]
    public void The_shortbow_reaches_16_grid_steps_between_the_edges_of_the_feet()
    {
        // Along the grid: each tile is one (32, 16) px step. 16 steps is (512, 256), plus both feet radii.
        WorldPosition AlongGrid(float steps) => new(Attacker.X + 32f * steps, Attacker.Y + 16f * steps);
        float edge = 16f + 2 * FeetRadius / Tile;

        Assert.True(CombatRules.InRange(Attacker, FeetRadius, AlongGrid(edge - 0.01f), FeetRadius,
            TestCharacters.Shortbow));
        Assert.False(CombatRules.InRange(Attacker, FeetRadius, AlongGrid(edge + 0.01f), FeetRadius,
            TestCharacters.Shortbow));
    }

    [Fact]
    public void Fire_Bolt_reaches_24_tiles_of_about_35_78_px()
    {
        const float fireBoltReach = 24 * 35.777f + 2 * FeetRadius;   // about 874.6 px

        Assert.True(CombatRules.InRange(Attacker, FeetRadius, AlongX(fireBoltReach - 0.5f), FeetRadius,
            TestCharacters.FireBolt));
        Assert.False(CombatRules.InRange(Attacker, FeetRadius, AlongX(fireBoltReach + 0.5f), FeetRadius,
            TestCharacters.FireBolt));
    }

    [Fact]
    public void A_ranged_attack_is_not_in_range_at_its_range_in_screen_tile_widths()
    {
        // 16 tiles measured with the 64 px tile width is far past the shortbow's 16 grid steps.
        Assert.False(CombatRules.InRange(Attacker, FeetRadius, AlongX(16 * 64f), FeetRadius, TestCharacters.Shortbow));
        Assert.True(CombatRules.InRange(Attacker, FeetRadius, AlongX(16 * 32f), FeetRadius, TestCharacters.Shortbow));
    }
}
