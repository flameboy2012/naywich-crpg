namespace Crpg.Core;

/// <summary>
/// The tile scale fixed by ADR 0001: every tile is a 64x32 px isometric diamond and one tile is 5 ft. The one place the
/// tile size and the grid-step length are defined; the game's Tile and Character read them from here.
/// </summary>
public static class TileScale
{
    /// <summary>Tile width in pixels, as drawn and as set on the TileSet.</summary>
    public const int Width = 64;

    /// <summary>Tile height in pixels, as drawn and as set on the TileSet.</summary>
    public const int Height = 32;

    /// <summary>
    /// One tile of distance in world pixels: one isometric grid step, tile (x, y) to (x + 1, y). That is half the tile
    /// size, (32, 16), about 35.78 px; not the tile's screen width or height. Speeds, ranges and spread slots use it.
    /// </summary>
    public static readonly float GridStepLength = MathF.Sqrt(Width / 2f * (Width / 2f) + Height / 2f * (Height / 2f));
}
