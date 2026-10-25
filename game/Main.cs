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
        CentreCameraOn(zone.GetNode<TileMapLayer>("NavigationRegion2D/Ground"));
    }

    /// <summary>
    /// Loads a zone into ZoneContainer and reparents the party members into it. A Party node holding more than
    /// <see cref="Core.Party.Cap"/> party members is an authoring error.
    /// </summary>
    private Node2D LoadZone(string path)
    {
        var zone = GD.Load<PackedScene>(path).Instantiate<Node2D>();
        GetNode("ZoneContainer").AddChild(zone);
        Godot.Collections.Array<Node> party = GetNode("Party").GetChildren();
        if (party.Count > Core.Party.Cap)
        {
            GD.PushError($"Main: the Party node holds {party.Count} party members, more than the cap of {Core.Party.Cap}");
        }

        foreach (Node partyMember in party)
        {
            partyMember.Reparent(zone);
        }

        return zone;
    }

    private void CentreCameraOn(TileMapLayer ground)
    {
        Rect2I used = ground.GetUsedRect();
        Vector2 centre = ground.ToGlobal(ground.MapToLocal(used.Position + used.Size / 2));
        GetNode<Camera2D>("CameraRig/Camera2D").GlobalPosition = centre;
    }
}
