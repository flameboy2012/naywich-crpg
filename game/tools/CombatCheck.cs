using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crpg.Core.Combat;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for melee combat: loads Main with a fixed seed, checks the goblin's SRD stats and HP bar, then has
/// the fighter and cleric attack it. A left-click on the goblin changes nothing; a right-click on its body gives them
/// attack orders and target rings in their tints. They walk into melee reach and attack at most once per attack
/// cooldown: a repeated order mid-cooldown doesn't reset it, and it doesn't advance while paused. Every attack writes
/// one combat log line in the agreed format, a hit flashes the goblin and shrinks its HP bar, and at 0 HP it turns
/// grey, loses its click area and its attackers go idle. Then the rogue and wizard attack Goblin 2 from range: each
/// stops at the range of their attack (shortbow 16 tiles, Fire Bolt 24) and never closes to melee, both follow the
/// goblin when it walks away and attack again once back in range, and their log lines show shortbow damage with the
/// DEX mod and Fire Bolt damage without one. Those cases run with the goblins' AI off, so the goblins stand still.
/// Last, goblins fight back: the party goes back to its start and the AI comes on. The two living goblins stand idle
/// until the fighter walks within their awareness radius, then attack the fighter: the scimitar goblin walks into melee,
/// the shortbow goblin shoots from where it stands, and every attack writes a log line and shrinks the fighter's HP bar.
/// Then down and getting up: the goblins take the idle fighter to 0 HP. The fighter goes down, drops out of the
/// selection, greys out with their portrait, and can't be selected by click, shift-click, box select, portrait, number
/// key or Ctrl+A; the goblins switch to the nearest party member still up. The rest of the party kills both goblins,
/// and at combat end the fighter gets up at 1 HP and can be selected again. Defeat is on the play-test list.
/// Run from game/ after dotnet build, with the seed:
///   godot --headless --path . --fixed-fps 60 --quit-after 6000 --script res://tools/CombatCheck.cs --seed 1
/// Prints "CombatCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class CombatCheck : HeadlessCheck
{
    /// <summary>
    /// The seed the documented command passes. The fights depend on its rolls. Melee: four attacks, three misses then
    /// the fighter's killing hit, long enough for the cooldown cases. Ranged: seven attacks, with misses, Fire Bolt
    /// hits and a shortbow natural 20 crit for the kill, with Goblin 2 alive until after it walks away. Waking: two
    /// goblin hits take the fighter down, and the cleric, rogue and wizard then kill both goblins with nobody else down.
    /// </summary>
    private const int ExpectedSeed = 1;

    /// <summary>Physics frames in one attack cooldown at normal combat speed and --fixed-fps 60.</summary>
    private const int CooldownFrames = (int)(AttackCooldown.Seconds * 60);

    /// <summary>Frames of slack on cooldown timing, for float rounding and the frame an order lands on.</summary>
    private const int FrameSlack = 3;

    /// <summary>How long the check waits for the attackers to reach the goblin, and for the goblin to die.</summary>
    private const int ReachFrames = 1500;

    private const int FightFrames = 2400;

    /// <summary>When the fighter's attack order is repeated, in frames after their first attack: mid-cooldown.</summary>
    private const int ReorderAfterFrames = 120;

    private const int PauseFrames = 120;

    private static readonly Regex AttackLine = new(
        @"^(?<attacker>Fighter|Cleric) → Goblin 1: \d+[+-]\d+=\d+ vs AC 15 (hit|miss|nat 20 crit|nat 1 miss)"
        + @"(, \d+d\d+([+-]\d+)?=\d+ \w+)? \(Goblin 1: (?<hp>\d+)/7(?<dead>, dead)?\)$");

    private static readonly Regex RangedAttackLine = new(
        @"^(?<attacker>Rogue|Wizard) → Goblin 2: \d+\+4=\d+ vs AC 15 (hit|miss|nat 20 crit|nat 1 miss)"
        + @"(, (?<damage>\d+d\d+(\+\d+)?=\d+ \w+))? \(Goblin 2: \d+/7(, dead)?\)$");

    /// <summary>Where Goblin 2 walks to: out of both the rogue's and the wizard's range, past the block at (36, 28).</summary>
    private static readonly Vector2I FleeTile = new(44, 30);

    /// <summary>How long the check waits for Goblin 2 to finish walking away.</summary>
    private const int FleeFrames = 1500;

    /// <summary>A goblin attack on the fighter: d20+4 against chain mail's AC 16, 1d6+2 on a hit.</summary>
    private static readonly Regex GoblinAttackLine = new(
        @"^Goblin [34] → Fighter: \d+\+4=\d+ vs AC 16 (hit|miss|nat 20 crit|nat 1 miss)"
        + @"(, \d+d6\+2=\d+ (slashing|piercing))? \(Fighter: (?<hp>\d+)/12(, down)?\)$");

    /// <summary>How long the goblins are watched standing idle once their AI is on and the party is out of reach.</summary>
    private const int IdleFrames = 60;

    /// <summary>How far short of Goblin 3, in tiles, the fighter's move order ends: inside both goblins' radius.</summary>
    private const float StopShortTiles = 5f;

    /// <summary>
    /// How far (px) inside the awareness radius a waking goblin may first be seen with a target: a frame or two of
    /// the fighter's and the goblin's walking.
    /// </summary>
    private const float WakeSlack = 3f;

    private bool _downPhase;
    private readonly List<(int Frame, string Line)> _downLines = new();

    private List<Vector2> _partyStarts = null!;
    private Character[] _wakeGoblins = [];
    private bool _wakePhase;
    private readonly Dictionary<Character, Wake> _woke = new();
    private readonly HashSet<Character> _goblinMoved = new();
    private readonly List<GoblinAttack> _goblinAttacks = new();
    private readonly List<string> _fighterHpBarMismatches = new();

    private readonly List<(int Frame, string Line)> _attacks = new();
    private readonly List<RangedAttack> _rangedAttacks = new();
    private readonly HashSet<Character> _followers = new();
    private Character _rangedGoblin = null!;
    private bool _rangedPhase;
    private bool _fleeing;
    private float _closestRanged = float.MaxValue;
    private bool _rangedFlashPending;
    private int _rangedHitsSeen;
    private int _rangedHitsFlashed;
    private int _frame;
    private int _pausedFrames;
    private bool _flashPending;
    private int _hitsSeen;
    private int _hitsFlashed;
    private readonly List<string> _hpBarMismatches = new();
    private Character _goblin = null!;

    private Character Fighter => PartyMembers[0];

    private Character Cleric => PartyMembers[1];

    private Character Rogue => PartyMembers[2];

    private Character Wizard => PartyMembers[3];

    private Vector2 GoblinBody => _goblin.GlobalPosition + PartyMemberClickOffset;

    protected override void EachPhysicsFrame()
    {
        _frame++;
        if (Paused)
        {
            _pausedFrames++;
        }

        if (_flashPending)
        {
            _flashPending = false;
            Color body = _goblin.BodyColour;
            if (body.R > _goblin.Tint.R + 0.3f && body.G > _goblin.Tint.G + 0.3f)
            {
                _hitsFlashed++;
            }
        }

        if (_rangedFlashPending)
        {
            _rangedFlashPending = false;
            Color body = _rangedGoblin.BodyColour;
            if (body.R > _rangedGoblin.Tint.R + 0.3f && body.G > _rangedGoblin.Tint.G + 0.3f)
            {
                _rangedHitsFlashed++;
            }
        }

        if (_wakePhase)
        {
            TrackWake();
        }

        if (_rangedPhase)
        {
            _closestRanged = Mathf.Min(_closestRanged, Mathf.Min(RangedDistance(Rogue), RangedDistance(Wizard)));
            foreach (Character attacker in new[] { Rogue, Wizard })
            {
                if (_fleeing && attacker.IsMoving)
                {
                    _followers.Add(attacker);
                }
            }
        }
    }

    protected override IEnumerator<int> Run()
    {
        foreach (int step in WaitForNavigationSync())
        {
            yield return step;
        }

        var rules = Root.GetNode<Rules>(Rules.Path);
        Expect($"--seed {ExpectedSeed} sets the seed (got {rules.Seed})", rules.Seed == ExpectedSeed);
        rules.Log.Changed += OnLogChanged;

        _goblin = Zone.GetNodeOrNull<Character>("Goblin1")!;
        Expect("the test zone has a goblin", _goblin is not null);
        if (_goblin is null)
        {
            yield break;
        }

        _partyStarts = PartyMembers.Select(partyMember => partyMember.GlobalPosition).ToList();
        CheckCluster();

        // The party's attacks are checked on goblins that stand still: their AI stays off until WakeFight.
        SetGoblinAi(false);

        Expect("the goblin's stats come from a MonsterData Resource", _goblin.CharacterStats is Data.MonsterData);
        Expect($"the goblin is an enemy named Goblin 1 (got {_goblin.DisplayName})",
            _goblin.IsEnemy && _goblin.DisplayName == "Goblin 1" && !PartyMembers.Contains(_goblin));
        Expect($"the goblin has SRD AC 15 and 7 HP (got AC {_goblin.Stats.ArmorClass}, {_goblin.Hp}/{_goblin.MaxHp})",
            _goblin.Stats.ArmorClass == 15 && _goblin.MaxHp == 7 && _goblin.Hp == 7);
        Expect("the goblin shows a full HP bar",
            _goblin.HpBarVisible && Mathf.IsEqualApprox(_goblin.HpBarFraction, 1f));
        float startDistance = Fighter.GlobalPosition.DistanceTo(_goblin.GlobalPosition) / Character.TileLength;
        Expect($"the goblin stands well away from the party start ({startDistance:0.0} tiles)", startDistance > 12f);

        // Both the party and the goblin on screen, so the clicks land inside the viewport.
        Root.GetNode<CameraRig>("Main/CameraRig").CentreOn((Fighter.GlobalPosition + _goblin.GlobalPosition) / 2);
        yield return 2;

        ClickPartyMember(MouseButton.Left, Fighter);
        yield return 2;
        ClickPartyMember(MouseButton.Left, Cleric, shift: true);
        yield return 2;
        Expect("the fighter and cleric are selected", Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);

        Click(MouseButton.Left, GoblinBody);
        yield return 2;
        Expect("a left-click on the goblin keeps the selection",
            Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);
        Expect("a left-click on the goblin gives no orders",
            !Fighter.IsMoving && !Cleric.IsMoving && Fighter.AttackTarget is null && Cleric.AttackTarget is null);

        Click(MouseButton.Right, GoblinBody);
        yield return 2;
        Expect("a right-click on the goblin's body gives the selection attack orders on it, not move orders",
            Fighter.AttackTarget == _goblin && Cleric.AttackTarget == _goblin);
        Expect("the attackers walk towards the goblin", Fighter.IsMoving && Cleric.IsMoving);
        Expect($"the goblin shows a target ring in each attacker's tint ({string.Join(", ", _goblin.TargetRingTints)})",
            _goblin.TargetRingTints.Count == 2 && _goblin.TargetRingTints.Contains(Fighter.Tint)
            && _goblin.TargetRingTints.Contains(Cleric.Tint));

        for (int i = 0; i < ReachFrames && AttacksBy("Fighter").Count == 0; i++)
        {
            yield return 1;
        }

        Expect("the fighter reaches the goblin and attacks", AttacksBy("Fighter").Count > 0);
        Expect("the fighter stops to attack", !Fighter.IsMoving);
        float reach = MeleeReach(Fighter, _goblin);
        Expect($"the fighter attacks from within melee reach ({Fighter.GlobalPosition.DistanceTo(_goblin.GlobalPosition):0.0} px)",
            Fighter.GlobalPosition.DistanceTo(_goblin.GlobalPosition) <= reach + 0.5f);

        Expect("the goblin survives the fighter's first attack, so the cooldown cases can run (needs the seed)",
            !_goblin.IsDead);

        // Mid-cooldown: the same order again, then a pause. Neither may reset or advance the cooldown.
        yield return ReorderAfterFrames;
        Click(MouseButton.Right, GoblinBody);
        yield return 2;
        Expect("a repeated right-click on the goblin keeps the attack orders",
            _goblin.IsDead || (Fighter.AttackTarget == _goblin && Cleric.AttackTarget == _goblin));
        foreach (int step in PressSpace())
        {
            yield return step;
        }

        int attacksBeforePause = _attacks.Count;
        yield return PauseFrames;
        Expect("nobody attacks while paused", _attacks.Count == attacksBeforePause);
        foreach (int step in PressSpace())
        {
            yield return step;
        }

        for (int i = 0; i < FightFrames && !_goblin.IsDead; i++)
        {
            yield return 1;
        }

        Expect($"the fighter and cleric kill the goblin ({_attacks.Count} attacks)", _goblin.IsDead && _goblin.Hp == 0);
        CheckAttackLines();
        CheckCooldowns();
        Expect($"every hit flashes the goblin ({_hitsFlashed} of {_hitsSeen})", _hitsSeen > 0 && _hitsFlashed == _hitsSeen);
        Expect($"every attack leaves the HP bar at the logged HP ({string.Join("; ", _hpBarMismatches)})",
            _hpBarMismatches.Count == 0);

        yield return 30;
        Expect($"the dead goblin is grey (got {_goblin.BodyColour})",
            _goblin.BodyColour.R < 0.6f && Mathf.IsEqualApprox(_goblin.BodyColour.R, _goblin.BodyColour.G)
            && Mathf.IsEqualApprox(_goblin.BodyColour.G, _goblin.BodyColour.B));
        Expect("the dead goblin's HP bar is empty", Mathf.IsZeroApprox(_goblin.HpBarFraction));
        Expect("the dead goblin shows no target rings", _goblin.TargetRingTints.Count == 0);
        Expect($"the attackers go idle once the goblin is dead (fighter moving {Fighter.IsMoving}, cleric moving {Cleric.IsMoving})",
            Fighter.AttackTarget is null && Cleric.AttackTarget is null && !Fighter.IsMoving && !Cleric.IsMoving);
        int attacksAtDeath = _attacks.Count;

        Click(MouseButton.Right, GoblinBody);
        yield return 2;
        Expect("the dead goblin can't be clicked: a right-click on its body is a move order",
            !_goblin.Clickable && Fighter.AttackTarget is null && Cleric.AttackTarget is null
            && (Fighter.IsMoving || Cleric.IsMoving));
        yield return 60;
        Expect("nobody attacks the dead goblin", _attacks.Count == attacksAtDeath);

        foreach (int step in RangedFight())
        {
            yield return step;
        }

        foreach (int step in WakeFight())
        {
            yield return step;
        }

        foreach (int step in DownAndGetUp())
        {
            yield return step;
        }

        rules.Log.Changed -= OnLogChanged;
    }

    private IEnumerable<Character> Goblins =>
        Zone.GetChildren().OfType<Character>().Where(character => character.IsEnemy);

    /// <summary>Turns every goblin's AI control component on or off.</summary>
    private void SetGoblinAi(bool on)
    {
        foreach (Character goblin in Goblins)
        {
            goblin.GetNode<AiControl>("AiControl").ProcessMode = on ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }
    }

    /// <summary>Four goblins, two scimitar and two shortbow, in one cluster about 15 tiles from the party start.</summary>
    private void CheckCluster()
    {
        List<Character> goblins = Goblins.ToList();
        Vector2 partyStart = _partyStarts.Aggregate(Vector2.Zero, (sum, at) => sum + at) / _partyStarts.Count;
        Vector2 centre = goblins.Aggregate(Vector2.Zero, (sum, goblin) => sum + goblin.GlobalPosition) / goblins.Count;
        float fromParty = partyStart.DistanceTo(centre) / Character.TileLength;
        float spread = goblins.Max(goblin => goblin.GlobalPosition.DistanceTo(centre)) / Character.TileLength;
        Expect($"four goblins in the zone ({goblins.Count})", goblins.Count == 4);
        Expect("two goblins have scimitars and two have shortbows",
            goblins.Count(goblin => goblin.Stats.Attack.Name == "Scimitar") == 2
            && goblins.Count(goblin => goblin.Stats.Attack.Name == "Shortbow") == 2);
        Expect($"the goblins stand in one cluster ({spread:0.0} tiles from its centre at most)", spread <= 3f);
        Expect($"the cluster is about 15 tiles from the party start ({fromParty:0.0} tiles)",
            fromParty >= 13f && fromParty <= 17f);
    }

    /// <summary>
    /// Goblins fight back. The party goes back to its start and the AI comes on: the living goblins, a scimitar and a
    /// shortbow, stand idle while every party member is outside their awareness radius. The fighter walks towards
    /// them; each goblin wakes the moment the fighter comes within its radius and attacks them. The scimitar goblin
    /// walks into melee reach to attack; the shortbow goblin shoots from where it stands. Every goblin attack writes a
    /// combat log line and leaves the fighter's HP bar at the logged HP.
    /// </summary>
    private IEnumerable<int> WakeFight()
    {
        Character? scimitar = Zone.GetNodeOrNull<Character>("Goblin3");
        Character? shortbow = Zone.GetNodeOrNull<Character>("Goblin4");
        Expect("Goblin 3 has a scimitar and Goblin 4 a shortbow",
            scimitar?.Stats.Attack.Name == "Scimitar" && shortbow?.Stats.Attack.Name == "Shortbow");
        if (scimitar is null || shortbow is null)
        {
            yield break;
        }

        for (int i = 0; i < PartyMembers.Count; i++)
        {
            PartyMembers[i].GlobalPosition = _partyStarts[i];
        }

        _wakeGoblins = new[] { scimitar, shortbow };
        SetGoblinAi(true);
        _wakePhase = true;
        yield return IdleFrames;
        Expect("the goblins stand idle while every party member is outside their awareness radius",
            _wakeGoblins.All(goblin => goblin.AttackTarget is null && !goblin.IsMoving) && _goblinAttacks.Count == 0
            && _woke.Count == 0);

        // The fighter walks to a point inside both goblins' awareness radius, short of melee.
        Vector2 destination = scimitar.GlobalPosition
            + (Fighter.GlobalPosition - scimitar.GlobalPosition).Normalized() * StopShortTiles * Character.TileLength;
        var camera = Root.GetNode<CameraRig>("Main/CameraRig");
        camera.CentreOn((Fighter.GlobalPosition + destination) / 2);
        yield return 2;
        ClickPartyMember(MouseButton.Left, Fighter);
        yield return 2;
        Expect("the fighter alone is selected", Fighter.SelectionRingVisible && !Cleric.SelectionRingVisible
            && !Rogue.SelectionRingVisible && !Wizard.SelectionRingVisible);
        Click(MouseButton.Right, destination);
        yield return 2;
        Expect("the fighter walks towards the goblins", Fighter.IsMoving);

        for (int i = 0; i < FightFrames && !(_woke.Count == 2 && GoblinAttacksBy(scimitar).Count > 0
                                             && GoblinAttacksBy(shortbow).Count > 0 && GoblinHitSeen()); i++)
        {
            yield return 1;
        }

        _wakePhase = false;
        foreach (Character goblin in _wakeGoblins)
        {
            float radius = AwarenessRadiusPixels(goblin);
            Expect($"{goblin.DisplayName} wakes when the fighter comes within its awareness radius "
                   + $"({(_woke.TryGetValue(goblin, out Wake? wake) ? $"{wake.Distance:0.0}" : "never")} px, radius {radius:0.0} px)",
                wake is not null && wake.Distance <= radius && wake.Distance > radius - WakeSlack);
            Expect($"{goblin.DisplayName} attacks the fighter, the nearest party member", wake?.Target == Fighter);
        }

        Expect($"the scimitar goblin walks to the fighter ({scimitar.DisplayName} moved {_goblinMoved.Contains(scimitar)})",
            _goblinMoved.Contains(scimitar));
        float reach = MeleeReach(Fighter, scimitar);
        GoblinAttack? melee = GoblinAttacksBy(scimitar).FirstOrDefault();
        Expect($"the scimitar goblin attacks standing still within melee reach ({melee?.Distance:0.0} px, reach {reach:0.0} px)",
            melee is { Moving: false } && melee.Distance <= reach + 0.5f);
        GoblinAttack? shot = GoblinAttacksBy(shortbow).FirstOrDefault();
        Expect($"the shortbow goblin shoots from where it stands, beyond melee reach ({shot?.Distance:0.0} px)",
            shot is not null && shot.Distance > reach && !_goblinMoved.Contains(shortbow));

        List<string> bad = _goblinAttacks.Select(attack => attack.Line).Where(line => !GoblinAttackLine.IsMatch(line))
            .ToList();
        Expect($"every goblin attack writes one line in the agreed format ({string.Join(" | ", bad)})",
            _goblinAttacks.Count > 0 && bad.Count == 0);
        Expect($"a goblin hits the fighter ({_goblinAttacks.Count} goblin attacks; needs the seed)", GoblinHitSeen());
        Expect($"every goblin attack leaves the fighter's HP bar at the logged HP ({string.Join("; ", _fighterHpBarMismatches)})",
            _fighterHpBarMismatches.Count == 0);
        Expect($"the fighter's HP bar shrinks ({Fighter.HpBarFraction:0.00})", Fighter.HpBarFraction < 1f);
    }

    /// <summary>How long the check waits for the fighter to go down, and for the rest of the party to kill a goblin.</summary>
    private const int DownFrames = 2400;

    /// <summary>
    /// Down and getting up. The fighter stands idle among the two waking goblins until they take them to 0 HP. The
    /// fighter goes down: they drop out of the selection, their body and portrait grey out, the goblins switch to the
    /// nearest party member still up and never attack the fighter again, and a click, shift-click, box select,
    /// portrait click, number key or Ctrl+A can't select them. Then Ctrl+A selects the rest of the party, who kill both
    /// goblins. At combat end the fighter gets up at 1 HP, with their colour and portrait back, and can be selected.
    /// </summary>
    private IEnumerable<int> DownAndGetUp()
    {
        _downPhase = true;
        for (int i = 0; i < DownFrames && Fighter.Hp > 0; i++)
        {
            yield return 1;
        }

        int downFrame = _frame;
        Expect($"the goblins take the fighter to 0 HP ({Fighter.Hp}/{Fighter.MaxHp}; needs the seed)", Fighter.Hp == 0);
        Expect("the fighter goes down", Fighter.IsDown);
        Expect($"the fighter's log line shows them down ({(_downLines.Count > 0 ? _downLines[^1].Line : "")})",
            _downLines.Count > 0 && _downLines[^1].Line.EndsWith($"(Fighter: 0/{Fighter.MaxHp}, down)"));
        yield return 2;
        Expect("the fighter drops out of the selection", !Fighter.SelectionRingVisible);
        Expect("the fighter's portrait greys out", Portraits[0].ShowsDown && !Portraits[1].ShowsDown);

        // Past the hit flash that took the fighter down.
        yield return 30;
        Expect($"the downed fighter is grey (got {Fighter.BodyColour})", IsGrey(Fighter.BodyColour));
        Character[] up = { Cleric, Rogue, Wizard };
        foreach (Character goblin in _wakeGoblins.Where(goblin => !goblin.IsDead))
        {
            Character nearest = up.Where(partyMember => !partyMember.IsDown)
                .MinBy(partyMember => partyMember.GlobalPosition.DistanceTo(goblin.GlobalPosition))!;
            Expect($"{goblin.DisplayName} switches to the nearest party member still up, {nearest.DisplayName} "
                   + $"(got {goblin.AttackTarget?.DisplayName ?? "nobody"})", goblin.AttackTarget == nearest);
        }

        // Every way of selecting, aimed at the fighter. Key 2 first selects the cleric, so a click that changed
        // nothing can be told from a click that cleared the selection.
        Root.GetNode<CameraRig>("Main/CameraRig").CentreOn(Fighter.GlobalPosition);
        yield return 2;
        Expect("the downed fighter's click area lies down with the body",
            Fighter.BodyCentre.DistanceTo(Fighter.GlobalPosition + PartyMemberClickOffset) > 16f);
        PressKey(Key.Key2);
        yield return 2;
        Click(MouseButton.Left, Fighter.BodyCentre);
        yield return 2;
        Expect("a click on the downed fighter changes nothing", !Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);
        Click(MouseButton.Left, Fighter.BodyCentre, shift: true);
        yield return 2;
        Expect("a shift-click on the downed fighter changes nothing",
            !Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);
        Vector2 corner = new(48, 48);
        Drag(Fighter.GlobalPosition - corner, Fighter.GlobalPosition + corner, shift: true);
        yield return 2;
        Expect("a box select around the downed fighter leaves them out",
            !Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);
        ClickPortrait(1, MouseButton.Left);
        yield return 2;
        Expect("a click on the downed fighter's portrait changes nothing",
            !Fighter.SelectionRingVisible && Cleric.SelectionRingVisible && !Portraits[0].Highlighted);
        PressKey(Key.Key1);
        yield return 2;
        Expect("key 1 doesn't select the downed fighter", !Fighter.SelectionRingVisible && Cleric.SelectionRingVisible);
        PressKey(Key.A, ctrl: true);
        yield return 2;
        Expect("Ctrl+A selects the party members still up, not the downed fighter",
            !Fighter.SelectionRingVisible && up.All(partyMember => partyMember.SelectionRingVisible == !partyMember.IsDown));

        // The rest of the party kills both goblins.
        foreach (Character goblin in _wakeGoblins)
        {
            for (int i = 0; i < DownFrames && !goblin.IsDead; i++)
            {
                if (i % 120 == 0)
                {
                    Root.GetNode<CameraRig>("Main/CameraRig").CentreOn(goblin.GlobalPosition);
                    yield return 2;
                    if (!goblin.IsDead && up.All(partyMember => partyMember.AttackTarget != goblin))
                    {
                        Click(MouseButton.Right, goblin.GlobalPosition + PartyMemberClickOffset);
                    }
                }

                yield return 1;
            }

            Expect($"the rest of the party kills {goblin.DisplayName} (needs the seed)", goblin.IsDead);
        }

        Expect("no goblin attacks the downed fighter", _downLines.All(attack =>
            attack.Frame <= downFrame || !attack.Line.Contains(" → Fighter: ")));
        Expect("the party isn't defeated (needs the seed)", up.Any(partyMember => !partyMember.IsDown));

        yield return 10;
        _downPhase = false;
        Expect($"at combat end the fighter gets up at 1 HP (got {Fighter.Hp}, down {Fighter.IsDown})",
            !Fighter.IsDown && Fighter.Hp == 1 && Mathf.IsEqualApprox(Fighter.HpBarFraction, 1f / Fighter.MaxHp));
        Expect($"every downed party member gets up at combat end ({string.Join(", ", PartyMembers.Where(p => p.IsDown).Select(p => p.DisplayName))})",
            PartyMembers.All(partyMember => !partyMember.IsDown && partyMember.Hp >= 1));
        Expect($"the fighter's colour comes back (got {Fighter.BodyColour})", Fighter.BodyColour == Fighter.Tint);
        Expect("the fighter's portrait comes back", !Portraits[0].ShowsDown);
        PressKey(Key.Key1);
        yield return 2;
        Expect("key 1 selects the fighter again", Fighter.SelectionRingVisible && !Cleric.SelectionRingVisible);
    }

    private static bool IsGrey(Color colour) =>
        colour.R < 0.6f && Mathf.IsEqualApprox(colour.R, colour.G) && Mathf.IsEqualApprox(colour.G, colour.B);
    private static float AwarenessRadiusPixels(Character goblin) =>
        ((Data.MonsterData)goblin.CharacterStats!).AwarenessRadiusTiles * Character.TileLength;

    private float NearestPartyMemberDistance(Character goblin) =>
        PartyMembers.Min(partyMember => partyMember.GlobalPosition.DistanceTo(goblin.GlobalPosition));

    private List<GoblinAttack> GoblinAttacksBy(Character goblin) =>
        _goblinAttacks.Where(attack => attack.Line.StartsWith(goblin.DisplayName + " → ")).ToList();

    private bool GoblinHitSeen() => _goblinAttacks.Any(attack => attack.Line.Contains(" hit,") || attack.Line.Contains(" crit,"));

    private void OnGoblinAttack(string line)
    {
        Character? goblin = _wakeGoblins.FirstOrDefault(g => line.StartsWith(g.DisplayName + " → "));
        _goblinAttacks.Add(new GoblinAttack(line, goblin is null ? -1f : goblin.GlobalPosition.DistanceTo(Fighter.GlobalPosition),
            goblin?.IsMoving ?? true));
        Match match = GoblinAttackLine.Match(line);
        if (match.Success && !Mathf.IsEqualApprox(Fighter.HpBarFraction, (float)int.Parse(match.Groups["hp"].Value) / Fighter.MaxHp))
        {
            _fighterHpBarMismatches.Add($"{line} shows {Fighter.HpBarFraction}");
        }
    }

    private void TrackWake()
    {
        foreach (Character goblin in _wakeGoblins)
        {
            if (goblin.IsMoving)
            {
                _goblinMoved.Add(goblin);
            }

            if (!_woke.ContainsKey(goblin) && goblin.AttackTarget is { } target)
            {
                _woke[goblin] = new Wake(NearestPartyMemberDistance(goblin), target);
            }
        }
    }

    private sealed record Wake(float Distance, Character Target);

    private sealed record GoblinAttack(string Line, float Distance, bool Moving);

    /// <summary>
    /// The rogue and wizard attack Goblin 2 from range. The rogue starts out of shortbow range, walks until in range
    /// and stops there; the wizard starts inside Fire Bolt range and shoots without moving. Then the goblin walks away
    /// out of both ranges: both re-path after it, and once it stops they stop at range and attack again, until it dies.
    /// Neither ever comes within melee reach.
    /// </summary>
    private IEnumerable<int> RangedFight()
    {
        _rangedGoblin = Zone.GetNodeOrNull<Character>("Goblin2")!;
        Expect("the test zone has a second goblin for the ranged attackers", _rangedGoblin is not null);
        if (_rangedGoblin is null)
        {
            yield break;
        }

        Expect($"the shortbow reaches 16 tiles and Fire Bolt 24 (got {Rogue.Stats.Attack.RangeTiles}, {Wizard.Stats.Attack.RangeTiles})",
            Rogue.Stats.Attack.RangeTiles == 16 && Wizard.Stats.Attack.RangeTiles == 24);
        Expect($"the rogue starts out of shortbow range ({RangedDistance(Rogue):0} px, range {RangeEdge(Rogue):0} px)",
            RangedDistance(Rogue) > RangeEdge(Rogue));
        Expect($"the wizard starts inside Fire Bolt range ({RangedDistance(Wizard):0} px, range {RangeEdge(Wizard):0} px)",
            RangedDistance(Wizard) <= RangeEdge(Wizard));

        Root.GetNode<CameraRig>("Main/CameraRig").CentreOn((Rogue.GlobalPosition + _rangedGoblin.GlobalPosition) / 2);
        yield return 2;
        ClickPartyMember(MouseButton.Left, Rogue);
        yield return 2;
        ClickPartyMember(MouseButton.Left, Wizard, shift: true);
        yield return 2;
        Expect("the rogue and wizard alone are selected", Rogue.SelectionRingVisible && Wizard.SelectionRingVisible
            && !Fighter.SelectionRingVisible && !Cleric.SelectionRingVisible);

        _rangedPhase = true;
        Click(MouseButton.Right, _rangedGoblin.GlobalPosition + PartyMemberClickOffset);
        yield return 2;
        Expect("a right-click on Goblin 2 gives the rogue and wizard attack orders on it",
            Rogue.AttackTarget == _rangedGoblin && Wizard.AttackTarget == _rangedGoblin);
        Expect("the rogue walks into range; the wizard, already in range, doesn't move", Rogue.IsMoving && !Wizard.IsMoving);

        for (int i = 0; i < ReachFrames && RangedAttacksBy("Rogue").Count == 0; i++)
        {
            yield return 1;
        }

        Expect("the wizard attacks from where they stand", RangedAttacksBy("Wizard").Count > 0);
        ExpectAttackAtRange("Rogue", RangedAttacksBy("Rogue").FirstOrDefault(), "walks into shortbow range");
        Expect("Goblin 2 survives until it walks away, so the re-path case can run (needs the seed)",
            !_rangedGoblin.IsDead);

        // The target walks away, out of both ranges.
        Vector2 away = Ground.ToGlobal(Ground.MapToLocal(FleeTile));
        _rangedGoblin.MoveTo(away);
        _fleeing = true;
        yield return 2;
        for (int i = 0; i < FleeFrames && _rangedGoblin.IsMoving && !_rangedGoblin.IsDead; i++)
        {
            yield return 1;
        }

        _fleeing = false;
        int stoppedFrame = _frame;
        Expect($"Goblin 2 walks away to tile {FleeTile}, out of both starting ranges",
            !_rangedGoblin.IsDead && _rangedGoblin.GlobalPosition.DistanceTo(away) < 2 * ArriveTolerance);
        Expect($"the rogue and wizard re-path after the moving goblin ({string.Join(", ", _followers.Select(f => f.DisplayName))})",
            _followers.Contains(Rogue) && _followers.Contains(Wizard));

        for (int i = 0; i < FightFrames && !_rangedGoblin.IsDead
             && !(AttackAfter("Rogue", stoppedFrame) is not null && AttackAfter("Wizard", stoppedFrame) is not null); i++)
        {
            yield return 1;
        }

        ExpectAttackAtRange("Rogue", AttackAfter("Rogue", stoppedFrame), "follows into shortbow range and attacks again");
        ExpectAttackAtRange("Wizard", AttackAfter("Wizard", stoppedFrame), "follows into Fire Bolt range and attacks again");

        for (int i = 0; i < FightFrames && !_rangedGoblin.IsDead; i++)
        {
            yield return 1;
        }

        _rangedPhase = false;
        Expect($"the rogue and wizard kill Goblin 2 ({_rangedAttacks.Count} attacks)", _rangedGoblin.IsDead);
        float reach = MeleeReach(Rogue, _rangedGoblin);
        Expect($"the ranged attackers never come within melee reach (closest {_closestRanged:0} px, reach {reach:0} px)",
            _closestRanged > reach);
        CheckRangedAttackLines();
        Expect($"every ranged hit flashes Goblin 2 ({_rangedHitsFlashed} of {_rangedHitsSeen})",
            _rangedHitsSeen > 0 && _rangedHitsFlashed == _rangedHitsSeen);
    }

    private float RangedDistance(Character attacker) => attacker.GlobalPosition.DistanceTo(_rangedGoblin.GlobalPosition);

    /// <summary>The farthest the attacker's feet may be from Goblin 2's to attack: range in tiles plus both feet radii.</summary>
    private float RangeEdge(Character attacker) =>
        CombatRules.MaxAttackDistance(attacker.Stats.Attack.RangeTiles, attacker.FeetRadius, _rangedGoblin.FeetRadius);

    /// <summary>The farthest the attacker's feet may be from the target's for a melee attack to reach.</summary>
    private static float MeleeReach(Character attacker, Character target) =>
        CombatRules.MaxAttackDistance(CombatRules.MeleeReachTiles, attacker.FeetRadius, target.FeetRadius);

    private List<RangedAttack> RangedAttacksBy(string attacker) =>
        _rangedAttacks.Where(attack => attack.Line.StartsWith(attacker + " → ")).ToList();

    private RangedAttack? AttackAfter(string attacker, int frame) =>
        RangedAttacksBy(attacker).FirstOrDefault(attack => attack.Frame > frame);

    /// <summary>An attack made standing still, inside range, and less than half a tile short of it: stopped at range.</summary>
    private void ExpectAttackAtRange(string attacker, RangedAttack? attack, string what)
    {
        Character character = attacker == "Rogue" ? Rogue : Wizard;
        float edge = RangeEdge(character);
        Expect($"the {attacker.ToLowerInvariant()} {what} and stops at range ({attack?.Distance:0.0} px, range {edge:0.0} px, moving {attack?.Moving})",
            attack is { Moving: false } && attack.Distance <= edge && attack.Distance > edge - Character.TileLength / 2);
    }

    private void CheckRangedAttackLines()
    {
        List<string> lines = _rangedAttacks.Select(attack => attack.Line).ToList();
        List<string> bad = lines.Where(line => !RangedAttackLine.IsMatch(line)).ToList();
        Expect($"every ranged attack writes one line in the agreed format ({string.Join(" | ", bad)})",
            lines.Count > 0 && bad.Count == 0);
        List<Match> hits = lines.Select(line => RangedAttackLine.Match(line))
            .Where(match => match.Success && match.Groups["damage"].Success).ToList();
        List<Match> bowHits = hits.Where(match => match.Groups["attacker"].Value == "Rogue").ToList();
        List<Match> boltHits = hits.Where(match => match.Groups["attacker"].Value == "Wizard").ToList();
        Expect($"the rogue and wizard each hit at least once ({bowHits.Count}, {boltHits.Count}; needs the seed)",
            bowHits.Count > 0 && boltHits.Count > 0);
        Expect("shortbow damage adds the DEX mod: 1d6+2 piercing", bowHits.All(match =>
            Regex.IsMatch(match.Groups["damage"].Value, @"^\d+d6\+2=\d+ piercing$")));
        Expect("Fire Bolt damage adds no ability mod: 1d10 fire", boltHits.All(match =>
            Regex.IsMatch(match.Groups["damage"].Value, @"^\d+d10=\d+ fire$")));
        Expect("only the killing blow shows Goblin 2 dead",
            lines.Count(line => line.EndsWith(", dead)")) == 1 && lines[^1].EndsWith("(Goblin 2: 0/7, dead)"));
    }

    private void OnLogChanged(IReadOnlyList<string> lines)
    {
        string line = lines[^1];
        GD.Print($"frame {_frame}: {line}");
        if (_downPhase)
        {
            _downLines.Add((_frame, line));
            return;
        }

        if (_wakePhase)
        {
            OnGoblinAttack(line);
            return;
        }

        if (_rangedPhase)
        {
            OnRangedAttack(line);
            return;
        }

        _attacks.Add((_frame, line));
        Match match = AttackLine.Match(line);
        if (!match.Success)
        {
            return;
        }

        int hp = int.Parse(match.Groups["hp"].Value);
        if (!Mathf.IsEqualApprox(_goblin.HpBarFraction, hp / 7f))
        {
            _hpBarMismatches.Add($"{line} shows {_goblin.HpBarFraction}");
        }

        if (line.Contains(" hit,") || line.Contains(" crit,"))
        {
            _hitsSeen++;
            _flashPending = true;
        }
    }

    private void OnRangedAttack(string line)
    {
        Character? attacker = line.StartsWith("Rogue → ") ? Rogue : line.StartsWith("Wizard → ") ? Wizard : null;
        _rangedAttacks.Add(new RangedAttack(_frame, line, attacker is null ? -1f : RangedDistance(attacker),
            attacker?.IsMoving ?? true));
        if (line.Contains(" hit,") || line.Contains(" crit,"))
        {
            _rangedHitsSeen++;
            _rangedFlashPending = true;
        }
    }

    private sealed record RangedAttack(int Frame, string Line, float Distance, bool Moving);

    private List<int> AttacksBy(string attacker) =>
        _attacks.Where(attack => attack.Line.StartsWith(attacker + " → ")).Select(attack => attack.Frame).ToList();

    private void CheckAttackLines()
    {
        List<string> bad = _attacks.Select(attack => attack.Line).Where(line => !AttackLine.IsMatch(line)).ToList();
        Expect($"every attack writes one line in the agreed format ({string.Join(" | ", bad)})",
            _attacks.Count > 0 && bad.Count == 0);
        string last = _attacks.Count > 0 ? _attacks[^1].Line : "";
        Expect($"the killing blow's line shows the goblin dead ({last})", last.EndsWith("(Goblin 1: 0/7, dead)"));
        Expect("only the killing blow shows the goblin dead",
            _attacks.Count(attack => attack.Line.EndsWith(", dead)")) == 1);
    }

    /// <summary>
    /// Each attacker attacks at most once per attack cooldown, counting only unpaused frames. The fighter's second
    /// attack, after the repeated order and the pause, comes exactly one cooldown of unpaused time after the first:
    /// the order neither reset the cooldown to ready (too early) nor restarted it (too late).
    /// </summary>
    private void CheckCooldowns()
    {
        foreach (string attacker in new[] { "Fighter", "Cleric" })
        {
            List<int> frames = AttacksBy(attacker);
            for (int i = 1; i < frames.Count; i++)
            {
                int gap = frames[i] - frames[i - 1];
                bool spansPause = i == 1 && attacker == "Fighter";
                int unpaused = spansPause ? gap - _pausedFrames : gap;
                Expect($"{attacker} waits a full attack cooldown between attacks {i} and {i + 1} ({unpaused} frames)",
                    unpaused >= CooldownFrames - FrameSlack);
            }
        }

        List<int> fighter = AttacksBy("Fighter");
        Expect($"the fighter attacks at least twice ({fighter.Count})", fighter.Count >= 2);
        if (fighter.Count >= 2)
        {
            int unpaused = fighter[1] - fighter[0] - _pausedFrames;
            Expect($"a repeated order and a pause don't reset or restart the fighter's cooldown ({unpaused} unpaused frames, paused {_pausedFrames})",
                _pausedFrames >= PauseFrames && unpaused <= CooldownFrames + FrameSlack);
        }
    }

    /// <summary>Presses and releases Space through the input singleton, waiting for the release to land.</summary>
    private static IEnumerable<int> PressSpace()
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = true });
        yield return 1;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = false });
        yield return 3;
    }
}
