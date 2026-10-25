namespace Crpg.Core.Combat;

/// <summary>
/// The one source of randomness for the rules: rolls a single die. The game uses a <see cref="SeededDiceRoller"/>;
/// tests inject fixed rolls.
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Rolls one die with the given number of sides: a whole number from 1 to <paramref name="sides"/>.
    /// </summary>
    int Roll(int sides);
}
