using Godot;

namespace Crpg.Tools;

/// <summary>
/// Builds the flat isometric test zone: res://zones/test_tiles.tres (TileSet) and res://zones/TestZone.tscn,
/// with a NavigationRegion2D baked from the ground tiles. Needs res://art/tiles.png imported first (see GenArt).
/// Run from game/ after dotnet build:
///   godot --headless --path . --quit-after 60 --script res://tools/BuildTestZone.cs
/// The output is committed; rerun this only to change the zone layout or navigation.
/// </summary>
public partial class BuildTestZone : SceneTree
{
    /// <summary>Zone is Size x Size tiles. 24x24 is 1536x768 px, inside 1920x1080 with margin.</summary>
    private const int Size = 24;

    /// <summary>Navigation mesh margin kept clear of edges (and later obstacles). Matches Character's feet radius.</summary>
    private const float AgentRadius = 8f;

    private const string TileSetPath = "res://zones/test_tiles.tres";
    private const string ZonePath = "res://zones/TestZone.tscn";

    /// <summary>One tile's diamond in tile-local coordinates (centred on the tile).</summary>
    private static readonly Vector2[] Diamond = { new(-32, 0), new(0, -16), new(32, 0), new(0, 16) };

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
            NavigationPolygon = new NavigationPolygon { AgentRadius = AgentRadius },
        };

        // The region bakes from its children: the ground tiles' navigation polygons. The layer's own per-tile
        // navigation is off so it does not register regions on top of the baked mesh.
        var ground = new TileMapLayer { Name = "Ground", TileSet = tileSet, NavigationEnabled = false };
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                ground.SetCell(new Vector2I(x, y), sourceId, new Vector2I((x + y) % 2, 0));
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
            TileSize = new Vector2I(64, 32),
        };
        tileSet.AddNavigationLayer();   // layer 0: walkable ground
        var atlas = new TileSetAtlasSource
        {
            Texture = GD.Load<Texture2D>("res://art/tiles.png"),
            TextureRegionSize = new Vector2I(64, 32),
        };
        sourceId = tileSet.AddSource(atlas);

        // Region baking only sees tile navigation polygons that have an outline, not just a polygon.
        var walkable = new NavigationPolygon { Vertices = Diamond };
        walkable.AddPolygon(new[] { 0, 1, 2, 3 });
        walkable.AddOutline(Diamond);
        foreach (var coords in new[] { new Vector2I(0, 0), new Vector2I(1, 0) })   // ground, light and dark
        {
            atlas.CreateTile(coords);
            atlas.GetTileData(coords, 0).SetNavigationPolygon(0, walkable);
        }

        Check(ResourceSaver.Save(tileSet, TileSetPath), TileSetPath);
        return GD.Load<TileSet>(TileSetPath);   // reload so the scene references the file, not an embedded copy
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
