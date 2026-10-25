using Crpg.Core.Selection;

namespace Crpg.Core.Combat;

/// <summary>One party member as an enemy's target pick sees them: who, where their feet are, and whether they are down.</summary>
public readonly record struct TargetCandidate(PartyMemberId Id, WorldPosition Feet, bool IsDown);
