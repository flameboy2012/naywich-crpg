using System.Collections.Generic;
using Godot;

namespace Crpg;

/// <summary>
/// The combat log in the bottom-right corner: the last lines of the <see cref="Rules"/> combat log, newest at the
/// bottom. Clicks pass through it to the world.
/// </summary>
public partial class CombatLogPanel : PanelContainer
{
    /// <summary>How far (px) the panel sits from the window's bottom and right edges.</summary>
    [Export]
    public float Inset { get; set; } = 16f;

    /// <summary>The panel's width (px).</summary>
    [Export]
    public float Width { get; set; } = 640f;

    private Label _text = null!;
    private Core.Combat.CombatLog _log = null!;

    /// <summary>The lines on screen, oldest first.</summary>
    public IReadOnlyList<string> ShownLines { get; private set; } = [];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        var style = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.55f) };
        style.SetContentMarginAll(8);
        AddThemeStyleboxOverride("panel", style);

        _text = new Label { MouseFilter = MouseFilterEnum.Ignore, Name = "Text" };
        _text.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_text);

        // Anchored to the bottom-right corner, growing upwards as lines are added.
        SetAnchorsPreset(LayoutPreset.BottomRight);
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.Begin;
        OffsetLeft = -Inset - Width;
        OffsetRight = -Inset;
        OffsetBottom = -Inset;
        OffsetTop = -Inset;

        _log = GetNode<Rules>(Rules.Path).Log;
        _log.Changed += Show;
        Show(_log.Lines);
    }

    public override void _ExitTree() => _log.Changed -= Show;

    private void Show(IReadOnlyList<string> lines)
    {
        ShownLines = lines;
        _text.Text = string.Join("\n", lines);
    }
}
