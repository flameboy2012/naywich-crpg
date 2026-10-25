using Godot;

namespace Crpg;

/// <summary>
/// Anyone in a zone that has stats. Party members and (later) monsters share this scene; a child
/// control component (PlayerControl in M1) decides who gives it orders.
/// </summary>
public partial class Character : CharacterBody2D
{
    /// <summary>
    /// Length of one tile in world pixels, for movement speed. A "tile" here is one isometric grid step
    /// (tile (x, y) to (x + 1, y)): half the tile size, (32, 16) on a 64x32 tile, about 35.78 px. Characters move
    /// at the same pixel speed in every direction.
    /// </summary>
    public static readonly float TileLength = ((Vector2)Tile.Size / 2).Length();

    /// <summary>The character scene; the source of the feet radius for tools that bake navigation.</summary>
    public const string ScenePath = "res://characters/Character.tscn";

    private Sprite2D _selectionRing = null!;
    private NavigationAgent2D _agent = null!;

    /// <summary>
    /// Radius of the feet collision circle, set in Character.tscn. The one source for the navigation agent radius and
    /// the zone navigation mesh's margin. Readable before the character enters the tree.
    /// </summary>
    public float FeetRadius => ((CircleShape2D)GetNode<CollisionShape2D>("CollisionShape2D").Shape).Radius;

    /// <summary>
    /// Movement speed in tiles per second. 5e 30 ft per 6-second round = 1 tile per second (ADR 0001).
    /// Tunable in the inspector, no code change needed.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,10,0.05,or_greater")]
    public float SpeedTilesPerSecond { get; set; } = 1f;

    public float SpeedPixelsPerSecond => SpeedTilesPerSecond * TileLength;

    /// <summary>True while the character has a destination it has not reached yet.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Colour the body sprite is multiplied by, set per instance so party members on the shared scene can be told
    /// apart without new art. Read it once the character is ready.
    /// </summary>
    [Export]
    public Color Tint { get; set; } = Colors.White;

    public override void _Ready()
    {
        GetNode<Sprite2D>("Sprite").Modulate = Tint;
        _selectionRing = GetNode<Sprite2D>("SelectionRing");
        _agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        _agent.Radius = FeetRadius;
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
