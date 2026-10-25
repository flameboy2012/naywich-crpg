namespace Crpg.Core.Selection;

/// <summary>Identity of one enemy in the zone, as the selection model sees it.</summary>
public readonly record struct EnemyId(string Value)
{
    public override string ToString() => Value;
}
