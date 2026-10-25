using Crpg.Core.Camera;
using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class CameraClampTests
{
    /// <summary>Zone bounds 3200x1600 px, from (-1600, -32) to (1600, 1568): the 48x48 test zone plus its margin.</summary>
    private static readonly ZoneBounds Bounds = new(Left: -1600, Top: -32, Right: 1600, Bottom: 1568);

    private static readonly ViewSize View = new(1920, 1080);

    [Fact]
    public void A_centre_well_inside_the_zone_bounds_is_unchanged()
    {
        var wanted = new WorldPosition(100, 700);

        Assert.Equal(wanted, CameraClamp.Clamp(wanted, View, Bounds));
    }

    /// <summary>Half the view is 960x540, so a view touching an edge has its centre 960 px or 540 px inside it.</summary>
    [Theory]
    [InlineData(-5000, 700, -640, 700)]    // past the left edge: view's left touches -1600
    [InlineData(5000, 700, 640, 700)]      // past the right edge: view's right touches 1600
    [InlineData(100, -5000, 100, 508)]     // past the top edge: view's top touches -32
    [InlineData(100, 5000, 100, 1028)]     // past the bottom edge: view's bottom touches 1568
    public void A_centre_past_an_edge_is_pulled_back_so_the_view_touches_that_edge(
        float wantedX, float wantedY, float expectedX, float expectedY)
    {
        WorldPosition clamped = CameraClamp.Clamp(new WorldPosition(wantedX, wantedY), View, Bounds);

        Assert.Equal(new WorldPosition(expectedX, expectedY), clamped);
    }

    [Fact]
    public void A_centre_past_a_corner_is_pulled_back_on_both_axes()
    {
        WorldPosition clamped = CameraClamp.Clamp(new WorldPosition(-5000, 5000), View, Bounds);

        Assert.Equal(new WorldPosition(-640, 1028), clamped);
    }

    [Fact]
    public void On_an_axis_where_the_view_is_larger_than_the_zone_bounds_the_centre_is_the_middle_of_the_zone_bounds()
    {
        // 1000x2000 px zone bounds from (0, 0): narrower than the 1920 px view, taller than the 1080 px view.
        var narrow = new ZoneBounds(Left: 0, Top: 0, Right: 1000, Bottom: 2000);

        WorldPosition clamped = CameraClamp.Clamp(new WorldPosition(-300, 5000), View, narrow);

        Assert.Equal(new WorldPosition(500, 1460), clamped);
    }
}
