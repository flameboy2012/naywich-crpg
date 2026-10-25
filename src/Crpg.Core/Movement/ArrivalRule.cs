namespace Crpg.Core.Movement;

/// <summary>
/// Decides when a move that can't reach its exact destination is finished anyway: the party member is close to the
/// destination and has stopped getting closer. Covers spread slots another party member stands on and slots that
/// snapped together near a wall, so nobody jitters forever. The navigation agent's own target distance still ends
/// ordinary moves; this rule only adds a second way to finish.
/// </summary>
public sealed class ArrivalRule
{
    /// <summary>
    /// How much closer (world px) than ever before the party member must get for it to count as progress. Sliding
    /// sideways around another party member, or rocking back and forth against them, doesn't count.
    /// </summary>
    public const float MeaningfulProgress = 1f;

    private readonly float _arrivalDistance;
    private readonly float _noProgressTimeout;
    private float _closest = float.PositiveInfinity;
    private float _stalledFor;

    /// <param name="arrivalDistance">How close (world px) to the destination a stalled move must be to count as arrived.</param>
    /// <param name="noProgressTimeout">How long (s) without progress counts as stalled.</param>
    public ArrivalRule(float arrivalDistance, float noProgressTimeout)
    {
        _arrivalDistance = arrivalDistance;
        _noProgressTimeout = noProgressTimeout;
    }

    /// <summary>
    /// Call once per physics frame while moving, with the distance left to the destination and the frame time.
    /// Returns true once the party member has spent the timeout inside the arrival distance without progress.
    /// </summary>
    public bool Update(float distanceToDestination, float delta)
    {
        if (distanceToDestination < _closest - MeaningfulProgress)
        {
            _closest = distanceToDestination;
            _stalledFor = 0f;
            return false;
        }

        // Only time stalled inside the arrival distance counts; stepping back out starts the wait again.
        if (distanceToDestination > _arrivalDistance)
        {
            _stalledFor = 0f;
            return false;
        }

        _stalledFor += delta;
        return _stalledFor >= _noProgressTimeout;
    }
}
