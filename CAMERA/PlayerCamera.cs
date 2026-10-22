// Reusable camera-only mouse-wheel zoom for the fully 2D production world.
// The authored/default zoom is the closest view; scrolling out reveals at most 25%
// more world width/height. No per-frame process or player movement dependencies.
using Godot;

public partial class PlayerCamera : Camera2D
{
    [Export] public PlayerMasterConfig Config { get; set; }

    private float _viewMultiplier = 1f;

    #region Initialization
    // Always start at the authored default view; child camera owns its framing.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new PlayerMasterConfig();
        SetViewMultiplier(1f);
    }
    #endregion

    #region Mouse zoom
    // Handle wheel events only after UI has had a chance to consume scrolling.
    // =========================================================
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton wheel || !wheel.Pressed) return;
        float direction = wheel.ButtonIndex switch
        {
            MouseButton.WheelDown => 1f,
            MouseButton.WheelUp => -1f,
            _ => 0f
        };
        if (direction == 0f) return;

        float previous = _viewMultiplier;
        SetViewMultiplier(_viewMultiplier + direction * Mathf.Max(0.01f, Config.CameraZoomWheelStep));
        if (!Mathf.IsEqualApprox(previous, _viewMultiplier))
            GetViewport().SetInputAsHandled();
    }

    // A 1.25 view multiplier makes 25% more world visible per screen dimension.
    // Camera2D zoom is inverse to world-view size: default 2.0 -> minimum 1.6.
    // =========================================================
    private void SetViewMultiplier(float factor)
    {
        _viewMultiplier = Mathf.Clamp(factor, 1f, Mathf.Max(1f, Config.CameraMaxViewMultiplier));
        Zoom = Vector2.One * (Mathf.Max(0.01f, Config.CameraZoom) / _viewMultiplier);
    }
    #endregion
}
