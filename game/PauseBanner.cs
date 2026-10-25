using Godot;

namespace Crpg;

/// <summary>A "PAUSED" banner at the top centre of the screen, shown only while the game is paused.</summary>
public partial class PauseBanner : Label
{
    private PauseManager _pauseManager = null!;

    public override void _Ready()
    {
        Text = "PAUSED";
        _pauseManager = GetNode<PauseManager>("/root/PauseManager");
        _pauseManager.PauseChanged += OnPauseChanged;
        OnPauseChanged(_pauseManager.IsPaused);
    }

    public override void _ExitTree() => _pauseManager.PauseChanged -= OnPauseChanged;

    private void OnPauseChanged(bool paused) => Visible = paused;
}
