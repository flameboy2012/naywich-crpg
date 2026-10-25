using Godot;

namespace Crpg.Data;

/// <summary>
/// A monster's stats: a <see cref="CharacterStats"/> with what only monsters need. Converts to the same Core stats on
/// load (ADR 0004).
/// </summary>
[GlobalClass]
public partial class MonsterData : CharacterStats
{
    /// <summary>How far, in tiles, the monster notices an enemy. Provisional until the M2 play-test.</summary>
    [Export(PropertyHint.Range, "1,40,1,or_greater")]
    public int AwarenessRadiusTiles { get; set; } = 8;
}
