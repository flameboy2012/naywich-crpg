using System.Reflection;

namespace Crpg.Core.Tests;

public class CoreIsolationTests
{
    [Fact]
    public void Core_does_not_reference_Godot()
    {
        Assembly core = Assembly.Load("Crpg.Core");

        var godotRefs = core.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Godot", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(godotRefs);
    }
}
