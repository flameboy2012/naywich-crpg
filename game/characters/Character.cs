using Godot;

namespace Crpg;

/// <summary>
/// Any actor in a zone that has stats. Party members and (later) monsters share this scene; a child
/// control component (PlayerControl in M1) decides who gives it orders.
/// </summary>
public partial class Character : CharacterBody2D
{
    private Sprite2D _selectionRing = null!;

    public override void _Ready()
    {
        _selectionRing = GetNode<Sprite2D>("SelectionRing");
    }

    /// <summary>Shows or hides the ring under the character's feet.</summary>
    public bool SelectionRingVisible
    {
        get => _selectionRing.Visible;
        set => _selectionRing.Visible = value;
    }
}
