namespace Crpg.Core.Combat;

/// <summary>One enemy as combat end sees it: whether it is dead, and whether it is aware of the party.</summary>
public readonly record struct EnemyAwareness(bool IsDead, bool IsAware);
