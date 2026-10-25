using Crpg.Core.Movement;

namespace Crpg.Core.Tests;

public class ArrivalRuleTests
{
    private const float Step = 1f / 60f;

    private static ArrivalRule Rule() => new(arrivalDistance: 18f, noProgressTimeout: 0.5f);

    /// <summary>Feeds the same distance for a number of physics frames; true once any frame reports arrival.</summary>
    private static bool Hold(ArrivalRule rule, float distance, int frames)
    {
        bool arrived = false;
        for (int i = 0; i < frames; i++)
        {
            arrived |= rule.Update(distance, Step);
        }

        return arrived;
    }

    [Fact]
    public void Stuck_close_to_the_destination_for_the_timeout_counts_as_arrived()
    {
        ArrivalRule rule = Rule();

        Assert.False(Hold(rule, 10f, 29));
        Assert.True(Hold(rule, 10f, 2));
    }

    [Fact]
    public void Stuck_further_than_the_arrival_distance_never_counts_as_arrived()
    {
        ArrivalRule rule = Rule();

        Assert.False(Hold(rule, 30f, 600));
    }

    [Fact]
    public void Time_stalled_outside_the_arrival_distance_does_not_count_once_back_inside()
    {
        ArrivalRule rule = Rule();

        // Gets close, is pushed back out and stalls there for a second, then drifts back in without beating the
        // closest distance: the timeout starts again from the moment they are back inside.
        Assert.False(Hold(rule, 16f, 1));
        Assert.False(Hold(rule, 30f, 60));
        Assert.False(Hold(rule, 16.5f, 29));
        Assert.True(Hold(rule, 16.5f, 2));
    }

    [Fact]
    public void Walking_steadily_towards_the_destination_never_counts_as_arrived()
    {
        ArrivalRule rule = Rule();
        bool arrived = false;

        // Half a tile per second (about 0.3 px a frame, slowed by steering), from 17 px out to 1 px out: about 0.9 s.
        for (float distance = 17f; distance > 1f; distance -= 0.3f)
        {
            arrived |= rule.Update(distance, Step);
        }

        Assert.False(arrived);
    }

    [Fact]
    public void Jittering_against_an_occupied_slot_counts_as_arrived_after_the_timeout()
    {
        ArrivalRule rule = Rule();
        bool arrivedEarly = false;
        bool arrived = false;

        // Pushed back and forth between 12 px and 14 px out every frame, never getting any closer.
        for (int frame = 0; frame < 40; frame++)
        {
            bool now = rule.Update(frame % 2 == 0 ? 12f : 14f, Step);
            if (frame < 29)
            {
                arrivedEarly |= now;
            }

            arrived |= now;
        }

        Assert.False(arrivedEarly);
        Assert.True(arrived);
    }
}
