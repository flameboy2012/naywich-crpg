using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Crpg;

/// <summary>
/// The target rings under an enemy's feet: one ring in each attacking party member's tint, nested outside the
/// selection ring, so the player can see who attacks whom.
/// </summary>
public partial class TargetRings : Node2D
{
    /// <summary>Radius (px) of the innermost ring, just outside the 22 px selection ring.</summary>
    [Export]
    public float InnerRadius { get; set; } = 26f;

    /// <summary>Gap (px) between nested rings.</summary>
    [Export]
    public float Spacing { get; set; } = 5f;

    [Export]
    public float LineWidth { get; set; } = 2.5f;

    private readonly List<Character> _attackers = new();

    /// <summary>The tints of the rings shown, innermost first.</summary>
    public IReadOnlyList<Color> Tints => _attackers.Select(attacker => attacker.Tint).ToArray();

    public void Add(Character attacker)
    {
        if (!_attackers.Contains(attacker))
        {
            _attackers.Add(attacker);
            QueueRedraw();
        }
    }

    public void Remove(Character attacker)
    {
        if (_attackers.Remove(attacker))
        {
            QueueRedraw();
        }
    }

    public void Clear()
    {
        _attackers.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < _attackers.Count; i++)
        {
            DrawPolyline(Ellipse(InnerRadius + i * Spacing), _attackers[i].Tint, LineWidth, antialiased: true);
        }
    }

    /// <summary>A closed ellipse flattened to the isometric ground, 2:1 like the tiles.</summary>
    private static Vector2[] Ellipse(float radius)
    {
        const int Segments = 48;
        var points = new Vector2[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float angle = Mathf.Tau * i / Segments;
            points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius / 2f);
        }

        return points;
    }
}
