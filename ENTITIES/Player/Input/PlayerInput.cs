// Central player controls: movement, mouse tool aim, RShift sprint and double-tap cursor dodge.
using Godot;

public partial class PlayerInput : Node
{
    [Signal] public delegate void ToolUseRequestedEventHandler(Vector2 worldAim);
    [Signal] public delegate void DodgeRequestedEventHandler(Vector2 worldDirection);

    public bool SprintHeld { get; private set; }
    public float DodgeDoubleTapSeconds { get; set; } = 0.28f;
    private long _lastShiftPressMs = -100000;
    private bool _shiftDown;

    #region Input events
    // Mouse requests remain centralized; right Shift handles sprint and an optional dodge request.
    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        {
            var player = GetParent<Node2D>();
            Vector2 aim = player.GetGlobalMousePosition() - player.GlobalPosition;
            EmitSignal(SignalName.ToolUseRequested, aim);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is not InputEventKey key || key.Echo
            || (key.Keycode != Key.Shift && key.PhysicalKeycode != Key.Shift)
            || key.Location != KeyLocation.Right) return;

        if (!key.Pressed) { _shiftDown = false; SprintHeld = false; return; }
        if (_shiftDown) return;
        _shiftDown = true; SprintHeld = true;
        long now = (long)Time.GetTicksMsec();
        bool doubleTap = now - _lastShiftPressMs <= Mathf.Max(0.1f, DodgeDoubleTapSeconds) * 1000f;
        _lastShiftPressMs = now;
        if (!doubleTap) return;
        _lastShiftPressMs = -100000;
        var playerNode = GetParent<Node2D>();
        Vector2 aimDirection = playerNode.GetGlobalMousePosition() - playerNode.GlobalPosition;
        if (aimDirection.LengthSquared() > 1f)
            EmitSignal(SignalName.DodgeRequested, aimDirection.Normalized());
    }

    // Clear held sprint/tap history when player dies or input becomes unavailable.
    // =========================================================
    public void ResetMovementActions()
    { SprintHeld = false; _shiftDown = false; _lastShiftPressMs = -100000; }
    #endregion

    #region Read intents
    // Cursor reads remain centralized; combat/tool code does not poll mouse independently.
    // =========================================================
    public Vector2 ReadToolAim(Vector2 worldOrigin) =>
        GetParent<Node2D>().GetGlobalMousePosition() - worldOrigin;

    // Normalize diagonals so they are not faster than cardinal movement.
    // =========================================================
    public Vector2 ReadMovement() => Input.GetVector("player_left", "player_right", "player_up", "player_down");
    #endregion
}
