namespace Crpg.Core.Camera;

/// <summary>The size of the camera's view, in world pixels. There is no zoom, so it is the viewport size.</summary>
public readonly record struct ViewSize(float Width, float Height);
