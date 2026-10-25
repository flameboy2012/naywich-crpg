using Crpg.Core.Movement;
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

    /// <summary>
    /// Avoidance priorities. An agent ignores agents of lower priority, so idle characters (higher) never step aside
    /// and moving ones steer around them, while moving characters share the avoiding between themselves.
    /// </summary>
    private const float IdleAvoidancePriority = 1f;

    private const float MovingAvoidancePriority = 0.5f;

    private Sprite2D _selectionRing = null!;
    private NavigationAgent2D _agent = null!;
    private ArrivalRule _arrival = null!;

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

    /// <summary>
    /// How close to the destination, in tiles, a party member that has stopped making progress must be for the move to
    /// finish anyway (an occupied spread slot, or slots snapped together near a wall). Tunable in the inspector.
    /// </summary>
    [Export(PropertyHint.Range, "0,2,0.05,or_greater")]
    public float ArrivalDistanceTiles { get; set; } = 0.5f;

    /// <summary>
    /// Seconds without meaningful progress, within <see cref="ArrivalDistanceTiles"/> of the destination, after which a
    /// move finishes. Tunable in the inspector.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,0.05,or_greater")]
    public float NoProgressTimeoutSeconds { get; set; } = 0.5f;

    public float ArrivalDistancePixels => ArrivalDistanceTiles * TileLength;

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
        _agent.AvoidanceEnabled = true;
        _agent.AvoidancePriority = IdleAvoidancePriority;
        _agent.VelocityComputed += OnVelocityComputed;
    }

    /// <summary>True while the navigation agent steers around other characters. Bodies never collide with each other.</summary>
    public bool AvoidanceEnabled => _agent.AvoidanceEnabled;

    /// <summary>Shows or hides the ring under the character's feet.</summary>
    public bool SelectionRingVisible
    {
        get => _selectionRing.Visible;
        set => _selectionRing.Visible = value;
    }

    /// <summary>
    /// Where the current navigation path ends: where the character will really stop, which differs from the requested
    /// destination when that is unreachable. Read-only; the zero vector before any move.
    /// </summary>
    public Vector2 PathEnd => _agent.GetFinalPosition();

    /// <summary>
    /// Walks to a global position along the zone's navigation mesh. Calling it again replaces the current
    /// destination. An unreachable point sends the character to the closest reachable point.
    /// </summary>
    public void MoveTo(Vector2 globalPosition)
    {
        _agent.TargetPosition = globalPosition;
        _agent.MaxSpeed = SpeedPixelsPerSecond;
        _agent.AvoidancePriority = MovingAvoidancePriority;
        _arrival = new ArrivalRule(ArrivalDistancePixels, NoProgressTimeoutSeconds);
        IsMoving = true;
    }

    // Physics processing obeys ProcessMode, so pausing the tree (M2) stops movement.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsMoving)
        {
            return;
        }

        // The final path point, not the target: an unreachable target is replaced by the closest reachable point.
        if (_agent.IsNavigationFinished()
            || _arrival.Update(GlobalPosition.DistanceTo(_agent.GetFinalPosition()), (float)delta))
        {
            Stop();
            return;
        }

        // Avoidance turns this into a safe velocity and hands it back through VelocityComputed, which moves the body.
        Vector2 next = _agent.GetNextPathPosition();
        _agent.Velocity = GlobalPosition.DirectionTo(next) * SpeedPixelsPerSecond;
    }

    private void OnVelocityComputed(Vector2 safeVelocity)
    {
        if (!IsMoving)
        {
            return;
        }

        Velocity = safeVelocity;
        MoveAndSlide();
    }

    private void Stop()
    {
        IsMoving = false;
        Velocity = Vector2.Zero;
        _agent.Velocity = Vector2.Zero;
        _agent.AvoidancePriority = IdleAvoidancePriority;
    }
}