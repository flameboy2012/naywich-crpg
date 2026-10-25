using System;
using Crpg.Core.Selection;
using Godot;
using GodotMouseButton = Godot.MouseButton;

namespace Crpg;

/// <summary>
/// A placeholder portrait for one party member: a panel in their tint showing their portrait slot number, with a
/// bright border while they are in the selection. Consumes every mouse button on it, so none reaches the world.
/// </summary>
public partial class Portrait : Panel
{
    private static readonly Color BorderColor = new(0.1f, 0.1f, 0.1f);
    private static readonly Color HighlightColor = new(1f, 0.95f, 0.55f);

    private readonly StyleBoxFlat _style = new();
    private bool _highlighted;

    /// <summary>The portrait slot this portrait shows: 1 is the top.</summary>
    public int Slot { get; init; }

    /// <summary>The party member's tint, which fills the panel.</summary>
    public Color Tint { get; init; } = Colors.White;

    /// <summary>A left click: this portrait's slot, with whether shift was held.</summary>
    public event Action<SlotSelectEvent>? Clicked;

    /// <summary>A left double-click.</summary>
    public event Action<int>? DoubleClicked;

    /// <summary>True while the party member is in the selection; shown as a bright border.</summary>
    public bool Highlighted
    {
        get => _highlighted;
        set
        {
            _highlighted = value;
            _style.BorderColor = value ? HighlightColor : BorderColor;
        }
    }

    public override void _Ready()
    {
        _style.BgColor = Tint;
        _style.BorderColor = BorderColor;
        _style.SetBorderWidthAll(4);
        AddThemeStyleboxOverride("panel", _style);

        var label = new Label
        {
            Text = Slot.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(label);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton button)
        {
            return;
        }

        if (button is { ButtonIndex: GodotMouseButton.Left, Pressed: true })
        {
            if (button.DoubleClick)
            {
                DoubleClicked?.Invoke(Slot);
            }
            else
            {
                Clicked?.Invoke(new SlotSelectEvent(Slot, button.ShiftPressed));
            }
        }

        // Right-click and every release do nothing, but are still kept from the world.
        AcceptEvent();
    }
}
