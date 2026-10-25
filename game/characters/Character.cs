using Godot;

namespace Crpg;

/// <summary>
/// Any actor in a zone that has stats. Party members and (later) monsters share this scene; a child
/// control component (PlayerControl in M1) decides who gives it orders.
/// </summary>
public partial class Character : CharacterBody2D
{
    /// <summary>
    /// Length of one tile in world pixels, for movement speed. A "tile" here is one isometric grid step
    /// (cell (x, y) to (x + 1, y)): the vector (32, 16) on a 64x32 tile, about 35.78 px. Characters move at
    /// the same pixel speed in every direction.
    /// </summary>
    public static readonly float TileLength = new Vector2(32, 16).Length();

    private Sprite2D _selectionRing = null!;
    private NavigationAgent2D _agent = null!;

    /// <summary>
    /// Movement speed in tiles per second. 5e 30 ft per 6-second round = 1 tile per second (ADR 0001).
    /// Tunable in the inspector, no code change needed.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,10,0.05,or_greater")]
    public float SpeedTilesPerSecond { get; set; } = 1f;

    public float SpeedPixelsPerSecond => SpeedTilesPerSecond * TileLength;

    /// <summary>True while the character has a destination it has not reached yet.</summary>
    public bool IsMoving { get; private set; }

    public override void _Ready()
    {
        _selectionRing = GetNode<Sprite2D>("SelectionRing");
        _agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
    }

    /// <summary>Shows or hides the ring under the character's feet.</summary>
    public bool SelectionRingVisible
    {
        get => _selectionRing.Visible;
        set => _selectionRing.Visible = value;
    }

    /// <summary>
    /// Walks to a global position along the zone's navigation mesh. Calling it again replaces the current
    /// destination. An unreachable point sends the character to the closest reachable point.
    /// </summary>
    public void MoveTo(Vector2 globalPosition)
    {
        _agent.TargetPosition = globalPosition;
        IsMoving = true;
    }

    // Physics processing obeys ProcessMode, so pausing the tree (M2) stops movement.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsMoving)
        {
            return;
        }

        if (_agent.IsNavigationFinished())
        {
            IsMoving = false;
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 next = _agent.GetNextPathPosition();
        Velocity = GlobalPosition.DirectionTo(next) * SpeedPixelsPerSecond;
        MoveAndSlide();
    }
}
