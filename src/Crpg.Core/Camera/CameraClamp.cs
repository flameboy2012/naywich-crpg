using Crpg.Core.Selection;

namespace Crpg.Core.Camera;

/// <summary>Keeps the camera's view inside the zone bounds. Every camera move goes through it.</summary>
public static class CameraClamp
{
    /// <summary>Returns the camera centre nearest to the wanted one whose view stays inside the zone bounds.</summary>
    public static WorldPosition Clamp(WorldPosition wanted, ViewSize view, ZoneBounds bounds) =>
        new(ClampAxis(wanted.X, view.Width, bounds.Left, bounds.Right),
            ClampAxis(wanted.Y, view.Height, bounds.Top, bounds.Bottom));

    /// <summary>Clamps one axis. Where the view is larger than the zone bounds, centres on them so the view never jitters.</summary>
    private static float ClampAxis(float wanted, float viewLength, float low, float high)
    {
        if (viewLength >= high - low)
        {
            return (low + high) / 2f;
        }

        float half = viewLength / 2f;
        return Math.Clamp(wanted, low + half, high - half);
    }
}
