namespace Crpg.Core.Tests;

public class TileScaleTests
{
    [Fact]
    public void Tiles_are_64_by_32_px_and_one_tile_of_distance_is_a_grid_step_of_about_35_78_px()
    {
        Assert.Equal(64, TileScale.Width);
        Assert.Equal(32, TileScale.Height);
        Assert.Equal(35.78f, TileScale.GridStepLength, 0.01f);
    }
}
