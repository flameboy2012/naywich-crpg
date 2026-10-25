using Godot;

namespace Crpg;

/// <summary>Isometric tile geometry (ADR 0001): every tile is a 64x32 px diamond, one tile = 5 ft.</summary>
public static class Tile
{
    /// <summary>Tile size in pixels, as drawn and as set on the TileSet.</summary>
    public static readonly Vector2I Size = new(64, 32);

    /// <summary>One tile's diamond in tile-local coordinates, centred on the tile: left, top, right, bottom.</summary>
    public static Vector2[] Diamond()
    {
        Vector2 half = (Vector2)Size / 2;
        return new[] { new Vector2(-half.X, 0), new Vector2(0, -half.Y), new Vector2(half.X, 0), new Vector2(0, half.Y) };
    }
}
