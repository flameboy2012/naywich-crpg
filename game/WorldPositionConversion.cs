using Crpg.Core.Selection;
using Godot;

namespace Crpg;

/// <summary>The boundary between Crpg.Core's <see cref="WorldPosition"/> and Godot's <see cref="Vector2"/>.</summary>
public static class WorldPositionConversion
{
    public static WorldPosition ToWorldPosition(this Vector2 vector) => new(vector.X, vector.Y);

    public static Vector2 ToVector2(this WorldPosition position) => new(position.X, position.Y);
}
