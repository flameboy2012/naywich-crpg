using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class SelectionModelTests
{
    private static readonly PartyMemberId Aldric = new("aldric");
    private static readonly PartyMemberId Brenna = new("brenna");

    private static readonly WorldPosition Somewhere = new(10f, 20f);

    private static ClickEvent LeftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember, Somewhere));

    private static ClickEvent RightClickOn(PartyMemberId partyMember, float x, float y) =>
        new(ClickButton.Right, new ClickTarget.PartyMember(partyMember, new WorldPosition(x, y)));

    private static ClickEvent RightClickOnGround(float x, float y) =>
        new(ClickButton.Right, new ClickTarget.Ground(new WorldPosition(x, y)));

    private static ClickEvent RightClickOnGround(float x, float y, IReadOnlyDictionary<PartyMemberId, WorldPosition> standing) =>
        new(ClickButton.Right, new ClickTarget.Ground(new WorldPosition(x, y))) { PartyMemberPositions = standing };

    private static ClickEvent RightClickOn(
        PartyMemberId partyMember, IReadOnlyDictionary<PartyMemberId, WorldPosition> standing) =>
        new(ClickButton.Right, new ClickTarget.PartyMember(partyMember, standing[partyMember])) { PartyMemberPositions = standing };

    /// <summary>Where each party member currently stands, as the right-click event carries it.</summary>
    private static IReadOnlyDictionary<PartyMemberId, WorldPosition> Standing(
        params (PartyMemberId Id, float X, float Y)[] partyMembers) =>
        partyMembers.ToDictionary(p => p.Id, p => new WorldPosition(p.X, p.Y));

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

    [Fact]
    public void Right_click_on_ground_with_a_selection_issues_a_move_order_to_the_clicked_position()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(RightClickOnGround(300f, 120f));

        Assert.Equal(new Order[] { new MoveOrder(Aldric, new WorldPosition(300f, 120f)) }, result.Orders);
    }

    [Fact]
    public void A_single_selected_party_member_goes_exactly_to_the_clicked_point()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(RightClickOnGround(300f, 120f, Standing((Aldric, 250f, 90f), (Brenna, 310f, 125f))));

        Assert.Equal(new Order[] { new MoveOrder(Aldric, new WorldPosition(300f, 120f)) }, result.Orders);
    }

    [Fact]
    public void Right_click_on_ground_with_an_empty_selection_issues_no_orders()
    {
        var model = new SelectionModel();

        ClickResult result = model.Click(RightClickOnGround(300f, 120f));

        Assert.Empty(result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Right_click_does_not_change_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult onGround = model.Click(RightClickOnGround(300f, 120f));
        ClickResult onPartyMember = model.Click(RightClickOn(Brenna, 64f, 32f));

        Assert.Equal(new[] { Aldric }, onGround.Selection);
        Assert.Equal(new[] { Aldric }, onPartyMember.Selection);
        Assert.Equal(new[] { Aldric }, model.Selection);
    }

    [Fact]
    public void A_second_right_click_issues_a_new_move_order_to_the_new_position()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));
        model.Click(RightClickOnGround(300f, 120f));

        ClickResult result = model.Click(RightClickOnGround(-40f, 500f));

        Assert.Equal(new Order[] { new MoveOrder(Aldric, new WorldPosition(-40f, 500f)) }, result.Orders);
    }

    [Fact]
    public void Right_click_on_a_party_member_with_a_selection_issues_a_move_order_to_their_position()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(RightClickOn(Brenna, 64f, 32f));

        Assert.Equal(new Order[] { new MoveOrder(Aldric, new WorldPosition(64f, 32f)) }, result.Orders);
        Assert.Equal(new[] { Aldric }, result.Selection);
    }

    [Fact]
    public void Right_click_on_a_party_member_with_an_empty_selection_issues_no_orders()
    {
        var model = new SelectionModel();

        ClickResult result = model.Click(RightClickOn(Brenna, 64f, 32f));

        Assert.Empty(result.Selection);
        Assert.Empty(result.Orders);
    }
}
