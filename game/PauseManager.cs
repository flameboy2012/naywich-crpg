using System;
using Godot;

namespace Crpg;

/// <summary>
/// Autoload that owns pause state. Space (the toggle_pause action) pauses and resumes, and it sets the scene tree's
/// pause to match. Characters keep the default pausable process mode, so they stop where they are; the nodes the player
/// still uses while paused (camera rig, selection controller, UI, path lines and move markers) process always, set in
/// Main.tscn.
/// </summary>
public partial class PauseManager : Node
{
    /// <summary>True while the game is paused.</summary>
    public bool IsPaused => GetTree().Paused;

    /// <summary>Raised with the new state whenever the game pauses or resumes.</summary>
    public event Action<bool>? PauseChanged;

    public override void _EnterTree() => ProcessMode = ProcessModeEnum.Always;

    /// <summary>Space toggles pause. Read ahead of the UI, so a focused control can't swallow it.</summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Echo: false } key && key.IsActionPressed("toggle_pause"))
        {
            SetPaused(!IsPaused);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Pauses or resumes the game.</summary>
    public void SetPaused(bool paused)
    {
        if (paused == IsPaused)
        {
            return;
        }

        GetTree().Paused = paused;
        PauseChanged?.Invoke(paused);
    }
}
