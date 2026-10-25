using Godot;

namespace Crpg;

/// <summary>
/// A brief ring showing where a party member will actually stop after a move order. It pulses and fades over
/// <see cref="FadeSeconds"/>, then frees itself. Drawn in code, flattened 2:1 to read as a circle on the isometric ground.
/// </summary>
public partial class MoveMarker : Node2D
{
    /// <summary>Group every live move marker is in, so a new right-click can find and replace them.</summary>
    public const string Group = "move_markers";

    /// <summary>How long a marker lives, fading out as it goes.</summary>
    public const float FadeSeconds = 1f;

    /// <summary>Ring radius (px, horizontal) at rest; the vertical radius is half of it.</summary>
    private const float Radius = 14f;

    private const float LineWidth = 2f;

    /// <summary>Pulses per second, and how far (fraction of the radius) each pulse swells the ring.</summary>
    private const float PulseHz = 3f;

    private const float PulseAmount = 0.15f;

    private const int Segments = 32;

    private float _age;

    /// <summary>The party member's tint the ring is drawn in.</summary>
    public Color Tint { get; init; } = Colors.White;

    /// <summary>1 when the marker appears, falling to 0 at the end of the fade.</summary>
    public float Opacity => 1f - Mathf.Clamp(_age / FadeSeconds, 0f, 1f);

    public override void _EnterTree() => AddToGroup(Group);

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= FadeSeconds)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = _age / FadeSeconds;
        // Starts small and swells to full size in the first fifth, pulsing on top of that.
        float grow = Mathf.Min(1f, 0.6f + 2f * t);
        float pulse = 1f + PulseAmount * Mathf.Sin(_age * PulseHz * Mathf.Tau);
        float r = Radius * grow * pulse;
        var points = new Vector2[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float angle = i * Mathf.Tau / Segments;
            points[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r / 2f);
        }

        DrawPolyline(points, Tint with { A = Tint.A * Opacity }, LineWidth, antialiased: true);
    }
}
