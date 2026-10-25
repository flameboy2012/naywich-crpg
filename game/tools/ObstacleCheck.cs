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
public partial class ObstacleCheck : SceneTree
{
    /// <summary>A cell on the far side of the wall from the party member's start.</summary>
    private static readonly Vector2I PastWallCell = new(2, 10);

    /// <summary>The middle cell of an obstacle block, unreachable.</summary>
    private static readonly Vector2I InsideObstacleCell = new(17, 5);

    /// <summary>How close (px) counts as "arrived": the agent's target distance plus one physics step.</summary>
    private const float ArriveTolerance = 6f;

    /// <summary>Radius of the party member's feet collision circle (Character.tscn).</summary>
    private const float FeetRadius = 8f;

    private static readonly Vector2 ViewSize = new(1920, 1080);

    private Character _partyMember = null!;
    private TileMapLayer _ground = null!;
    private List<Vector2[]> _obstacles = null!;
    private IEnumerator<int> _script = null!;
    private int _wait;
    private bool _failed;
    private float _closestToObstacle = float.MaxValue;

    public override void _Initialize()
    {
        Root.AddChild(GD.Load<PackedScene>("res://Main.tscn").Instantiate());
        _script = Run();
        _wait = 5;   // let Main load the zone and the navigation map sync
    }

    public override bool _PhysicsProcess(double delta)
    {
        if (_obstacles is not null)
        {
            _closestToObstacle = Mathf.Min(_closestToObstacle, DistanceToObstacles(_partyMember.GlobalPosition));
        }

        if (--_wait > 0)
        {
            return false;
        }

        if (_script.MoveNext())
        {
            _wait = _script.Current;
        }
        else
        {
            GD.Print(_failed ? "ObstacleCheck: FAILED" : "ObstacleCheck: PASSED");
            Quit(_failed ? 1 : 0);
        }

        return false;
    }

    /// <summary>The check, as steps. Each yield is the number of physics frames to wait before the next step.</summary>
    private IEnumerator<int> Run()
    {
        _partyMember = Root.GetNode<Character>("Main/ZoneContainer/TestZone/PartyMember1");
        _ground = Root.GetNode<TileMapLayer>("Main/ZoneContainer/TestZone/NavigationRegion2D/Ground");
        List<Vector2I> obstacleCells = _ground.GetUsedCells().Where(IsObstacle).ToList();
        _obstacles = obstacleCells.Select(CellDiamond).ToList();
        Rid map = _ground.GetParent<NavigationRegion2D>().GetNavigationMap();
        Vector2 groundCentre = CellCentre(_ground.LocalToMap(_ground.ToLocal(_partyMember.GlobalPosition)));
        for (int i = 0; i < 120 && NavigationServer2D.MapGetClosestPoint(map, groundCentre) == Vector2.Zero; i++)
        {
            yield return 1;   // map queries return (0, 0) until the map has synced with the zone's region
        }

        Vector2 groundClosest = NavigationServer2D.MapGetClosestPoint(map, groundCentre);
        Expect($"ground under the party member is on the navigation mesh ({groundCentre} -> {groundClosest})",
            groundClosest.DistanceTo(groundCentre) < 0.5f);
        Expect($"zone has obstacle tiles ({obstacleCells.Count})", obstacleCells.Count > 0);
        Expect("every obstacle tile is a hole in the navigation mesh, with the feet-radius margin", obstacleCells.All(c =>
            DistanceToObstacles(NavigationServer2D.MapGetClosestPoint(map, CellCentre(c))) >= FeetRadius - 0.5f));

        Rect2 zoneOnScreen = ZoneScreenRect();
        Expect($"whole zone fits the 1920x1080 view with the static camera ({zoneOnScreen})",
            new Rect2(Vector2.Zero, ViewSize).Encloses(zoneOnScreen) && Root.GetVisibleRect().Size == ViewSize);

        Vector2 start = _partyMember.GlobalPosition;
        Vector2 onObstacle = CellCentre(obstacleCells[0]);
        Click(MouseButton.Right, onObstacle);
        yield return 30;
        Expect("right-click on an obstacle with nothing selected does nothing",
            _partyMember.GlobalPosition.DistanceTo(start) < 0.01f && !_partyMember.IsMoving);
        Click(MouseButton.Left, start + new Vector2(0, -24));
        yield return 2;
        Expect("left-click selects them", _partyMember.SelectionRingVisible);
        Click(MouseButton.Left, onObstacle);
        yield return 2;
        Expect("left-click on an obstacle counts as ground and clears the selection", !_partyMember.SelectionRingVisible);

        Click(MouseButton.Left, start + new Vector2(0, -24));
        yield return 2;
        Expect("left-click selects them again", _partyMember.SelectionRingVisible);

        Vector2 pastWall = CellCentre(PastWallCell);
        Expect("the straight line to the target crosses an obstacle (precondition)",
            _obstacles.Any(d => SegmentHitsPolygon(start, pastWall, d)));
        Expect("the target past the wall is not an obstacle (precondition)", !IsObstacle(PastWallCell));
        _closestToObstacle = float.MaxValue;
        float walked = 0;
        Click(MouseButton.Right, pastWall);
        yield return 1;
        for (int i = 0; i < 2400 && _partyMember.IsMoving; i++)
        {
            Vector2 before = _partyMember.GlobalPosition;
            yield return 1;
            walked += before.DistanceTo(_partyMember.GlobalPosition);
        }

        Expect($"arrives past the wall (off by {_partyMember.GlobalPosition.DistanceTo(pastWall):0.0} px)",
            !_partyMember.IsMoving && _partyMember.GlobalPosition.DistanceTo(pastWall) < ArriveTolerance);
        Expect($"walks round the wall ({walked:0} px walked, {start.DistanceTo(pastWall):0} px straight)",
            walked > start.DistanceTo(pastWall) * 1.2f);
        Expect($"never overlaps an obstacle on the way (closest {_closestToObstacle:0.0} px, feet radius {FeetRadius})",
            _closestToObstacle >= FeetRadius - 0.5f);

        Expect("the inside-obstacle target is an obstacle (precondition)", IsObstacle(InsideObstacleCell));
        Vector2 inside = CellCentre(InsideObstacleCell);
        Vector2 closest = NavigationServer2D.MapGetClosestPoint(map, inside);
        _closestToObstacle = float.MaxValue;
        Click(MouseButton.Right, inside);
        yield return 1;
        for (int i = 0; i < 2400 && _partyMember.IsMoving; i++)
        {
            yield return 1;
        }

        Vector2 stopped = _partyMember.GlobalPosition;
        Expect($"right-click inside an obstacle ends at the closest reachable point (off by {stopped.DistanceTo(closest):0.0} px)",
            !_partyMember.IsMoving && stopped.DistanceTo(closest) < ArriveTolerance);
        Expect($"never overlaps an obstacle on the way (closest {_closestToObstacle:0.0} px)",
            _closestToObstacle >= FeetRadius - 0.5f);
        yield return 60;
        Expect("stays still after stopping", _partyMember.GlobalPosition.DistanceTo(stopped) < 0.01f);
    }

    private bool IsObstacle(Vector2I cell) =>
        _ground.TileSet.GetPhysicsLayersCount() > 0
        && _ground.GetCellTileData(cell) is { } data && data.GetCollisionPolygonsCount(0) > 0;

    private Vector2 CellCentre(Vector2I cell) => _ground.ToGlobal(_ground.MapToLocal(cell));

    /// <summary>The cell's diamond in global coordinates.</summary>
    private Vector2[] CellDiamond(Vector2I cell)
    {
        Vector2 c = CellCentre(cell);
        Vector2 half = _ground.TileSet.TileSize / 2;
        return new[] { c + new Vector2(-half.X, 0), c + new Vector2(0, -half.Y), c + new Vector2(half.X, 0), c + new Vector2(0, half.Y) };
    }

    /// <summary>Distance from a point to the nearest obstacle diamond; 0 if inside one.</summary>
    private float DistanceToObstacles(Vector2 p)
    {
        float best = float.MaxValue;
        foreach (Vector2[] d in _obstacles)
        {
            if (Geometry2D.IsPointInPolygon(p, d))
            {
                return 0;
            }

            for (int i = 0; i < d.Length; i++)
            {
                Vector2 q = Geometry2D.GetClosestPointToSegment(p, d[i], d[(i + 1) % d.Length]);
                best = Mathf.Min(best, p.DistanceTo(q));
            }
        }

        return best;
    }

    private static bool SegmentHitsPolygon(Vector2 a, Vector2 b, Vector2[] polygon) =>
        Geometry2D.IntersectPolylineWithPolygon(new[] { a, b }, polygon).Count > 0;

    /// <summary>The bounding box of every used cell's diamond, in screen (viewport) pixels.</summary>
    private Rect2 ZoneScreenRect()
    {
        Transform2D toScreen = Root.GetCanvasTransform();
        Rect2? rect = null;
        foreach (Vector2I cell in _ground.GetUsedCells())
        {
            foreach (Vector2 corner in CellDiamond(cell))
            {
                Vector2 s = toScreen * corner;
                rect = rect is { } r ? r.Expand(s) : new Rect2(s, Vector2.Zero);
            }
        }

        return rect ?? new Rect2();
    }

    private void Click(MouseButton button, Vector2 world)
    {
        Vector2 screen = Root.GetCanvasTransform() * world;
        Root.PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = screen }, true);
        Root.PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = screen }, true);
    }

    private void Expect(string name, bool ok)
    {
        if (ok)
        {
            GD.Print($"ok: {name}");
        }
        else
        {
            _failed = true;
            GD.PushError($"ObstacleCheck: {name}: failed at {_partyMember.GlobalPosition}");
        }
    }
}
