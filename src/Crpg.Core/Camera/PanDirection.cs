namespace Crpg.Core.Camera;

/// <summary>Which way the camera pans, in screen axes: X right, Y down. A length of 1 pans at full pan speed.</summary>
public readonly record struct PanDirection(float X, float Y)
{
    public static readonly PanDirection None = new(0, 0);
}
