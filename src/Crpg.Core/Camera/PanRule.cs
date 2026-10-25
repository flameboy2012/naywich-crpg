namespace Crpg.Core.Camera;

/// <summary>Turns the mouse and the pan keys into the direction the camera pans.</summary>
public static class PanRule
{
    /// <summary>
    /// The edge pan direction for a mouse at a screen point: towards each window edge the mouse is within the edge
    /// width of. In a corner it pans towards both edges.
    /// </summary>
    public static PanDirection EdgeDirection(float mouseX, float mouseY, ViewSize view, float edgeWidth) =>
        new(EdgeAxis(mouseX, view.Width, edgeWidth), EdgeAxis(mouseY, view.Height, edgeWidth));

    /// <summary>
    /// Combines the keyboard and edge pan directions. Each axis is held to -1..1, then a diagonal is shortened to
    /// length 1, so every direction pans at the one pan speed.
    /// </summary>
    public static PanDirection Combine(PanDirection keyboard, PanDirection edge)
    {
        float x = Math.Clamp(keyboard.X + edge.X, -1f, 1f);
        float y = Math.Clamp(keyboard.Y + edge.Y, -1f, 1f);
        float length = MathF.Sqrt(x * x + y * y);
        return length > 1f ? new PanDirection(x / length, y / length) : new PanDirection(x, y);
    }

    /// <summary>-1 near the low edge of one axis, 1 near the high edge, otherwise 0.</summary>
    private static float EdgeAxis(float mouse, float viewLength, float edgeWidth)
    {
        if (mouse < edgeWidth)
        {
            return -1;
        }

        return mouse >= viewLength - edgeWidth ? 1 : 0;
    }
}
