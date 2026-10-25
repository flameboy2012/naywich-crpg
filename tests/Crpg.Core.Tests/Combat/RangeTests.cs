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
}
