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
/// grey, loses its click area and its attackers go idle.
/// Run from game/ after dotnet build, with the seed:
///   godot --headless --path . --fixed-fps 60 --quit-after 4000 --script res://tools/CombatCheck.cs --seed 11
/// Prints "CombatCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class CombatCheck : HeadlessCheck
{
    /// <summary>
    /// The seed the documented command passes. The fight depends on its rolls: six attacks, with misses, a natural 1
    /// and hits, long enough for the cooldown cases.
    /// </summary>
    private const int ExpectedSeed = 11;

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

    private readonly List<(int Frame, string Line)> _attacks = new();
    private int _frame;
    private int _pausedFrames;
    private bool _flashPending;
    private int _hitsSeen;
    private int _hitsFlashed;
    private readonly List<string> _hpBarMismatches = new();
    private Character _goblin = null!;

    private Character Fighter => PartyMembers[0];

    private Character Cleric => PartyMembers[1];

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
        float reach = Character.TileLength + Fighter.FeetRadius + _goblin.FeetRadius;
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
        rules.Log.Changed -= OnLogChanged;
    }

    private void OnLogChanged(IReadOnlyList<string> lines)
    {
        string line = lines[^1];
        _attacks.Add((_frame, line));
        GD.Print($"frame {_frame}: {line}");
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
