using System.Linq;
using Crpg.Core.Combat;
using Godot;

namespace Crpg;

/// <summary>
/// Autoload that owns the one seeded RNG and exposes the Core combat rules and combat log to the game. A thin wrapper:
/// the rules themselves live in <see cref="CombatRules"/>. The seed is random unless the command line passes
/// <c>--seed N</c>; it is printed to the console and is the combat log's first line.
/// </summary>
public partial class Rules : Node
{
    /// <summary>The autoload's path, for <see cref="Node.GetNode{T}(NodePath)"/>.</summary>
    public const string Path = "/root/Rules";

    /// <summary>The seed every roll this run comes from.</summary>
    public int Seed { get; private set; }

    /// <summary>The combat rules, rolling through the seeded RNG.</summary>
    public CombatRules Combat { get; private set; } = null!;

    /// <summary>
    /// The global combat speed (ADR 0003): attack cooldowns and movement run this many times faster than real time. Provisional
    /// until the M2 play-test.
    /// </summary>
    public float CombatSpeed { get; set; } = CombatRules.NormalCombatSpeed;

    /// <summary>The combat log's lines.</summary>
    public CombatLog Log { get; private set; } = null!;

    public override void _EnterTree()
    {
        Seed = ChooseSeed();
        Combat = new CombatRules(new SeededDiceRoller(Seed));
        Log = new CombatLog(Seed);
        GD.Print($"Rules: {CombatLog.SeedLine(Seed)}");
    }

    /// <summary>The seed from <c>--seed N</c> (engine or user arguments), or a random one.</summary>
    private static int ChooseSeed()
    {
        string[] args = OS.GetCmdlineArgs().Concat(OS.GetCmdlineUserArgs()).ToArray();
        try
        {
            if (SeedArgument.Find(args) is int seed)
            {
                return seed;
            }
        }
        catch (System.FormatException error)
        {
            GD.PushError($"Rules: {error.Message} Using a random seed.");
        }

        return (int)(GD.Randi() & int.MaxValue);
    }
}
