using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Builds the isometric test zone: res://zones/test_tiles.tres (TileSet) and res://zones/TestZone.tscn, with
/// obstacle tiles and a NavigationRegion2D baked from the ground tiles, with holes at the obstacles. Needs
/// res://art/tiles.png imported first (see GenArt). Run from game/ after dotnet build:
///   godot --headless --path . --quit-after 60 --script res://tools/BuildTestZone.cs
/// The output is committed; rerun this only to change the zone layout or navigation.
/// </summary>
public partial class BuildTestZone : SceneTree
{
    /// <summary>
    /// Zone is ZoneSizeInTiles x ZoneSizeInTiles tiles. 48x48 is 3072x1536 px, about 2-3 screens of 1920x1080, so the
    /// camera has somewhere to pan. It grew from 24x24 in place: tile (0, 0) and the M1.5 obstacles didn't move.
    /// </summary>
    private const int ZoneSizeInTiles = 48;

    private const string TileSetPath = "res://zones/test_tiles.tres";
    private const string ZonePath = "res://zones/TestZone.tscn";

    /// <summary>Atlas coordinates of the obstacle tile in res://art/tiles.png.</summary>
    private static readonly Vector2I ObstacleTile = new(2, 0);

    /// <summary>
    /// Obstacle layout, as tile rectangles (x, y, width, height). The party member starts at tile (8, 12):
    /// a long wall to their west, two blocks and an L to the east. Clear of the paths SelectionCheck and MoveCheck use.
    /// The rest are in the area added at 48x48, so pathfinding is exercised there too.
    /// </summary>
    private static readonly Rect2I[] Obstacles =
    {
        // M1.5 obstacles, at their original tile coordinates.
        new(5, 3, 1, 14),    // wall, tiles (5, 3) to (5, 16)
        new(16, 4, 3, 3),    // block, its middle (17, 5) is used by ObstacleCheck as an unreachable target
        new(14, 15, 3, 3),   // block
        new(19, 12, 3, 1),   // L, arm
        new(21, 13, 1, 2),   // L, leg

        // Added with the 48x48 zone.
        new(30, 6, 1, 12),   // wall, tiles (30, 6) to (30, 17)
        new(36, 28, 4, 4),   // block
        new(8, 32, 8, 1),    // wall, tiles (8, 32) to (15, 32)
        new(26, 38, 3, 3),   // block
        new(40, 10, 3, 1),   // L, arm
        new(42, 11, 1, 3),   // L, leg
    };

    private const string ScimitarGoblinPath = "res://data/monsters/goblin_scimitar.tres";
    private const string ShortbowGoblinPath = "res://data/monsters/goblin_shortbow.tres";

    /// <summary>Placeholder goblin tint: a muddy green, distinct from the four party member tints.</summary>
    private static readonly Color GoblinTint = new(0.45f, 0.7f, 0.25f);

    /// <summary>
    /// Goblins, as (node name, combat log name, stats, tile): one cluster of four, two scimitars and two shortbows,
    /// about 15 tiles south-east of the party start at tile (8, 12). The cluster sits more than the 8-tile awareness
    /// radius away from everywhere the other headless checks walk (moves of about 200 px from the start, and the
    /// obstacle block at (16, 4)), so those checks never wake a goblin. Goblin 1 is the nearest; Goblin 2 stands at the
    /// back, just out of the rogue's shortbow range from the party start, for CombatCheck's ranged attackers.
    /// </summary>
    private static readonly (string Node, string Name, string Stats, Vector2I Tile)[] Goblins =
    {
        ("Goblin1", "Goblin 1", ScimitarGoblinPath, new Vector2I(27, 24)),
        ("Goblin2", "Goblin 2", ShortbowGoblinPath, new Vector2I(30, 26)),
        ("Goblin3", "Goblin 3", ScimitarGoblinPath, new Vector2I(29, 24)),
        ("Goblin4", "Goblin 4", ShortbowGoblinPath, new Vector2I(28, 26)),
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
        AddGoblins(ground);
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

    /// <summary>
    /// Adds each goblin as an instance of the shared character scene with the goblin's tint and stats and an
    /// <see cref="AiControl"/>. They sit outside the navigation region, so the bake doesn't carve them out.
    /// </summary>
    private void AddGoblins(TileMapLayer ground)
    {
        var scene = GD.Load<PackedScene>(Character.ScenePath);
        foreach ((string node, string name, string statsPath, Vector2I tile) in Goblins)
        {
            if (IsObstacle(tile))
            {
                GD.PushError($"BuildTestZone: goblin {name} stands on an obstacle tile {tile}");
            }

            var goblin = scene.Instantiate<Character>(PackedScene.GenEditState.Instance);
            goblin.Name = node;
            goblin.InstanceName = name;
            goblin.Tint = GoblinTint;
            goblin.CharacterStats = GD.Load<Data.CharacterStats>(statsPath);
            goblin.Position = ground.MapToLocal(tile);
            _zone.AddChild(goblin);
            goblin.Owner = _zone;

            var control = new AiControl { Name = "AiControl" };
            goblin.AddChild(control);
            control.Owner = _zone;
            GD.Print($"{name} at tile {tile}, position {goblin.Position}");
        }
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
