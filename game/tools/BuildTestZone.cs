using Godot;

namespace Crpg.Tools;

/// <summary>
/// Builds the flat isometric test zone: res://zones/test_tiles.tres (TileSet) and res://zones/TestZone.tscn.
/// Needs res://art/tiles.png imported first (see GenArt). Run from game/:
///   godot --headless --path . --quit-after 60 --script res://tools/BuildTestZone.cs
/// The output is committed; rerun this only to change the zone layout.
/// </summary>
public partial class BuildTestZone : SceneTree
{
    /// <summary>Zone is Size x Size tiles. 24x24 is 1536x768 px, inside 1920x1080 with margin.</summary>
    private const int Size = 24;

    private const string TileSetPath = "res://zones/test_tiles.tres";
    private const string ZonePath = "res://zones/TestZone.tscn";

    public override void _Initialize()
    {
        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = new Vector2I(64, 32),
        };
        var atlas = new TileSetAtlasSource
        {
            Texture = GD.Load<Texture2D>("res://art/tiles.png"),
            TextureRegionSize = new Vector2I(64, 32),
        };
        int sourceId = tileSet.AddSource(atlas);
        atlas.CreateTile(new Vector2I(0, 0));   // ground, light
        atlas.CreateTile(new Vector2I(1, 0));   // ground, dark
        Check(ResourceSaver.Save(tileSet, TileSetPath), TileSetPath);
        tileSet = GD.Load<TileSet>(TileSetPath);   // reload so the scene references the file, not an embedded copy

        var zone = new Node2D { Name = "TestZone" };
        var ground = new TileMapLayer { Name = "Ground", TileSet = tileSet };
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                ground.SetCell(new Vector2I(x, y), sourceId, new Vector2I((x + y) % 2, 0));
            }
        }

        zone.AddChild(ground);
        ground.Owner = zone;

        var scene = new PackedScene();
        Check(scene.Pack(zone), "pack");
        Check(ResourceSaver.Save(scene, ZonePath), ZonePath);
        zone.Free();
        Quit();
    }

    private static void Check(Error err, string what)
    {
        GD.Print($"{what}: {err}");
        if (err != Error.Ok)
        {
            GD.PushError($"BuildTestZone: {what} failed: {err}");
        }
    }
}
