using System.Collections.Generic;
using System.Linq;
using Crpg.Core.Selection;
using Godot;

namespace Crpg;

/// <summary>
/// The portrait column on the left edge of the screen: one <see cref="Portrait"/> per party member, top to bottom in
/// portrait slot order. Clicks send the same slot events the number keys do; a double-click also centres the camera on
/// that party member. Highlights follow every selection change the SelectionController reports.
/// </summary>
public partial class PortraitColumn : VBoxContainer
{
    /// <summary>How far (px) the column sits from the window's top and left edges, so edge pan to the left still works.</summary>
    [Export]
    public float Inset { get; set; } = 16f;

    /// <summary>The size (px) of one placeholder portrait.</summary>
    [Export]
    public Vector2 PortraitSize { get; set; } = new(64, 80);

    [Export]
    public SelectionController SelectionController { get; set; } = null!;

    [Export]
    public CameraRig CameraRig { get; set; } = null!;

    private readonly List<(Portrait Portrait, PlayerControl PartyMember)> _portraits = new();

    public override void _Ready()
    {
        Position = new Vector2(Inset, Inset);
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 8);

        // SelectionController comes before the UI in Main, so its _Ready has already read the portrait slot order.
        IReadOnlyList<PlayerControl> partyMembers = SelectionController.PortraitSlots;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            PlayerControl partyMember = partyMembers[i];
            int slot = i + 1;
            var portrait = new Portrait
            {
                Slot = slot, Tint = partyMember.Tint, CustomMinimumSize = PortraitSize, Name = $"Portrait{slot}",
            };
            portrait.Clicked += SelectionController.SelectSlot;
            portrait.DoubleClicked += OnDoubleClicked;
            AddChild(portrait);
            _portraits.Add((portrait, partyMember));
        }

        SelectionController.SelectionChanged += ShowSelection;
    }

    public override void _ExitTree() => SelectionController.SelectionChanged -= ShowSelection;

    /// <summary>Selects only the party member in the slot, then centres the camera on them, clamped.</summary>
    private void OnDoubleClicked(int slot)
    {
        SelectionController.SelectSlot(new SlotSelectEvent(slot));
        CameraRig.CentreOn(_portraits[slot - 1].PartyMember.GlobalPosition);
    }

    private void ShowSelection(IReadOnlySet<PartyMemberId> selection)
    {
        foreach ((Portrait portrait, PlayerControl partyMember) in _portraits)
        {
            portrait.Highlighted = selection.Contains(partyMember.PartyMemberId);
        }
    }
}
