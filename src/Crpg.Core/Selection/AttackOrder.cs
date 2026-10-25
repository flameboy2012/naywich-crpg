namespace Crpg.Core.Selection;

/// <summary>
/// Order for one party member to attack an enemy: walk until it is in range, then attack once per attack cooldown
/// until it is dead. Any new order replaces it.
/// </summary>
public sealed record AttackOrder(PartyMemberId PartyMember, EnemyId Target) : Order(PartyMember);
