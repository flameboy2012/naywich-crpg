using Crpg.Core.Combat;
using Godot;

namespace Crpg.Data;

/// <summary>
/// A character's stats and equipment, authored in the inspector. Converts to a Core <see cref="Stats"/> on load
/// (ADR 0004); attack bonus and AC are derived there, never stored here.
/// </summary>
[GlobalClass]
public partial class CharacterStats : Resource
{
    [Export]
    public string Name { get; set; } = "";

    [ExportGroup("Ability scores")]
    [Export(PropertyHint.Range, "1,30,1")]
    public int Strength { get; set; } = 10;

    [Export(PropertyHint.Range, "1,30,1")]
    public int Dexterity { get; set; } = 10;

    [Export(PropertyHint.Range, "1,30,1")]
    public int Constitution { get; set; } = 10;

    [Export(PropertyHint.Range, "1,30,1")]
    public int Intelligence { get; set; } = 10;

    [Export(PropertyHint.Range, "1,30,1")]
    public int Wisdom { get; set; } = 10;

    [Export(PropertyHint.Range, "1,30,1")]
    public int Charisma { get; set; } = 10;

    [ExportGroup("")]
    [Export(PropertyHint.Range, "1,500,1,or_greater")]
    public int MaxHp { get; set; } = 1;

    /// <summary>Walking speed in feet per round, as in 5e. 30 ft moves one tile per second (ADR 0001).</summary>
    [Export(PropertyHint.Range, "5,120,5,or_greater")]
    public int SpeedFeet { get; set; } = 30;

    [Export(PropertyHint.Range, "2,6,1")]
    public int ProficiencyBonus { get; set; } = 2;

    /// <summary>The equipped attack.</summary>
    [Export]
    public WeaponData? Attack { get; set; }

    /// <summary>The equipped armour; empty for none.</summary>
    [Export]
    public ArmorData? Armor { get; set; }

    public Stats ToCore()
    {
        if (Attack is null)
        {
            throw new System.InvalidOperationException($"CharacterStats '{Name}' ({ResourcePath}) has no attack.");
        }

        return new Stats(Name, new AbilityScores(Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma),
            MaxHp, SpeedFeet, ProficiencyBonus, Attack.ToCore(), Armor?.ToCore());
    }
}
