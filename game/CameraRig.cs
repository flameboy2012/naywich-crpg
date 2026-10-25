using Crpg.Core.Camera;
using Godot;

namespace Crpg;

/// <summary>
/// Owns the camera. Every camera move goes through <see cref="CentreOn"/>, which keeps the view inside the zone bounds
/// with the Core camera clamp. The Camera2D child sits at the rig's origin; moving the rig moves the view.
/// </summary>
public partial class CameraRig : Node2D
{
    /// <summary>
    /// How far (px) the zone bounds reach past the ground tiles, left and right (X) and top and bottom (Y). Starts at
    /// one tile's screen size, 64x32, split as 64 px a side and 32 px top and bottom. Provisional until the play-test.
    /// </summary>
    [Export]
    public Vector2 ZoneBoundsMargin { get; set; } = new(64, 32);

    private ZoneBounds _bounds;

    /// <summary>Sets the zone bounds from a zone's ground layer: its tiles' bounding rectangle, grown by the margin.</summary>
    public void SetZone(TileMapLayer ground)
    {
        Rect2 rect = GroundRect(ground).GrowIndividual(
            ZoneBoundsMargin.X, ZoneBoundsMargin.Y, ZoneBoundsMargin.X, ZoneBoundsMargin.Y);
        _bounds = new ZoneBounds(rect.Position.X, rect.Position.Y, rect.End.X, rect.End.Y);
    }

    /// <summary>Centres the camera on a world point, as near as the zone bounds allow.</summary>
    public void CentreOn(Vector2 point)
    {
        Vector2 viewSize = GetViewport().GetVisibleRect().Size;
        GlobalPosition = CameraClamp.Clamp(point.ToWorldPosition(), new ViewSize(viewSize.X, viewSize.Y), _bounds)
            .ToVector2();
    }

    /// <summary>The bounding rectangle of the ground tiles' diamonds, in world pixels.</summary>
    private static Rect2 GroundRect(TileMapLayer ground)
    {
        Vector2 halfTile = ground.TileSet.TileSize / 2;
        Rect2? rect = null;
        foreach (Vector2I cell in ground.GetUsedCells())
        {
            Vector2 centre = ground.ToGlobal(ground.MapToLocal(cell));
            var tile = new Rect2(centre - halfTile, halfTile * 2);
            rect = rect?.Merge(tile) ?? tile;
        }

        return rect ?? new Rect2(ground.GlobalPosition, Vector2.Zero);
    }
}
