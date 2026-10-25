using Crpg.Core.Camera;

namespace Crpg.Core.Tests;

public class PanRuleTests
{
    private static readonly ViewSize View = new(1920, 1080);

    private const float EdgeWidth = 8;

    [Fact]
    public void A_mouse_away_from_every_window_edge_doesnt_pan()
    {
        Assert.Equal(PanDirection.None, PanRule.EdgeDirection(960, 540, View, EdgeWidth));
    }

    /// <summary>The edge band is the 8 px nearest each window edge: pixels 0-7 and 1912-1919 across, 0-7 and 1072-1079 down.</summary>
    [Theory]
    [InlineData(0, 540, -1, 0)]       // left edge
    [InlineData(7.5f, 540, -1, 0)]    // just inside the left band
    [InlineData(8, 540, 0, 0)]        // just past the left band
    [InlineData(1919, 540, 1, 0)]     // right edge
    [InlineData(1912, 540, 1, 0)]     // just inside the right band
    [InlineData(1911.5f, 540, 0, 0)]  // just past the right band
    [InlineData(960, 0, 0, -1)]       // top edge
    [InlineData(960, 1079, 0, 1)]     // bottom edge
    public void A_mouse_within_the_edge_width_of_a_window_edge_pans_towards_it(
        float mouseX, float mouseY, float expectedX, float expectedY)
    {
        Assert.Equal(new PanDirection(expectedX, expectedY), PanRule.EdgeDirection(mouseX, mouseY, View, EdgeWidth));
    }

    [Fact]
    public void A_mouse_in_a_corner_pans_towards_both_edges()
    {
        Assert.Equal(new PanDirection(1, -1), PanRule.EdgeDirection(1919, 0, View, EdgeWidth));
    }

    [Fact]
    public void One_held_direction_pans_at_full_speed_that_way()
    {
        Assert.Equal(new PanDirection(0, 1), PanRule.Combine(new PanDirection(0, 1), PanDirection.None));
    }

    /// <summary>A diagonal keeps the one pan speed: each axis is 1/sqrt(2) ≈ 0.7071.</summary>
    [Fact]
    public void Two_held_directions_combine_into_a_diagonal_at_the_same_speed()
    {
        PanDirection combined = PanRule.Combine(new PanDirection(-1, 0), new PanDirection(0, 1));

        Assert.Equal(-0.7071f, combined.X, 0.0001f);
        Assert.Equal(0.7071f, combined.Y, 0.0001f);
    }

    [Fact]
    public void Keyboard_and_edge_pan_the_same_way_dont_add_up_to_more_than_full_speed()
    {
        Assert.Equal(new PanDirection(1, 0), PanRule.Combine(new PanDirection(1, 0), new PanDirection(1, 0)));
    }

    [Fact]
    public void Opposite_directions_cancel_out()
    {
        Assert.Equal(PanDirection.None, PanRule.Combine(new PanDirection(-1, 0), new PanDirection(1, 0)));
    }
}
