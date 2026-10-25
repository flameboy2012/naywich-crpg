using Crpg.Core.Selection;

namespace Crpg.Core.Combat;

/// <summary>
/// The result of an enemy's target pick: whether the enemy is now aware of the party, and the party member it attacks,
/// or nobody.
/// </summary>
public readonly record struct EnemyTargetPick(bool IsAware, PartyMemberId? Target);
