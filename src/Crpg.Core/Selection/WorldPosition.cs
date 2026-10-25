namespace Crpg.Core.Selection;

/// <summary>A point in zone world space, in pixels. Converted to and from Godot's Vector2 at the boundary.</summary>
public readonly record struct WorldPosition(float X, float Y)
{
    /// <summary>Straight-line distance to another point, in world pixels.</summary>
    public float DistanceTo(WorldPosition other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
