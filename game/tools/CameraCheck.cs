using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for the camera: loads Main and checks where the camera starts and what it shows. Run from game/
/// after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 300 --script res://tools/CameraCheck.cs
/// Prints "CameraCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class CameraCheck : HeadlessCheck
{
    /// <summary>The zone bounds margin outside the ground tiles: one tile's screen size, split between the sides.</summary>
    private static readonly Vector2 ZoneBoundsMargin = new(64, 32);

    /// <summary>How close (px) counts as the same camera position.</summary>
    private const float PositionTolerance = 0.5f;

    protected override IEnumerator<int> Run()
    {
        Camera2D camera = Root.GetNode<Camera2D>("Main/CameraRig/Camera2D");
        Rect2 view = VisibleWorldRect(camera);
        Rect2 bounds = GroundRect().GrowIndividual(
            ZoneBoundsMargin.X, ZoneBoundsMargin.Y, ZoneBoundsMargin.X, ZoneBoundsMargin.Y);
        Vector2 partyCentre = PartyMembers.Aggregate(Vector2.Zero, (sum, partyMember) => sum + partyMember.GlobalPosition)
                              / PartyMembers.Count;
        GD.Print($"view {view}, zone bounds {bounds}, party centre {partyCentre}");

        Expect("the zone is 48x48 tiles", Ground.GetUsedRect() == new Rect2I(0, 0, 48, 48));
        Expect("the zone bounds are larger than the view on both axes",
            bounds.Size.X > view.Size.X && bounds.Size.Y > view.Size.Y);
        Expect("the view starts inside the zone bounds", bounds.Encloses(view));
        Expect("the camera starts centred on the party horizontally",
            Mathf.Abs(view.GetCenter().X - partyCentre.X) < PositionTolerance);

        // The party starts near the top of the zone, closer to it than half a view, so the clamp pulls the view down
        // until its top touches the zone bounds.
        Expect("the party starts closer to the top of the zone bounds than half a view",
            partyCentre.Y - bounds.Position.Y < view.Size.Y / 2);
        Expect("the camera start is clamped: the view's top touches the zone bounds",
            Mathf.Abs(view.Position.Y - bounds.Position.Y) < PositionTolerance);

        Expect("the area outside the zone is black",
            ProjectSettings.GetSetting("rendering/environment/defaults/default_clear_color").AsColor() == Colors.Black);
        yield break;
    }

    /// <summary>The world rectangle the camera shows: the viewport's visible rect seen through the canvas transform.</summary>
    private Rect2 VisibleWorldRect(Camera2D camera)
    {
        Rect2 screen = camera.GetViewport().GetVisibleRect();
        return Root.GetCanvasTransform().AffineInverse() * screen;
    }

    /// <summary>The bounding rectangle of the ground tiles' diamonds, in world pixels.</summary>
    private Rect2 GroundRect()
    {
        Rect2I used = Ground.GetUsedRect();
        Vector2 halfTile = Ground.TileSet.TileSize / 2;
        var rect = new Rect2(Ground.ToGlobal(Ground.MapToLocal(used.Position)), Vector2.Zero);
        foreach (Vector2I cell in Ground.GetUsedCells())
        {
            Vector2 centre = Ground.ToGlobal(Ground.MapToLocal(cell));
            rect = rect.Expand(centre - halfTile).Expand(centre + halfTile);
        }

        return rect;
    }
}
