namespace Crpg.Core.Combat;

/// <summary>
/// One character's attack cooldown: the time they must wait between attacks, one 5e round (ADR 0003). It belongs to
/// the character, not to an order, so new orders and target switches never reset it. Ready at start.
/// </summary>
public sealed class AttackCooldown
{
    /// <summary>
    /// One 5e round in real seconds at normal combat speed. The one place the attack cooldown length lives.
    /// </summary>
    public const float Seconds = 6f;

    /// <summary>Slack for float rounding, so 60 frames of 0.1 s count as a full 6 s.</summary>
    private const float Tolerance = 1e-4f;

    private float _remaining;

    /// <summary>True when the character may attack now.</summary>
    public bool IsReady => _remaining <= Tolerance;

    /// <summary>Seconds of cooldown left at normal combat speed; 0 when ready.</summary>
    public float Remaining => Math.Max(0f, _remaining);

    /// <summary>
    /// Counts down by real seconds scaled by combat speed. Stopping the calls (pause) stops the countdown.
    /// </summary>
    public void Tick(float realSeconds, float combatSpeed)
    {
        _remaining = Math.Max(0f, _remaining - realSeconds * combatSpeed);
    }

    /// <summary>Starts a full cooldown: call on each attack.</summary>
    public void Restart()
    {
        _remaining = Seconds;
    }
}
