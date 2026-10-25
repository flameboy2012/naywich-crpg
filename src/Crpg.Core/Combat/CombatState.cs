namespace Crpg.Core.Combat;

/// <summary>Where a zone's combat stands, from <see cref="CombatRules.AssessCombat"/>.</summary>
public enum CombatState
{
    /// <summary>A living enemy is aware of the party, and at least one party member is up.</summary>
    Fighting,

    /// <summary>Combat end: no living enemy is aware of the party. Downed party members get up.</summary>
    Ended,

    /// <summary>Every party member is down.</summary>
    Defeat,
}
