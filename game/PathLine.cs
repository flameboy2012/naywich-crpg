using System.Linq;
using Godot;

namespace Crpg;

/// <summary>
/// The drawn route of one party member: a thin line in their tint from where they stand along the rest of their
/// navigation path. It shows only while the party member is selected and moving, and shrinks as they walk.
/// </summary>
public partial class PathLine : Line2D
{
    /// <summary>Group every path line is in, so checks can find them.</summary>
    public const string Group = "path_lines";

    private const float LineWidth = 2f;

    /// <summary>The party member whose route this line draws.</summary>
    public PlayerControl Source { get; init; } = null!;

    /// <summary>The character the line belongs to.</summary>
    public Character PartyMember => Source.Character;

    public override void _EnterTree() => AddToGroup(Group);

    public override void _Ready()
    {
        Width = LineWidth;
        DefaultColor = Source.Tint;
        Antialiased = true;
        JointMode = LineJointMode.Round;
        Visible = false;
    }

    // Updated every frame after physics, so the line starts where the body stands now.
    public override void _Process(double delta)
    {
        Vector2[] remaining = Source.IsSelected && Source.IsMoving ? Source.RemainingPath : [];
        Visible = remaining.Length > 0;
        Points = Visible ? remaining.Prepend(Source.GlobalPosition).Select(ToLocal).ToArray() : [];
    }
}
