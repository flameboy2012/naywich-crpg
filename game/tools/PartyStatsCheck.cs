using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg.Tools;

/// <summary>
/// Headless check for party stats and the combat log: loads Main with a fixed seed and checks that the party members,
/// in portrait slot order, are the spec's fighter, cleric, rogue and wizard with their derived AC, HP and attacks, that
/// each shows a full HP bar, and that the combat log panel sits in the bottom-left corner with the seed as its first
/// line. Run from game/ after dotnet build, with the seed:
///   godot --headless --path . --fixed-fps 60 --quit-after 120 --script res://tools/PartyStatsCheck.cs --seed 4242
/// Prints "PartyStatsCheck: PASSED" and exits 0, or prints ERROR lines and exits 1.
/// </summary>
public partial class PartyStatsCheck : HeadlessCheck
{
    /// <summary>The seed the documented command passes.</summary>
    private const int ExpectedSeed = 4242;

    /// <summary>The spec's party (#24), in portrait slot order: name, AC, max HP, attack.</summary>
    private static readonly (string Name, int ArmorClass, int MaxHp, string Attack, int AttackBonus)[] Expected =
    [
        ("Fighter", 16, 12, "Longsword", 4),
        ("Cleric", 16, 9, "Mace", 4),
        ("Rogue", 13, 10, "Shortbow", 4),
        ("Wizard", 12, 7, "Fire Bolt", 4),
    ];

    protected override IEnumerator<int> Run()
    {
        var controller = Root.GetNode<SelectionController>("Main/SelectionController");
        string[] slotIds = controller.PortraitSlots.Select(slot => slot.PartyMemberId.Value).ToArray();
        Expect("portrait slots follow the party members' spawn order",
            slotIds.SequenceEqual(PartyMembers.Select(partyMember => partyMember.Name.ToString())));

        Expect($"the party has {Expected.Length} party members", PartyMembers.Count == Expected.Length);
        for (int i = 0; i < Expected.Length && i < PartyMembers.Count; i++)
        {
            Character partyMember = PartyMembers[i];
            var expected = Expected[i];
            string slot = $"slot {i + 1}";
            Expect($"{slot} is the {expected.Name} (got {partyMember.DisplayName})",
                partyMember.DisplayName == expected.Name);
            Expect($"{slot} has AC {expected.ArmorClass} (got {partyMember.Stats.ArmorClass})",
                partyMember.Stats.ArmorClass == expected.ArmorClass);
            Expect($"{slot} has max HP {expected.MaxHp} (got {partyMember.MaxHp})", partyMember.MaxHp == expected.MaxHp);
            Expect($"{slot} attacks with a {expected.Attack} (got {partyMember.Stats.Attack.Name})",
                partyMember.Stats.Attack.Name == expected.Attack);
            Expect($"{slot} has attack bonus +{expected.AttackBonus} (got {partyMember.Stats.AttackBonus})",
                partyMember.Stats.AttackBonus == expected.AttackBonus);
            Expect($"{slot} starts at full HP", partyMember.Hp == partyMember.MaxHp);
            Expect($"{slot} shows an HP bar", partyMember.HpBarVisible);
            Expect($"{slot}'s HP bar is full (got {partyMember.HpBarFraction})",
                Mathf.IsEqualApprox(partyMember.HpBarFraction, 1f));
        }

        var rules = Root.GetNode<Rules>(Rules.Path);
        Expect($"--seed {ExpectedSeed} sets the seed (got {rules.Seed})", rules.Seed == ExpectedSeed);

        var panel = Root.GetNode<CombatLogPanel>("Main/UI/CombatLogPanel");
        yield return 2;
        Expect($"the combat log's first line is the seed (got '{panel.ShownLines.FirstOrDefault()}')",
            panel.ShownLines.FirstOrDefault() == $"Seed: {ExpectedSeed}");
        Rect2 rect = panel.GetGlobalRect();
        Vector2 viewport = Root.GetVisibleRect().Size;
        Expect($"the combat log panel sits in the bottom-left corner (rect {rect}, viewport {viewport})",
            rect.Position.X < viewport.X / 4 && rect.End.Y > viewport.Y * 3 / 4 && rect.End.Y <= viewport.Y
            && rect.Size.Y > 0);
    }
}
