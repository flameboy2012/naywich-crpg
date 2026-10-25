using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

/// <summary>ADR 0003: the global combat speed scales movement as well as the attack cooldown.</summary>
public class CombatSpeedTests
{
    /// <summary>One tile of distance in world pixels: one (32, 16) px isometric grid step.</summary>
    private const float Tile = 35.777f;

    [Fact]
    public void At_normal_combat_speed_a_30_ft_character_walks_one_tile_per_second()
    {
        Assert.Equal(Tile, CombatRules.MoveSpeedPixelsPerSecond(TestCharacters.Fighter().SpeedTilesPerSecond,
            CombatRules.NormalCombatSpeed), precision: 2);
    }

    [Fact]
    public void Double_combat_speed_doubles_walking_speed()
    {
        Assert.Equal(2 * Tile, CombatRules.MoveSpeedPixelsPerSecond(1f, combatSpeed: 2f), precision: 2);
    }

    [Fact]
    public void Half_combat_speed_halves_walking_speed()
    {
        Assert.Equal(Tile / 2, CombatRules.MoveSpeedPixelsPerSecond(1f, combatSpeed: 0.5f), precision: 2);
    }
}
