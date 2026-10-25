namespace Crpg.Core.Combat;

/// <summary>A character's six ability scores.</summary>
public readonly record struct AbilityScores(
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma)
{
    /// <summary>The 5e ability modifier for a score: (score - 10) / 2, rounded down.</summary>
    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public int StrengthModifier => Modifier(Strength);

    public int DexterityModifier => Modifier(Dexterity);

    public int IntelligenceModifier => Modifier(Intelligence);
}
