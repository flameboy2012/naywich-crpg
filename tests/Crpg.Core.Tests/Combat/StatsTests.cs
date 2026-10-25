using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class StatsTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(14, 2)]
    [InlineData(15, 2)]
    [InlineData(20, 5)]
    public void An_ability_modifier_follows_the_5e_table(int score, int modifier)
    {
        Assert.Equal(modifier, AbilityScores.Modifier(score));
    }

    [Fact]
    public void Attack_bonus_is_proficiency_plus_the_attack_ability_modifier()
    {
        Assert.Equal(4, TestCharacters.Fighter().AttackBonus); // +2 proficiency, STR 15 +2
        Assert.Equal(4, TestCharacters.Rogue().AttackBonus); // DEX 15 +2
        Assert.Equal(4, TestCharacters.Wizard().AttackBonus); // INT 15 +2
    }

    [Fact]
    public void Finesse_uses_the_higher_of_strength_and_dexterity()
    {
        Stats goblin = TestCharacters.Goblin(); // STR 8, DEX 14
        Stats strongGoblin = goblin with { Abilities = goblin.Abilities with { Strength = 18 } };

        Assert.Equal(4, goblin.AttackBonus); // DEX +2
        Assert.Equal(6, strongGoblin.AttackBonus); // STR +4
    }

    [Fact]
    public void Armour_class_is_the_armour_base_plus_the_dexterity_modifier()
    {
        Assert.Equal(13, TestCharacters.Rogue().ArmorClass); // leather 11 + DEX 2
        Assert.Equal(15, TestCharacters.Goblin().ArmorClass); // leather and shield 13 + DEX 2
    }

    [Fact]
    public void The_armour_caps_the_dexterity_bonus()
    {
        Stats fighter = TestCharacters.Fighter();
        Stats nimbleFighter = fighter with { Abilities = fighter.Abilities with { Dexterity = 16 } };
        Stats halfPlateRogue = TestCharacters.Rogue() with { Armor = new Armor("Half plate", 15, MaxDexterityBonus: 2) };
        Stats veryNimble = halfPlateRogue with { Abilities = halfPlateRogue.Abilities with { Dexterity = 18 } };

        Assert.Equal(16, fighter.ArmorClass); // chain mail allows no DEX
        Assert.Equal(16, nimbleFighter.ArmorClass);
        Assert.Equal(17, veryNimble.ArmorClass); // DEX +4 capped at +2
    }

    [Fact]
    public void A_negative_dexterity_modifier_still_lowers_capped_armour()
    {
        Stats clumsy = TestCharacters.Rogue() with
        {
            Abilities = TestCharacters.Rogue().Abilities with { Dexterity = 8 },
            Armor = new Armor("Half plate", 15, MaxDexterityBonus: 2),
        };

        Assert.Equal(14, clumsy.ArmorClass);
    }

    [Fact]
    public void Armour_that_allows_no_dexterity_bonus_ignores_a_negative_modifier_too()
    {
        Stats fighter = TestCharacters.Fighter();
        Stats clumsyFighter = fighter with { Abilities = fighter.Abilities with { Dexterity = 8 } };

        Assert.Equal(16, clumsyFighter.ArmorClass);
    }

    [Fact]
    public void With_no_armour_armour_class_is_10_plus_the_dexterity_modifier()
    {
        Assert.Equal(12, TestCharacters.Wizard().ArmorClass); // DEX 14
    }

    [Fact]
    public void Speed_in_feet_converts_to_tiles_per_second_at_one_tile_per_second_for_30_ft()
    {
        Assert.Equal(1f, TestCharacters.Fighter().SpeedTilesPerSecond);
        Assert.Equal(0.5f, (TestCharacters.Fighter() with { SpeedFeet = 15 }).SpeedTilesPerSecond);
    }
}
