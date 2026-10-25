using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Builds the isometric test zone: res://zones/test_tiles.tres (TileSet) and res://zones/TestZone.tscn, with
/// obstacle tiles and a NavigationRegion2D baked from the ground tiles, with holes at the obstacles. Needs res://art/tiles.png imported first (see GenArt).
/// Run from game/ after dotnet build:
///   godot --headless --path . --quit-after 60 --script res://tools/BuildTestZone.cs
/// The output is committed; rerun this only to change the zone layout or navigation.
/// </summary>
public partial class BuildTestZone : SceneTree
{
    /// <summary>
    /// Zone is ZoneSizeInTiles x ZoneSizeInTiles tiles. 24x24 is 1536x768 px, inside 1920x1080 with margin.
    /// </summary>
    private const int ZoneSizeInTiles = 24;

    private const string TileSetPath = "res://zones/test_tiles.tres";
    private const string ZonePath = "res://zones/TestZone.tscn";

    /// <summary>Atlas coordinates of the obstacle tile in res://art/tiles.png.</summary>
    private static readonly Vector2I ObstacleTile = new(2, 0);

    /// <summary>
    /// Obstacle layout, as tile rectangles (x, y, width, height). The party member starts at tile (8, 12):
    /// a long wall to their west, two blocks and an L to the east. Clear of the paths SelectionCheck and MoveCheck use.
    /// </summary>
    private static readonly Rect2I[] Obstacles =
    {
        new(5, 3, 1, 14),    // wall, tiles (5, 3) to (5, 16)
        new(16, 4, 3, 3),    // block, its middle (17, 5) is used by ObstacleCheck as an unreachable target
        new(14, 15, 3, 3),   // block
        new(19, 12, 3, 1),   // L, arm
        new(21, 13, 1, 2),   // L, leg
    };

    private Node2D _zone = null!;
    private NavigationRegion2D _region = null!;
    private int _frame;

    public override void _Initialize()
    {
        TileSet tileSet = BuildTileSet(out int sourceId);

        _zone = new Node2D { Name = "TestZone" };
        _region = new NavigationRegion2D
        {
            Name = "NavigationRegion2D",
            // Margin kept clear of edges and obstacles: the character's feet radius.
            NavigationPolygon = new NavigationPolygon { AgentRadius = CharacterFeetRadius() },
        };

        // The region bakes from its children: the ground tiles' navigation polygons. The layer's own per-tile
        // navigation is off so it does not register regions on top of the baked mesh.
        var ground = new TileMapLayer { Name = "Ground", TileSet = tileSet, NavigationEnabled = false };
        for (int x = 0; x < ZoneSizeInTiles; x++)
        {
            for (int y = 0; y < ZoneSizeInTiles; y++)
            {
                var tile = new Vector2I(x, y);
                Vector2I atlas = IsObstacle(tile) ? ObstacleTile : new Vector2I((x + y) % 2, 0);
                ground.SetCell(tile, sourceId, atlas);
            }
        }

        _zone.AddChild(_region);
        _region.Owner = _zone;
        _region.AddChild(ground);
        ground.Owner = _zone;
        Root.AddChild(_zone);   // baking needs the nodes in the tree; bake on a later frame
    }

    public override bool _Process(double delta)
    {
        if (++_frame != 2)
        {
            return false;
        }

        _region.BakeNavigationPolygon(false);
        int polygons = _region.NavigationPolygon.GetPolygonCount();
        GD.Print($"baked navigation polygons: {polygons}");
        if (polygons == 0)
        {
            GD.PushError("BuildTestZone: navigation bake produced no polygons");
        }

        Root.RemoveChild(_zone);
        var scene = new PackedScene();
        Check(scene.Pack(_zone), "pack");
        Check(ResourceSaver.Save(scene, ZonePath), ZonePath);
        _zone.Free();
        Quit(polygons == 0 ? 1 : 0);
        return false;
    }

    private static TileSet BuildTileSet(out int sourceId)
    {
        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Isometric,
            TileLayout = TileSet.TileLayoutEnum.DiamondDown,
            TileSize = Tile.Size,
        };
        tileSet.AddPhysicsLayer();      // layer 0: obstacle collision (physics layer 1, characters collide with it)
        tileSet.AddNavigationLayer();   // layer 0: walkable ground
        var atlas = new TileSetAtlasSource
        {
            Texture = GD.Load<Texture2D>("res://art/tiles.png"),
            TextureRegionSize = Tile.Size,
        };
        sourceId = tileSet.AddSource(atlas);

        // Region baking only sees tile navigation polygons that have an outline, not just a polygon.
        Vector2[] diamond = Tile.Diamond();
        var walkable = new NavigationPolygon { Vertices = diamond };
        walkable.AddPolygon(new[] { 0, 1, 2, 3 });
        walkable.AddOutline(diamond);
        foreach (var coords in new[] { new Vector2I(0, 0), new Vector2I(1, 0) })   // ground, light and dark
        {
            atlas.CreateTile(coords);
            atlas.GetTileData(coords, 0).SetNavigationPolygon(0, walkable);
        }

        // Obstacle: no navigation polygon, and a collision polygon over the whole diamond. The bake both leaves
        // it out and treats the collision as an obstruction, so the mesh gets a hole with the agent margin.
        atlas.CreateTile(ObstacleTile);
        TileData obstacle = atlas.GetTileData(ObstacleTile, 0);
        obstacle.AddCollisionPolygon(0);
        obstacle.SetCollisionPolygonPoints(0, 0, diamond);

        Check(ResourceSaver.Save(tileSet, TileSetPath), TileSetPath);
        return GD.Load<TileSet>(TileSetPath);   // reload so the scene references the file, not an embedded copy
    }

    private static float CharacterFeetRadius()
    {
        var character = GD.Load<PackedScene>(Character.ScenePath).Instantiate<Character>();
        float radius = character.FeetRadius;
        character.Free();
        return radius;
    }

    private static bool IsObstacle(Vector2I tile) => Obstacles.Any(obstacle => obstacle.HasPoint(tile));

    private static void Check(Error err, string what)
    {
        GD.Print($"{what}: {err}");
        if (err != Error.Ok)
        {
            GD.PushError($"BuildTestZone: {what} failed: {err}");
        }
    }
}
