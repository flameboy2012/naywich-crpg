namespace Crpg.Core.Camera;

/// <summary>
/// The rectangle the camera may show, in zone world pixels: the bounding rectangle of the zone's ground tiles, grown by
/// a margin on every side.
/// </summary>
public readonly record struct ZoneBounds(float Left, float Top, float Right, float Bottom);
