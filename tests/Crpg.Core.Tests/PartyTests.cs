using Crpg.Core.Selection;

namespace Crpg.Core.Tests;

public class PartyTests
{
    [Fact]
    public void The_spread_slot_pattern_has_exactly_one_slot_per_party_member_the_party_can_hold()
    {
        Assert.Equal(4, Party.Cap);
        Assert.Equal(Party.Cap, SpreadSlots.Count);
    }
}
