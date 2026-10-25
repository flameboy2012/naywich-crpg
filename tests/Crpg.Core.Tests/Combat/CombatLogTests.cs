using Crpg.Core.Combat;

namespace Crpg.Core.Tests.Combat;

public class CombatLogTests
{
    [Fact]
    public void The_log_starts_with_the_seed_line()
    {
        var log = new CombatLog(seed: 42);

        Assert.Equal(["Seed: 42"], log.Lines);
    }

    [Fact]
    public void New_lines_go_at_the_bottom()
    {
        var log = new CombatLog(seed: 42);

        log.Add("first");
        log.Add("second");

        Assert.Equal(["Seed: 42", "first", "second"], log.Lines);
    }

    [Fact]
    public void The_log_keeps_only_the_last_8_lines()
    {
        var log = new CombatLog(seed: 42);

        for (int i = 1; i <= 10; i++)
        {
            log.Add($"line {i}");
        }

        Assert.Equal(8, CombatLog.ShownLines);
        Assert.Equal(Enumerable.Range(3, 8).Select(i => $"line {i}"), log.Lines);
    }

    [Fact]
    public void Adding_a_line_raises_the_change_with_the_new_lines()
    {
        var log = new CombatLog(seed: 1);
        IReadOnlyList<string>? seen = null;
        log.Changed += lines => seen = lines;

        log.Add("hit");

        Assert.Equal(["Seed: 1", "hit"], seen);
    }
}
