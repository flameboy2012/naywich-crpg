using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class AttackCooldownTests
{
    private const float NormalSpeed = 1f;

    [Fact]
    public void A_character_is_ready_to_attack_at_start()
    {
        Assert.True(new AttackCooldown().IsReady);
    }

    [Fact]
    public void The_attack_cooldown_is_one_5e_round_of_6_seconds()
    {
        Assert.Equal(6f, AttackCooldown.Seconds);
    }

    [Fact]
    public void A_character_is_not_ready_right_after_attacking()
    {
        var cooldown = new AttackCooldown();

        cooldown.Restart();

        Assert.False(cooldown.IsReady);
    }

    [Fact]
    public void A_character_is_ready_again_6_seconds_after_attacking()
    {
        var cooldown = new AttackCooldown();
        cooldown.Restart();

        cooldown.Tick(5.9f, NormalSpeed);
        Assert.False(cooldown.IsReady);

        cooldown.Tick(0.1f, NormalSpeed);
        Assert.True(cooldown.IsReady);
    }

    [Fact]
    public void Combat_speed_scales_the_cooldown()
    {
        var cooldown = new AttackCooldown();
        cooldown.Restart();

        cooldown.Tick(2.9f, combatSpeed: 2f);
        Assert.False(cooldown.IsReady);

        cooldown.Tick(0.1f, combatSpeed: 2f);
        Assert.True(cooldown.IsReady);
    }

    [Fact]
    public void Attacking_again_restarts_the_full_cooldown()
    {
        var cooldown = new AttackCooldown();
        cooldown.Restart();
        cooldown.Tick(10f, NormalSpeed);

        cooldown.Restart();
        cooldown.Tick(5.9f, NormalSpeed);

        Assert.False(cooldown.IsReady);
    }

    [Fact]
    public void Waiting_while_ready_banks_no_extra_attacks()
    {
        var cooldown = new AttackCooldown();
        cooldown.Tick(30f, NormalSpeed);

        cooldown.Restart();

        Assert.False(cooldown.IsReady);
    }
}
