using Godot;

namespace Crpg;

/// <summary>A small bar over a character's head showing current HP as a share of max HP.</summary>
public partial class HpBar : Node2D
{
    [Export]
    public Vector2 Size { get; set; } = new(32, 4);

    [Export]
    public Color FillColor { get; set; } = new(0.3f, 0.85f, 0.3f);

    [Export]
    public Color EmptyColor { get; set; } = new(0.35f, 0.05f, 0.05f, 0.9f);

    private float _fraction = 1f;

    /// <summary>Current HP over max HP, 0 to 1.</summary>
    public float Fraction
    {
        get => _fraction;
        set
        {
            _fraction = Mathf.Clamp(value, 0f, 1f);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var back = new Rect2(-Size / 2, Size);
        DrawRect(back.Grow(1), Colors.Black);
        DrawRect(back, EmptyColor);
        DrawRect(new Rect2(back.Position, new Vector2(Size.X * _fraction, Size.Y)), FillColor);
    }
}
