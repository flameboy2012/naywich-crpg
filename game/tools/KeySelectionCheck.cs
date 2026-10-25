using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for keyboard selection: loads Main, pushes real key presses through the viewport and checks every
/// party member's selection ring. Keys 1-4 select by portrait slot, shift+number toggles a slot and Ctrl+A selects
/// the whole party; none of them issues an order or pans the camera. Also checks the portrait column: click, shift-click
/// and double-click on a portrait send the same slot events, double-click centres the camera, no portrait click reaches
/// the world, and the portrait highlights match the rings after every kind of selection change. Run from game/ after
/// dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 600 --script res://tools/KeySelectionCheck.cs
/// Prints "KeySelectionCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class KeySelectionCheck : HeadlessCheck
{
    /// <summary>The number keys, by portrait slot: slot 1 is key 1.</summary>
    private static readonly Key[] SlotKeys = { Key.Key1, Key.Key2, Key.Key3, Key.Key4 };

    /// <summary>Frames to wait after a key press that must not move anyone, to catch a stray order.</summary>
    private const int NoMoveFrames = 20;

    /// <summary>How far (px) the portrait column sits from the window's left edge, so edge pan to the left still works.</summary>
    private const float PortraitInset = 16f;

    /// <summary>How close (px) counts as the same camera position.</summary>
    private const float PositionTolerance = 0.5f;

    protected override IEnumerator<int> Run()
    {
        foreach (int step in WaitForNavigationSync())
        {
            yield return step;
        }

        Expect("no ring at start", RingsShow());
        Expect("no portrait highlighted at start", PortraitsShow());
        Vector2[] start = PartyMembers.Select(partyMember => partyMember.GlobalPosition).ToArray();

        // Portrait slot order is the Party node order, which is the spawn order PartyMembers keeps.
        Expect("one portrait per party member", Portraits.Count == PartyMembers.Count);
        Expect("portraits run top to bottom in slot order, in each party member's tint",
            Portraits.Select((portrait, i) => portrait.Tint == PartyMembers[i].Tint
                                              && portrait.Slot == i + 1
                                              && (i == 0 || portrait.GetGlobalRect().Position.Y
                                                  > Portraits[i - 1].GetGlobalRect().End.Y))
                .All(ok => ok));
        Expect("the portrait column is inset 16 px from the left edge",
            Portraits.All(portrait => Mathf.IsEqualApprox(portrait.GetGlobalRect().Position.X, PortraitInset)));

        for (int slot = 1; slot <= PartyMembers.Count; slot++)
        {
            PressKey(SlotKeys[slot - 1]);
            yield return 1;
            Character inSlot = PartyMembers[slot - 1];
            Expect($"key {slot} selects only {inSlot.Name}", RingsShow(inSlot));
            Expect($"key {slot} highlights only portrait {slot}", PortraitsShow(inSlot));
        }

        Character first = PartyMembers[0];
        Character second = PartyMembers[1];
        Character third = PartyMembers[2];
        Character fourth = PartyMembers[3];
        PressKey(Key.Key1);
        PressKey(Key.Key3, shift: true);
        yield return 1;
        Expect("shift+3 adds the party member in slot 3", RingsShow(first, third));
        Expect("shift+3 highlights portraits 1 and 3", PortraitsShow(first, third));

        PressKey(Key.Key1, shift: true);
        yield return 1;
        Expect("shift+1 removes the selected party member in slot 1", RingsShow(third));
        Expect("shift+1 leaves only portrait 3 highlighted", PortraitsShow(third));

        Transform2D canvasBefore = Root.GetCanvasTransform();
        PressKey(Key.A, ctrl: true);
        yield return NoMoveFrames;
        Expect("Ctrl+A selects every party member", RingsShow(PartyMembers.ToArray()));
        Expect("Ctrl+A highlights every portrait", PortraitsShow(PartyMembers.ToArray()));
        Expect("Ctrl+A doesn't pan the camera", Root.GetCanvasTransform() == canvasBefore);
        Expect("no key issued an order", NoneMovedFrom(start));

        ClickPortrait(1, MouseButton.Right);
        yield return NoMoveFrames;
        Expect("right-click on a portrait changes nothing",
            RingsShow(PartyMembers.ToArray()) && PortraitsShow(PartyMembers.ToArray()));

        ClickPortrait(2, MouseButton.Left);
        yield return 1;
        Expect("click on portrait 2 selects only the party member in slot 2", RingsShow(second));
        Expect("click on portrait 2 highlights only portrait 2", PortraitsShow(second));

        ClickPortrait(4, MouseButton.Left, shift: true);
        yield return 1;
        Expect("shift-click on portrait 4 adds the party member in slot 4", RingsShow(second, fourth));
        Expect("shift-click on portrait 4 highlights portraits 2 and 4", PortraitsShow(second, fourth));

        ClickPortrait(2, MouseButton.Left, shift: true);
        yield return 1;
        Expect("shift-click on portrait 2 removes the party member in slot 2", RingsShow(fourth));
        Expect("shift-click on portrait 2 leaves only portrait 4 highlighted", PortraitsShow(fourth));

        // Look somewhere else first, so the double-click has to bring the camera back.
        Rect2 startView = VisibleWorldRect();
        Root.GetNode<CameraRig>("Main/CameraRig").CentreOn(Ground.ToGlobal(Ground.MapToLocal(new Vector2I(40, 40))));
        yield return 2;
        Expect("the camera looks away from the party", !VisibleWorldRect().HasPoint(third.GlobalPosition));

        ClickPortrait(3, MouseButton.Left, doubleClick: true);
        yield return 2;
        Rect2 view = VisibleWorldRect();
        Expect("double-click on portrait 3 selects only the party member in slot 3", RingsShow(third));
        Expect("double-click on portrait 3 highlights only portrait 3", PortraitsShow(third));
        Expect("double-click centres the camera on them horizontally",
            Mathf.Abs(view.GetCenter().X - third.GlobalPosition.X) < PositionTolerance);
        Expect("double-click centring is clamped: the view's top stays at the zone bounds",
            Mathf.Abs(view.Position.Y - startView.Position.Y) < PositionTolerance);
        yield return NoMoveFrames;
        Expect("no portrait click issued an order", NoneMovedFrom(start));

        ClickPartyMember(MouseButton.Left, first);
        yield return 1;
        Expect("a world click moves the highlight to portrait 1", PortraitsShow(first));

        Drag(start.Aggregate(start[0], (min, at) => min.Min(at)) - new Vector2(40, 60),
            start.Aggregate(start[0], (max, at) => max.Max(at)) + new Vector2(40, 20));
        yield return 1;
        Expect("a box select highlights every portrait", PortraitsShow(PartyMembers.ToArray()));

        PressKey(Key.Key2);
        Click(MouseButton.Right, second.GlobalPosition + new Vector2(160, 80));
        yield return NoMoveFrames;
        Expect("a right-click after key 2 moves only the party member in slot 2",
            PartyMembers.Select((partyMember, i) => partyMember.GlobalPosition.DistanceTo(start[i]) >= StillTolerance)
                .SequenceEqual(PartyMembers.Select(partyMember => partyMember == second)));
    }

    /// <summary>Presses and releases a key through the viewport, optionally with shift or Ctrl held.</summary>
    private void PressKey(Key key, bool shift = false, bool ctrl = false)
    {
        foreach (bool pressed in new[] { true, false })
        {
            Root.PushInput(
                new InputEventKey
                {
                    Keycode = key, PhysicalKeycode = key, Pressed = pressed, ShiftPressed = shift, CtrlPressed = ctrl,
                },
                true);
        }
    }

    /// <summary>True when exactly the given party members' rings are visible and every other ring is hidden.</summary>
    private bool RingsShow(params Character[] selected) =>
        PartyMembers.All(partyMember => partyMember.SelectionRingVisible == selected.Contains(partyMember));

    /// <summary>
    /// True when exactly the given party members' portraits are highlighted. Portrait slot order is the spawn order, so
    /// the portrait in slot n belongs to PartyMembers[n - 1].
    /// </summary>
    private bool PortraitsShow(params Character[] selected) =>
        Portraits.Count == PartyMembers.Count
        && Portraits.Select((portrait, i) => portrait.Highlighted == selected.Contains(PartyMembers[i])).All(ok => ok);

    /// <summary>The portraits, top to bottom.</summary>
    private IReadOnlyList<Portrait> Portraits =>
        Root.GetNode("Main/UI/PortraitColumn").GetChildren().OfType<Portrait>().ToList();

    /// <summary>
    /// Presses and releases a mouse button on the middle of a portrait, through the viewport. A double-click is two
    /// clicks, the second press marked as a double-click, as Godot sends it.
    /// </summary>
    private void ClickPortrait(int slot, MouseButton button, bool shift = false, bool doubleClick = false)
    {
        Vector2 at = Portraits[slot - 1].GetGlobalRect().GetCenter();
        foreach (bool second in doubleClick ? new[] { false, true } : new[] { false })
        {
            foreach (bool pressed in new[] { true, false })
            {
                Root.PushInput(
                    new InputEventMouseButton
                    {
                        ButtonIndex = button, Pressed = pressed, Position = at, ShiftPressed = shift,
                        DoubleClick = second && pressed,
                    },
                    true);
            }
        }
    }

    /// <summary>True when no party member has moved from where they stood.</summary>
    private bool NoneMovedFrom(Vector2[] start) =>
        PartyMembers.Select((partyMember, i) => partyMember.GlobalPosition.DistanceTo(start[i]))
            .All(distance => distance < StillTolerance);

    /// <summary>The world rectangle the camera shows.</summary>
    private Rect2 VisibleWorldRect() => Root.GetCanvasTransform().AffineInverse() * Root.GetVisibleRect();
}
