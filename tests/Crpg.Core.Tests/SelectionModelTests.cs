using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class SelectionModelTests
{
    private static readonly PartyMemberId Aldric = new("aldric");
    private static readonly PartyMemberId Brenna = new("brenna");

    private static ClickEvent LeftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember));

    [Fact]
    public void Left_click_on_a_party_member_selects_only_them()
    {
        var model = new SelectionModel();

        ClickResult result = model.Click(LeftClickOn(Aldric));

        Assert.Equal(new[] { Aldric }, result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Left_click_on_a_different_party_member_replaces_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(LeftClickOn(Brenna));

        Assert.Equal(new[] { Brenna }, result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Left_click_on_the_already_selected_party_member_keeps_them_selected()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(LeftClickOn(Aldric));

        Assert.Equal(new[] { Aldric }, result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Left_click_on_ground_clears_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(new ClickEvent(ClickButton.Left, new ClickTarget.Ground(new WorldPosition(100f, 50f))));

        Assert.Empty(result.Selection);
        Assert.Empty(result.Orders);
    }
}
