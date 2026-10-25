using Crpg.Core.Combat;
using Godot;

namespace Crpg.Data;

/// <summary>A suit of armour (with shield, if any). Converts to a Core <see cref="Armor"/> on load (ADR 0004).</summary>
[GlobalClass]
public partial class ArmorData : Resource
{
    [Export]
    public string Name { get; set; } = "";

    [Export(PropertyHint.Range, "10,30,1")]
    public int BaseArmorClass { get; set; } = 11;

    /// <summary>Whether the armour limits the DEX bonus to <see cref="MaxDexterityBonus"/>. Off means no limit.</summary>
    [Export]
    public bool LimitsDexterityBonus { get; set; }

    /// <summary>The most DEX modifier the armour allows, when it limits it. 0 for heavy armour, 2 for medium.</summary>
    [Export(PropertyHint.Range, "0,10,1")]
    public int MaxDexterityBonus { get; set; }

    public Armor ToCore() => new(Name, BaseArmorClass, LimitsDexterityBonus ? MaxDexterityBonus : null);
}
