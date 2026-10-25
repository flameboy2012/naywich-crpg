namespace Crpg.Core.Selection;

/// <summary>A point in zone world space, in pixels. Converted to and from Godot's Vector2 at the boundary.</summary>
public readonly record struct WorldPosition(float X, float Y);
