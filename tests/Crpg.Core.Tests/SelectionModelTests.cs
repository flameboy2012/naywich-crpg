using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class SelectionModelTests
{
    private static readonly PartyMemberId Aldric = new("aldric");
    private static readonly PartyMemberId Brenna = new("brenna");
    private static readonly PartyMemberId Cerys = new("cerys");

    private static readonly PartyMemberId Cedric = new("cedric");
    private static readonly PartyMemberId Dara = new("dara");

    private static readonly WorldPosition Somewhere = new(10f, 20f);

    /// <summary>One tile of distance: one isometric grid step, (32, 16) in world pixels (ADR 0001).</summary>
    private static readonly float Tile = MathF.Sqrt(32f * 32f + 16f * 16f);

    private static SelectionModel ModelWith(params PartyMemberId[] selected)
    {
        var model = new SelectionModel();
        foreach (PartyMemberId partyMember in selected)
        {
            model.Click(ShiftClickOn(partyMember));
        }

        return model;
    }

    private static float Distance(WorldPosition a, WorldPosition b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static Dictionary<PartyMemberId, WorldPosition> Destinations(ClickResult result) =>
        result.Orders.Cast<MoveOrder>().ToDictionary(order => order.PartyMember, order => order.Destination);

    private static ClickEvent LeftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember, Somewhere));

    private static ClickEvent ShiftClickOn(PartyMemberId partyMember) =>
        new(ClickButton.Left, new ClickTarget.PartyMember(partyMember, Somewhere), Shift: true);

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
    public void A_single_selected_party_member_goes_exactly_to_the_clicked_point()
    {
        var model = new SelectionModel();
        model.Click(LeftClickOn(Aldric));

        ClickResult result = model.Click(RightClickOnGround(300f, 120f, Standing((Aldric, 250f, 90f), (Brenna, 310f, 125f))));

        Assert.Equal(new Order[] { new MoveOrder(Aldric, new WorldPosition(300f, 120f)) }, result.Orders);
    }

    [Fact]
    public void Several_selected_party_members_go_to_distinct_slots_about_a_tile_apart_with_slot_0_at_the_clicked_point()
    {
        SelectionModel model = ModelWith(Aldric, Brenna, Cedric, Dara);
        var clicked = new WorldPosition(300f, 120f);

        ClickResult result = model.Click(RightClickOnGround(clicked.X, clicked.Y,
            Standing((Aldric, 100f, 100f), (Brenna, 120f, 300f), (Cedric, 500f, 80f), (Dara, 420f, 260f))));

        Dictionary<PartyMemberId, WorldPosition> destinations = Destinations(result);
        Assert.Equal(new[] { Aldric, Brenna, Cedric, Dara }, destinations.Keys.Order(Comparer<PartyMemberId>.Create(
            (a, b) => string.CompareOrdinal(a.Value, b.Value))));
        Assert.Single(destinations.Values, destination => destination == clicked);
        foreach (WorldPosition other in destinations.Values.Where(destination => destination != clicked))
        {
            Assert.InRange(Distance(other, clicked), 0.9f * Tile, 1.1f * Tile);
        }

        WorldPosition[] slots = destinations.Values.ToArray();
        for (int i = 0; i < slots.Length; i++)
        {
            for (int j = i + 1; j < slots.Length; j++)
            {
                Assert.True(Distance(slots[i], slots[j]) >= 0.9f * Tile, $"slots {slots[i]} and {slots[j]} are too close");
            }
        }
    }

    [Fact]
    public void Slots_are_assigned_for_the_shortest_total_distance_where_nearest_slot_choices_would_cross()
    {
        // Aldric stands just below the slot a tile above the clicked point. Brenna's nearest slot is that same slot,
        // but sending her there and Aldric to the clicked point walks about 82 px in total; the other way round is
        // about 62 px.
        SelectionModel model = ModelWith(Aldric, Brenna);
        var clicked = new WorldPosition(200f, 200f);

        ClickResult result = model.Click(RightClickOnGround(clicked.X, clicked.Y, Standing((Aldric, 200f, 168f), (Brenna, 250f, 170f))));

        Dictionary<PartyMemberId, WorldPosition> destinations = Destinations(result);
        Assert.Equal(clicked, destinations[Brenna]);
        Assert.Equal(200f, destinations[Aldric].X, 0.01f);
        Assert.Equal(200f - Tile, destinations[Aldric].Y, 0.1f);
    }

    [Fact]
    public void The_same_input_always_gives_the_same_orders()
    {
        // Everyone stands on one spot, so every assignment walks the same total distance: only the tie-break decides.
        IReadOnlyDictionary<PartyMemberId, WorldPosition> standing =
            Standing((Aldric, 50f, 50f), (Brenna, 50f, 50f), (Cedric, 50f, 50f), (Dara, 50f, 50f));
        SelectionModel selectedInOneOrder = ModelWith(Aldric, Brenna, Cedric, Dara);
        SelectionModel selectedInAnother = ModelWith(Dara, Brenna, Aldric, Cedric);

        ClickResult first = selectedInOneOrder.Click(RightClickOnGround(300f, 120f, standing));
        ClickResult again = selectedInOneOrder.Click(RightClickOnGround(300f, 120f, standing));
        ClickResult other = selectedInAnother.Click(RightClickOnGround(300f, 120f, standing));

        Assert.Equal(4, first.Orders.Count);
        Assert.Equal(first.Orders, again.Orders);
        Assert.Equal(first.Orders, other.Orders);
    }

    [Fact]
    public void Right_click_on_a_party_member_sends_the_selection_to_their_position()
    {
        SelectionModel model = ModelWith(Aldric, Brenna);
        IReadOnlyDictionary<PartyMemberId, WorldPosition> standing =
            Standing((Aldric, 100f, 100f), (Brenna, 130f, 110f), (Cedric, 400f, 250f));

        ClickResult result = model.Click(RightClickOn(Cedric, standing));

        Dictionary<PartyMemberId, WorldPosition> destinations = Destinations(result);
        Assert.Equal(2, destinations.Count);
        Assert.Contains(new WorldPosition(400f, 250f), destinations.Values);
        Assert.All(destinations.Values, destination =>
            Assert.True(Distance(destination, new WorldPosition(400f, 250f)) < 1.1f * Tile));
        Assert.Equal(new[] { Aldric, Brenna }, result.Selection.OrderBy(id => id.Value));
    }

    [Fact]
    public void Right_click_with_an_empty_selection_issues_nothing_even_with_party_members_standing_about()
    {
        var model = new SelectionModel();
        IReadOnlyDictionary<PartyMemberId, WorldPosition> standing = Standing((Aldric, 100f, 100f), (Brenna, 130f, 110f));

        ClickResult onGround = model.Click(RightClickOnGround(300f, 120f, standing));
        ClickResult onPartyMember = model.Click(RightClickOn(Brenna, standing));

        Assert.Empty(onGround.Orders);
        Assert.Empty(onPartyMember.Orders);
        Assert.Empty(model.Selection);
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
