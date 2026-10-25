using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class SelectionModelTests
{
    private static readonly PartyMemberId Aldric = new("aldric");
    private static readonly PartyMemberId Brenna = new("brenna");
    private static readonly PartyMemberId Cerys = new("cerys");

    private static readonly WorldPosition Somewhere = new(10f, 20f);

    private static ClickEvent LeftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember, Somewhere));

    private static ClickEvent ShiftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember, Somewhere), Shift: true);

    private static ClickEvent RightClickOn(PartyMemberId partyMember, float x, float y) =>
        new(ClickButton.Right, new ClickTarget.PartyMember(partyMember, new WorldPosition(x, y)));

    private static ClickEvent RightClickOnGround(float x, float y) =>
        new(ClickButton.Right, new ClickTarget.Ground(new WorldPosition(x, y)));

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
    public void Shift_click_on_an_unselected_party_member_adds_them_to_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(ShiftClickOn(Brenna));

        Assert.Equal(new[] { Aldric, Brenna }, result.Selection.OrderBy(id => id.Value));
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Shift_click_on_a_selected_party_member_removes_them_from_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));
        model.Click(ShiftClickOn(Brenna));

        ClickResult result = model.Click(ShiftClickOn(Aldric));

        Assert.Equal(new[] { Brenna }, result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Shift_click_on_ground_leaves_the_selection_unchanged()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));
        model.Click(ShiftClickOn(Brenna));

        ClickResult result = model.Click(
            new ClickEvent(ClickButton.Left, new ClickTarget.Ground(new WorldPosition(100f, 50f)), Shift: true));

        Assert.Equal(new[] { Aldric, Brenna }, result.Selection.OrderBy(id => id.Value));
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

    [Fact]
    public void Box_select_replaces_the_selection_with_the_party_members_inside_the_box()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.BoxSelect(new BoxSelectEvent(new[] { Brenna, Cerys }));

        Assert.Equal(new[] { Brenna, Cerys }, result.Selection.OrderBy(id => id.Value));
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Shift_box_select_adds_the_party_members_inside_the_box_to_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.BoxSelect(new BoxSelectEvent(new[] { Brenna, Cerys }, Shift: true));

        Assert.Equal(new[] { Aldric, Brenna, Cerys }, result.Selection.OrderBy(id => id.Value));
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void An_empty_box_select_clears_the_selection()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.BoxSelect(new BoxSelectEvent(Array.Empty<PartyMemberId>()));

        Assert.Empty(result.Selection);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void An_empty_shift_box_select_leaves_the_selection_unchanged()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));
        model.Click(ShiftClickOn(Brenna));

        ClickResult result = model.BoxSelect(new BoxSelectEvent(Array.Empty<PartyMemberId>(), Shift: true));

        Assert.Equal(new[] { Aldric, Brenna }, result.Selection.OrderBy(id => id.Value));
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Box_select_issues_no_orders_even_with_party_members_already_selected()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));
        model.Click(RightClickOnGround(64f, 32f));

        ClickResult plain = model.BoxSelect(new BoxSelectEvent(new[] { Aldric, Brenna }));
        ClickResult shift = model.BoxSelect(new BoxSelectEvent(new[] { Cerys }, Shift: true));

        Assert.Empty(plain.Orders);
        Assert.Empty(shift.Orders);
    }
}
