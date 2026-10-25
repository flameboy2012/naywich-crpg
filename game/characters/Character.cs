using System;
using System.Collections.Generic;
using Crpg.Core.Combat;
using Crpg.Core.Movement;
using Godot;

namespace Crpg;

/// <summary>
/// Anyone in a zone that has stats. Party members and monsters share this scene; a child control component
/// (<see cref="PlayerControl"/> for party members, <see cref="AiControl"/> for enemies) decides who gives it orders.
/// Moving, attacking and taking hits live here, so every character behaves the same whoever controls it.
/// </summary>
public partial class Character : CharacterBody2D
{
    /// <summary>
    /// Length of one tile in world pixels, for movement speed: one isometric grid step,
    /// <see cref="Core.TileScale.GridStepLength"/>. Characters move at the same pixel speed in every direction.
    /// </summary>
    public static float TileLength => Core.TileScale.GridStepLength;

    /// <summary>The character scene; the source of the feet radius for tools that bake navigation.</summary>
    public const string ScenePath = "res://characters/Character.tscn";

    /// <summary>Physics layer 3: party members' click areas, which cover the whole capsule sprite.</summary>
    public const uint PartyMemberClickLayer = 1u << 2;

    /// <summary>Physics layer 4: living enemies' click areas. Right-clicks check it ahead of the ground.</summary>
    public const uint EnemyClickLayer = 1u << 3;

    /// <summary>
    /// Avoidance priorities. An agent ignores agents of lower priority, so idle characters (higher) never step aside
    /// and moving ones steer around them, while moving characters share the avoiding between themselves.
    /// </summary>
    private const float IdleAvoidancePriority = 1f;

    private const float MovingAvoidancePriority = 0.5f;

    /// <summary>How far (px) an attack target may move from where the attacker is heading before they re-path.</summary>
    private const float RepathDistance = 8f;

    /// <summary>How long (s) a hit flash takes to fade back.</summary>
    private const float HitFlashSeconds = 0.25f;

    /// <summary>The body colour at the start of a hit flash: an over-bright pulse of the tint.</summary>
    private static readonly Color HitFlashColor = new(2.2f, 2.2f, 2.2f, 1f);

    /// <summary>The body colour of a dead enemy.</summary>
    private static readonly Color DeadColor = new(0.42f, 0.42f, 0.42f, 1f);

    /// <summary>The body colour of a downed party member.</summary>
    private static readonly Color DownColor = new(0.42f, 0.42f, 0.42f, 1f);

    /// <summary>How far (radians) a downed party member's body turns to lie on the ground.</summary>
    private const float LyingRotation = Mathf.Pi / 2;

    private Sprite2D _sprite = null!;
    private Sprite2D _selectionRing = null!;
    private TargetRings _targetRings = null!;
    private Area2D _clickArea = null!;
    private NavigationAgent2D _agent = null!;
    private ArrivalRule _arrival = null!;
    private HpBar _hpBar = null!;
    private Rules? _rules;
    private Tween? _flash;
    private readonly AttackCooldown _cooldown = new();

    /// <summary>
    /// Radius of the feet collision circle, set in Character.tscn. The one source for the navigation agent radius and
    /// the zone navigation mesh's margin. Readable before the character enters the tree.
    /// </summary>
    public float FeetRadius => ((CircleShape2D)GetNode<CollisionShape2D>("CollisionShape2D").Shape).Radius;

    /// <summary>
    /// Movement speed in tiles per second at normal combat speed. 5e 30 ft per 6-second round = 1 tile per second
    /// (ADR 0001). Set from <see cref="CharacterStats"/> on ready; tune the speed there.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,10,0.05,or_greater")]
    public float SpeedTilesPerSecond { get; set; } = 1f;

    /// <summary>Walking speed in world pixels per real second, scaled by the global combat speed (ADR 0003).</summary>
    public float SpeedPixelsPerSecond => CombatRules.MoveSpeedPixelsPerSecond(SpeedTilesPerSecond, CombatSpeed);

    private float CombatSpeed => _rules?.CombatSpeed ?? CombatRules.NormalCombatSpeed;

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
    /// Colour the body sprite is multiplied by, set per instance so characters on the shared scene can be told
    /// apart without new art. Read it once the character is ready.
    /// </summary>
    [Export]
    public Color Tint { get; set; } = Colors.White;

    /// <summary>
    /// The character's stats and equipment, authored as a Resource. Converted once to <see cref="Stats"/> on ready;
    /// its speed replaces <see cref="SpeedTilesPerSecond"/>.
    /// </summary>
    [Export]
    public Data.CharacterStats? CharacterStats { get; set; }

    /// <summary>
    /// The name the combat log uses for this one character, such as "Goblin 1", when several share one stats
    /// Resource. Empty uses the name in the stats.
    /// </summary>
    [Export]
    public string InstanceName { get; set; } = "";

    /// <summary>The Core stats the rules see, converted from <see cref="CharacterStats"/>. Set once ready.</summary>
    public Stats Stats { get; private set; } = null!;

    /// <summary>The name the combat log uses.</summary>
    public string DisplayName => Stats.Name;

    /// <summary>True for an enemy of the party. Set by <see cref="AiControl"/> before the character is ready.</summary>
    public bool IsEnemy { get; set; }

    /// <summary>Current hit points. Characters start at full HP.</summary>
    public int Hp { get; private set; }

    public int MaxHp => Stats.MaxHp;

    /// <summary>
    /// True for an enemy at 0 HP: its grey body stays where it fell, can't be clicked and takes no part.
    /// </summary>
    public bool IsDead { get; private set; }

    /// <summary>
    /// True for a party member at 0 HP: they stay where they fell, greyed and lying down, can't be selected or
    /// ordered, and enemies ignore them. They get up at 1 HP at combat end.
    /// </summary>
    public bool IsDown { get; private set; }

    /// <summary>Raised when this party member goes down or gets up.</summary>
    public event Action<Character>? DownChanged;

    /// <summary>The character this one has an attack order on, or null.</summary>
    public Character? AttackTarget { get; private set; }

    /// <summary>What the HP bar over the character's head shows: current HP over max HP.</summary>
    public float HpBarFraction => _hpBar.Fraction;

    /// <summary>True while the HP bar over the character's head is drawn.</summary>
    public bool HpBarVisible => _hpBar.IsVisibleInTree();

    /// <summary>The colour the body sprite is drawn with now: the tint, a hit flash, or grey when dead.</summary>
    public Color BodyColor => _sprite.Modulate;

    /// <summary>The tints of the target rings under this character, one per party member attacking it.</summary>
    public IReadOnlyList<Color> TargetRingTints => _targetRings.Tints;

    /// <summary>True while the character's click area can be clicked: never for a dead enemy.</summary>
    public bool Clickable => _clickArea.CollisionLayer != 0;

    /// <summary>
    /// The middle of the body's click area, in global coordinates: where a click hits it, standing or lying.
    /// </summary>
    public Vector2 BodyCentre => _clickArea.GetNode<Node2D>("CollisionShape2D").GlobalPosition;

    public override void _Ready()
    {
        if (CharacterStats is null)
        {
            GD.PushError($"Character {Name} has no CharacterStats.");
        }
        else
        {
            Stats = CharacterStats.ToCore();
            if (InstanceName != "")
            {
                Stats = Stats with { Name = InstanceName };
            }

            SpeedTilesPerSecond = Stats.SpeedTilesPerSecond;
            Hp = Stats.MaxHp;
        }

        _rules = GetNodeOrNull<Rules>(Rules.Path);
        _hpBar = GetNode<HpBar>("HpBar");
        ShowHp();
        _sprite = GetNode<Sprite2D>("Sprite");
        _sprite.Modulate = Tint;
        _selectionRing = GetNode<Sprite2D>("SelectionRing");
        _targetRings = GetNode<TargetRings>("TargetRings");
        _clickArea = GetNode<Area2D>("ClickArea");
        _clickArea.CollisionLayer = IsEnemy ? EnemyClickLayer : PartyMemberClickLayer;
        _agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        _agent.Radius = FeetRadius;
        _agent.AvoidanceEnabled = true;
        _agent.AvoidancePriority = IdleAvoidancePriority;
        _agent.VelocityComputed += OnVelocityComputed;
    }

    /// <summary>
    /// True while the navigation agent steers around other characters. Bodies never collide with each other.
    /// </summary>
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
    /// The navigation path points still ahead of the character, next one first, in global coordinates. Empty when the
    /// character is not moving.
    /// </summary>
    public Vector2[] RemainingPath
    {
        get
        {
            if (!IsMoving)
            {
                return [];
            }

            Vector2[] path = _agent.GetCurrentNavigationPath();
            int next = _agent.GetCurrentNavigationPathIndex();
            return next < path.Length ? path[next..] : [];
        }
    }

    /// <summary>
    /// A move order: walks to a global position along the zone's navigation mesh. It replaces any earlier order. An
    /// unreachable point sends the character to the closest reachable point.
    /// </summary>
    public void MoveTo(Vector2 globalPosition)
    {
        if (IsDown)
        {
            return;
        }

        ClearAttackTarget();
        StartMove(globalPosition);
    }

    /// <summary>
    /// An attack order: walks until the target is in range of the equipped attack, then attacks once per attack
    /// cooldown until the target is dead, and then goes idle. It replaces any earlier order but never resets the
    /// cooldown. A dead or downed target is ignored, and a downed party member takes no orders.
    /// </summary>
    public void Attack(Character target)
    {
        if (target == this || target.IsDead || target.IsDown || IsDead || IsDown)
        {
            return;
        }

        if (AttackTarget != target)
        {
            ClearAttackTarget();
            AttackTarget = target;
            if (!IsEnemy)
            {
                target._targetRings.Add(this);
            }
        }

        // Set the path now, not on the next physics frame, so an order given while paused shows its path line.
        if (InRange(target))
        {
            Stop();
        }
        else
        {
            StartMove(target.GlobalPosition);
        }
    }

    // Physics processing obeys ProcessMode, so pausing the tree (PauseManager) stops movement and attack cooldowns.
    public override void _PhysicsProcess(double delta)
    {
        _cooldown.Tick((float)delta, CombatSpeed);
        if (AttackTarget is not null)
        {
            UpdateAttack(AttackTarget);
        }

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
        _agent.MaxSpeed = SpeedPixelsPerSecond;   // combat speed may change mid-move
        _agent.Velocity = GlobalPosition.DirectionTo(next) * SpeedPixelsPerSecond;
    }

    /// <summary>
    /// One frame of an attack order: go idle once the target is dead or down, attack when in range, else close in.
    /// </summary>
    private void UpdateAttack(Character target)
    {
        if (!IsInstanceValid(target) || target.IsDead || target.IsDown)
        {
            ClearAttackTarget();
            Stop();
            return;
        }

        if (InRange(target))
        {
            if (IsMoving)
            {
                Stop();
            }

            if (_cooldown.IsReady)
            {
                Strike(target);
            }

            return;
        }

        if (!IsMoving || _agent.TargetPosition.DistanceTo(target.GlobalPosition) > RepathDistance)
        {
            StartMove(target.GlobalPosition);
        }
    }

    private bool InRange(Character target) =>
        CombatRules.InRange(GlobalPosition.ToWorldPosition(), FeetRadius, target.GlobalPosition.ToWorldPosition(),
            target.FeetRadius, Stats.Attack);

    /// <summary>Makes one attack on the target through the Core rules and writes its combat log line.</summary>
    private void Strike(Character target)
    {
        if (_rules is null)
        {
            GD.PushError($"Character {Name} can't attack: the Rules autoload is missing.");
            return;
        }

        AttackResult result = _rules.Combat.ResolveAttack(Stats, target.Stats);
        _cooldown.Restart();
        target.TakeHit(result);
        _rules.Log.Add(CombatLog.AttackLine(result, target.Hp, target.MaxHp, target.IsEnemy));
        if (target.IsDead)
        {
            ClearAttackTarget();
        }
    }

    /// <summary>
    /// Applies an attack made on this character: on a hit, loses HP and flashes. At 0 HP an enemy dies and a party
    /// member goes down.
    /// </summary>
    private void TakeHit(AttackResult result)
    {
        if (!result.Hit)
        {
            return;
        }

        Hp = Math.Max(0, Hp - result.Damage);
        ShowHp();
        if (Hp == 0 && IsEnemy)
        {
            Die();
        }
        else if (Hp == 0)
        {
            GoDown();
        }

        Flash();
    }

    /// <summary>Shows the current HP on the HP bar over the character's head: empty without stats.</summary>
    private void ShowHp() => _hpBar.Fraction = Stats is null ? 0f : (float)Hp / MaxHp;

    /// <summary>A brief over-bright pulse on the body that fades back to its resting colour.</summary>
    private void Flash()
    {
        _flash?.Kill();
        _sprite.Modulate = HitFlashColor;
        _flash = CreateTween();
        _flash.TweenProperty(_sprite, "modulate", RestingColor, HitFlashSeconds);
    }

    /// <summary>The body colour between hit flashes: grey when dead or down, else the tint.</summary>
    private Color RestingColor => IsDead ? DeadColor : IsDown ? DownColor : Tint;

    /// <summary>
    /// A party member at 0 HP: grey and lying where they fell, no longer steered around, and no longer acting on any
    /// order. Their click area lies down with the body, so a click on it still finds them.
    /// </summary>
    private void GoDown()
    {
        IsDown = true;
        _sprite.Modulate = DownColor;
        _sprite.Rotation = LyingRotation;
        _clickArea.Rotation = LyingRotation;
        _agent.AvoidanceEnabled = false;
        ClearAttackTarget();
        Stop();
        DownChanged?.Invoke(this);
    }

    /// <summary>
    /// At combat end a downed party member gets up at 1 HP (<see cref="CombatRules.HpAtCombatEnd"/>), standing in
    /// their tint again and able to take orders. Does nothing for a party member who is up.
    /// </summary>
    public void GetUp()
    {
        if (!IsDown)
        {
            return;
        }

        IsDown = false;
        Hp = CombatRules.HpAtCombatEnd(Hp);
        ShowHp();
        _flash?.Kill();
        _sprite.Modulate = Tint;
        _sprite.Rotation = 0f;
        _clickArea.Rotation = 0f;
        _agent.AvoidanceEnabled = true;
        DownChanged?.Invoke(this);
    }

    /// <summary>An enemy at 0 HP: grey, unclickable, no longer steered around, and no longer attacking.</summary>
    private void Die()
    {
        IsDead = true;
        _sprite.Modulate = DeadColor;
        _clickArea.CollisionLayer = 0;
        _agent.AvoidanceEnabled = false;
        _targetRings.Clear();
        ClearAttackTarget();
        Stop();
    }

    private void ClearAttackTarget()
    {
        if (AttackTarget is { } target && IsInstanceValid(target))
        {
            target._targetRings.Remove(this);
        }

        AttackTarget = null;
    }

    private void StartMove(Vector2 globalPosition)
    {
        _agent.TargetPosition = globalPosition;
        _agent.MaxSpeed = SpeedPixelsPerSecond;
        _agent.AvoidancePriority = MovingAvoidancePriority;
        _arrival = new ArrivalRule(ArrivalDistancePixels, NoProgressTimeoutSeconds);
        IsMoving = true;
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
