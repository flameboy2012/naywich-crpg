using Godot;

namespace Crpg.Tools;

/// <summary>
/// Generates the placeholder art (ADR 0001: 64x32 diamond tiles) into res://art/.
/// Run from game/: dotnet build ..\Crpg.sln, then
///   godot --headless --path . --quit-after 60 --script res://tools/GenArt.cs
///   godot --headless --path . --import
/// The output PNGs are committed; rerun this only to change the art.
/// </summary>
public partial class GenArt : SceneTree
{
    private static readonly int TileWidth = Tile.Size.X;
    private static readonly int TileHeight = Tile.Size.Y;

    private static readonly Color GroundLight = new(0.36f, 0.62f, 0.32f);
    private static readonly Color GroundDark = new(0.30f, 0.54f, 0.27f);
    private static readonly Color ObstacleFill = new(0.55f, 0.53f, 0.50f);
    private static readonly Color ObstacleEdge = new(0.22f, 0.20f, 0.20f);
    // Light grey, so each party member's tint (Character.Tint, multiplied in) shows as its own colour.
    private static readonly Color CapsuleBody = new(0.92f, 0.92f, 0.92f);
    private static readonly Color CapsuleEdge = new(0.40f, 0.40f, 0.40f);
    private static readonly Color Ring = new(1.0f, 0.85f, 0.2f);

    public override void _Initialize()
    {
        Save(Tiles(), "res://art/tiles.png");
        Save(Capsule(), "res://art/capsule.png");
        Save(SelectionRing(), "res://art/selection_ring.png");
        Quit();
    }

    private static void Save(Image image, string path)
    {
        Error err = image.SavePng(path);
        GD.Print($"{path}: {err}");
        if (err != Error.Ok)
        {
            GD.PushError($"GenArt: failed to save {path}: {err}");
        }
    }

    /// <summary>
    /// Atlas of three tiles side by side: (0,0) light and (1,0) dark ground for a checker pattern, and (2,0) an
    /// obstacle, grey stone with a dark rim and a dark cross so it never reads as ground.
    /// </summary>
    private static Image Tiles()
    {
        var img = Image.CreateEmpty(TileWidth * 3, TileHeight, false, Image.Format.Rgba8);
        Color[] ground = { GroundLight, GroundDark };
        for (int t = 0; t < 3; t++)
        {
            for (int y = 0; y < TileHeight; y++)
            {
                for (int x = 0; x < TileWidth; x++)
                {
                    float dx = Mathf.Abs(x - (TileWidth - 1) / 2f) / (TileWidth / 2f);
                    float dy = Mathf.Abs(y - (TileHeight - 1) / 2f) / (TileHeight / 2f);
                    if (dx + dy <= 1f)
                    {
                        img.SetPixel(t * TileWidth + x, y, t < 2 ? ground[t] : ObstacleColor(dx, dy));
                    }
                }
            }
        }

        return img;
    }

    /// <summary>Obstacle pixel: dark rim at the diamond edge, dark cross joining the edge midpoints, grey fill.</summary>
    private static Color ObstacleColor(float dx, float dy)
    {
        bool rim = dx + dy > 0.86f;
        bool cross = Mathf.Abs(dx - dy) < 0.08f;
        return rim || cross ? ObstacleEdge : ObstacleFill;
    }

    /// <summary>A 24x48 upright capsule with a dark edge.</summary>
    private static Image Capsule()
    {
        const int w = 24, h = 48, r = 12;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cy = Mathf.Clamp(py, r, h - r);   // nearest point on the capsule's centre line
                float dist = new Vector2(px - r, py - cy).Length();
                if (dist <= r)
                {
                    img.SetPixel(x, y, dist > r - 2f ? CapsuleEdge : CapsuleBody);
                }
            }
        }

        return img;
    }

    /// <summary>A 44x22 ellipse outline, flattened 2:1 to read as a circle on the isometric ground.</summary>
    private static Image SelectionRing()
    {
        const int w = 44, h = 22;
        const float thickness = 2.5f;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        float a = w / 2f, b = h / 2f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - a, dy = y + 0.5f - b;
                float outer = dx * dx / (a * a) + dy * dy / (b * b);
                float ia = a - thickness, ib = b - thickness;
                float inner = dx * dx / (ia * ia) + dy * dy / (ib * ib);
                if (outer <= 1f && inner >= 1f)
                {
                    img.SetPixel(x, y, Ring);
                }
            }
        }

        return img;
    }
}
