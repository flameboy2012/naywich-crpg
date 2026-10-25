using System.Linq;
using Godot;

namespace Crpg;

/// <summary>
/// Persistent root scene. Holds the current zone, the party, the camera,
/// selection handling and the UI layer.
/// </summary>
public partial class Main : Node2D
{
    /// <summary>M1 has one hard-coded zone; a ZoneManager replaces this in M3.</summary>
    private const string TestZonePath = "res://zones/TestZone.tscn";

    public override void _Ready()
    {
        Node2D zone = LoadZone(TestZonePath);
        var cameraRig = GetNode<CameraRig>("CameraRig");
        cameraRig.SetZone(zone.GetNode<TileMapLayer>("NavigationRegion2D/Ground"));
        cameraRig.CentreOn(PartyCentre(zone));
    }

    /// <summary>The average position of the party members in a loaded zone.</summary>
    private Vector2 PartyCentre(Node2D zone)
    {
        var positions = GetTree().GetNodesInGroup(PlayerControl.PartyMembersGroup)
            .Select(control => control.GetParent<Node2D>())
            .Where(partyMember => partyMember.GetParent() == zone)
            .Select(partyMember => partyMember.GlobalPosition)
            .ToList();
        return positions.Count == 0
            ? zone.GlobalPosition
            : positions.Aggregate(Vector2.Zero, (sum, position) => sum + position) / positions.Count;
    }

    /// <summary>
    /// Loads a zone into ZoneContainer and reparents the party members into it. A Party node holding more than
    /// <see cref="Core.Party.Cap"/> party members is an authoring error: the game stops taking input and quits with
    /// exit code 1, so no move order can ever ask for more spread slots than there are.
    /// </summary>
    private Node2D LoadZone(string path)
    {
        var zone = GD.Load<PackedScene>(path).Instantiate<Node2D>();
        GetNode("ZoneContainer").AddChild(zone);
        Godot.Collections.Array<Node> party = GetNode("Party").GetChildren();
        if (party.Count > Core.Party.Cap)
        {
            GD.PushError($"Main: the Party node holds {party.Count} party members, more than the cap of "
                         + $"{Core.Party.Cap}. Quitting.");
            GetNode("SelectionController").ProcessMode = ProcessModeEnum.Disabled;
            GetTree().Quit(1);
            return zone;
        }

        foreach (Node partyMember in party)
        {
            partyMember.Reparent(zone);
        }

        return zone;
    }
}
