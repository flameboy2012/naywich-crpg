using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for keyboard selection: loads Main, pushes real key presses through the viewport and checks every
/// party member's selection ring. Keys 1-4 select by portrait slot, shift+number toggles a slot and Ctrl+A selects
/// the whole party; none of them issues an order or pans the camera. Run from game/ after dotnet build:
///   godot --headless --path . --fixed-fps 60 --quit-after 600 --script res://tools/KeySelectionCheck.cs
/// Prints "KeySelectionCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class KeySelectionCheck : HeadlessCheck
{
    /// <summary>The number keys, by portrait slot: slot 1 is key 1.</summary>
    private static readonly Key[] SlotKeys = { Key.Key1, Key.Key2, Key.Key3, Key.Key4 };

    /// <summary>Frames to wait after a key press that must not move anyone, to catch a stray order.</summary>
    private const int NoMoveFrames = 20;

    protected override IEnumerator<int> Run()
    {
        foreach (int step in WaitForNavigationSync())
        {
            yield return step;
        }

        Expect("no ring at start", RingsShow());
        Vector2[] start = PartyMembers.Select(partyMember => partyMember.GlobalPosition).ToArray();

        // Portrait slot order is the Party node order, which is the spawn order PartyMembers keeps.
        for (int slot = 1; slot <= PartyMembers.Count; slot++)
        {
            PressKey(SlotKeys[slot - 1]);
            yield return 1;
            Character inSlot = PartyMembers[slot - 1];
            Expect($"key {slot} selects only {inSlot.Name}", RingsShow(inSlot));
        }

        Character first = PartyMembers[0];
        Character second = PartyMembers[1];
        Character third = PartyMembers[2];
        PressKey(Key.Key1);
        PressKey(Key.Key3, shift: true);
        yield return 1;
        Expect("shift+3 adds the party member in slot 3", RingsShow(first, third));

        PressKey(Key.Key1, shift: true);
        yield return 1;
        Expect("shift+1 removes the selected party member in slot 1", RingsShow(third));

        Transform2D canvasBefore = Root.GetCanvasTransform();
        PressKey(Key.A, ctrl: true);
        yield return NoMoveFrames;
        Expect("Ctrl+A selects every party member", RingsShow(PartyMembers.ToArray()));
        Expect("Ctrl+A doesn't pan the camera", Root.GetCanvasTransform() == canvasBefore);
        Expect("no key issued an order",
            PartyMembers.Select((partyMember, i) => partyMember.GlobalPosition.DistanceTo(start[i]))
                .All(distance => distance < StillTolerance));

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
}
