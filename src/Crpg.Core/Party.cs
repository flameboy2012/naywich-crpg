namespace Crpg.Core;

/// <summary>Party-wide rules that don't belong to one party member.</summary>
public static class Party
{
    /// <summary>The most party members the player controls at once. The one place the v1 cap of four lives.</summary>
    public const int Cap = 4;
}
