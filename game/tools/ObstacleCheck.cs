using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless smoke check for obstacles: loads Main, checks the baked navigation mesh has holes at the obstacle
/// tiles and the zone fits the static camera's view, then pushes real clicks through the viewport and watches the
/// party member walk round obstacles. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 3600 --script res://tools/ObstacleCheck.cs
/// Prints "ObstacleCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class ObstacleCheck : HeadlessCheck
{
    /// <summary>A tile on the far side of the wall from the party member's start.</summary>
    private static readonly Vector2I PastWallTile = new(2, 10);

    /// <summary>The middle tile of an obstacle block, unreachable.</summary>
    private static readonly Vector2I InsideObstacleTile = new(17, 5);


    private static readonly Vector2 ViewSize = new(1920, 1080);

    private TileMapLayer _ground = null!;
    private List<Vector2[]> _obstacles = null!;
    private float _closestToObstacle = float.MaxValue;

    protected override void EachPhysicsFrame()
    {
        if (_obstacles is not null)
        {
            _closestToObstacle = Mathf.Min(_closestToObstacle, DistanceToObstacles(PartyMember.GlobalPosition));
        }
    }

    protected override IEnumerator<int> Run()
    {
        _ground = Root.GetNode<TileMapLayer>("Main/ZoneContainer/TestZone/NavigationRegion2D/Ground");
        List<Vector2I> obstacleTiles = _ground.GetUsedCells().Where(IsObstacle).ToList();
        _obstacles = obstacleTiles.Select(TileDiamond).ToList();
        Rid map = _ground.GetParent<NavigationRegion2D>().GetNavigationMap();
        Vector2 groundCentre = TileCentre(_ground.LocalToMap(_ground.ToLocal(PartyMember.GlobalPosition)));
        for (int i = 0; i < 120 && NavigationServer2D.MapGetClosestPoint(map, groundCentre) == Vector2.Zero; i++)
        {
            yield return 1;   // map queries return (0, 0) until the map has synced with the zone's region
        }

        Vector2 groundClosest = NavigationServer2D.MapGetClosestPoint(map, groundCentre);
        Expect($"ground under the party member is on the navigation mesh ({groundCentre} -> {groundClosest})",
            groundClosest.DistanceTo(groundCentre) < 0.5f);
        Expect($"zone has obstacle tiles ({obstacleTiles.Count})", obstacleTiles.Count > 0);
        Expect("every obstacle tile is a hole in the navigation mesh, with the feet-radius margin", obstacleTiles.All(tile =>
            DistanceToObstacles(NavigationServer2D.MapGetClosestPoint(map, TileCentre(tile))) >= PartyMember.FeetRadius - 0.5f));

        Rect2 zoneOnScreen = ZoneScreenRect();
        Expect($"whole zone fits the 1920x1080 view with the static camera ({zoneOnScreen})",
            new Rect2(Vector2.Zero, ViewSize).Encloses(zoneOnScreen) && Root.GetVisibleRect().Size == ViewSize);

        Vector2 start = PartyMember.GlobalPosition;
        Vector2 onObstacle = TileCentre(obstacleTiles[0]);
        Click(MouseButton.Right, onObstacle);
        yield return 30;
        Expect("right-click on an obstacle with nothing selected does nothing",
            PartyMember.GlobalPosition.DistanceTo(start) < StillTolerance && !PartyMember.IsMoving);
        ClickPartyMember(MouseButton.Left);
        yield return 2;
        Expect("left-click selects them", PartyMember.SelectionRingVisible);
        Click(MouseButton.Left, onObstacle);
        yield return 2;
        Expect("left-click on an obstacle counts as ground and clears the selection", !PartyMember.SelectionRingVisible);

        ClickPartyMember(MouseButton.Left);
        yield return 2;
        Expect("left-click selects them again", PartyMember.SelectionRingVisible);

        Vector2 pastWall = TileCentre(PastWallTile);
        Expect("the straight line to the target crosses an obstacle (precondition)",
            _obstacles.Any(diamond => SegmentHitsPolygon(start, pastWall, diamond)));
        Expect("the target past the wall is not an obstacle (precondition)", !IsObstacle(PastWallTile));
        _closestToObstacle = float.MaxValue;
        float walked = 0;
        Click(MouseButton.Right, pastWall);
        yield return 1;
        for (int i = 0; i < 2400 && PartyMember.IsMoving; i++)
        {
            Vector2 before = PartyMember.GlobalPosition;
            yield return 1;
            walked += before.DistanceTo(PartyMember.GlobalPosition);
        }

        Expect($"arrives past the wall (off by {PartyMember.GlobalPosition.DistanceTo(pastWall):0.0} px)",
            !PartyMember.IsMoving && PartyMember.GlobalPosition.DistanceTo(pastWall) < ArriveTolerance);
        Expect($"walks round the wall ({walked:0} px walked, {start.DistanceTo(pastWall):0} px straight)",
            walked > start.DistanceTo(pastWall) * 1.2f);
        Expect($"never overlaps an obstacle on the way (closest {_closestToObstacle:0.0} px, feet radius {PartyMember.FeetRadius})",
            _closestToObstacle >= PartyMember.FeetRadius - 0.5f);

        Expect("the inside-obstacle target is an obstacle (precondition)", IsObstacle(InsideObstacleTile));
        Vector2 inside = TileCentre(InsideObstacleTile);
        Vector2 closest = NavigationServer2D.MapGetClosestPoint(map, inside);
        _closestToObstacle = float.MaxValue;
        Click(MouseButton.Right, inside);
        yield return 1;
        for (int i = 0; i < 2400 && PartyMember.IsMoving; i++)
        {
            yield return 1;
        }

        Vector2 stopped = PartyMember.GlobalPosition;
        Expect($"right-click inside an obstacle ends at the closest reachable point (off by {stopped.DistanceTo(closest):0.0} px)",
            !PartyMember.IsMoving && stopped.DistanceTo(closest) < ArriveTolerance);
        Expect($"never overlaps an obstacle on the way (closest {_closestToObstacle:0.0} px)",
            _closestToObstacle >= PartyMember.FeetRadius - 0.5f);
        yield return 60;
        Expect("stays still after stopping", PartyMember.GlobalPosition.DistanceTo(stopped) < StillTolerance);
    }

    private bool IsObstacle(Vector2I tile) =>
        _ground.TileSet.GetPhysicsLayersCount() > 0
        && _ground.GetCellTileData(tile) is { } data && data.GetCollisionPolygonsCount(0) > 0;

    private Vector2 TileCentre(Vector2I tile) => _ground.ToGlobal(_ground.MapToLocal(tile));

    /// <summary>The tile's diamond in global coordinates.</summary>
    private Vector2[] TileDiamond(Vector2I tile)
    {
        Vector2 centre = TileCentre(tile);
        return Tile.Diamond().Select(corner => centre + corner).ToArray();
    }

    /// <summary>Distance from a point to the nearest obstacle diamond; 0 if inside one.</summary>
    private float DistanceToObstacles(Vector2 point)
    {
        float best = float.MaxValue;
        foreach (Vector2[] diamond in _obstacles)
        {
            if (Geometry2D.IsPointInPolygon(point, diamond))
            {
                return 0;
            }

            for (int i = 0; i < diamond.Length; i++)
            {
                Vector2 closest = Geometry2D.GetClosestPointToSegment(point, diamond[i], diamond[(i + 1) % diamond.Length]);
                best = Mathf.Min(best, point.DistanceTo(closest));
            }
        }

        return best;
    }

    private static bool SegmentHitsPolygon(Vector2 a, Vector2 b, Vector2[] polygon) =>
        Geometry2D.IntersectPolylineWithPolygon(new[] { a, b }, polygon).Count > 0;

    /// <summary>The bounding box of every used tile's diamond, in screen (viewport) pixels.</summary>
    private Rect2 ZoneScreenRect()
    {
        Transform2D toScreen = Root.GetCanvasTransform();
        Rect2? rect = null;
        foreach (Vector2I tile in _ground.GetUsedCells())
        {
            foreach (Vector2 corner in TileDiamond(tile))
            {
                Vector2 onScreen = toScreen * corner;
                rect = rect is { } soFar ? soFar.Expand(onScreen) : new Rect2(onScreen, Vector2.Zero);
            }
        }

        return rect ?? new Rect2();
    }
}
