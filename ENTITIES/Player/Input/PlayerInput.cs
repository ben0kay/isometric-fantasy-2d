// Central player controls: movement, mouse tool aim, RShift sprint and double-tap movement-direction dodge.
using Godot;

public partial class PlayerInput : Node
{
    [Signal] public delegate void ToolUseRequestedEventHandler(Vector2 worldAim);
    [Signal] public delegate void AlternateUseRequestedEventHandler(Vector2 worldAim);
    [Signal] public delegate void UtilityRequestedEventHandler(bool held, Vector2 worldAim);
    [Signal] public delegate void StarterSelectionRequestedEventHandler(int selection);
    [Signal] public delegate void PickupRequestedEventHandler();
    public bool PickupInteractionEnabled { get; set; }
    [Signal] public delegate void HotbarSelectionRequestedEventHandler(int slot);
    public bool HotbarSelectionEnabled { get; set; }
    [Signal] public delegate void DodgeRequestedEventHandler(Vector2 worldDirection);
    [Signal] public delegate void InventoryToggleRequestedEventHandler();
    [Signal] public delegate void InventoryCloseRequestedEventHandler();
    [Signal] public delegate void CommandSuppressionChangedEventHandler(bool suppressed);
    public bool InventoryOpen { get; private set; }

    public bool SprintHeld { get; private set; }
    public float DodgeDoubleTapSeconds { get; set; } = 0.28f;
    private long _lastShiftPressMs = -100000;
    private bool _shiftDown;
    private Window _window;
    // Release held controls even when focus/UI consumes the corresponding release event.
    // =========================================================
    public override void _Ready() { _window=GetWindow(); _window.FocusExited+=ResetMovementActions; }
    // Unsubscribe window events with the input owner.
    // =========================================================
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(_window)) _window.FocusExited-=ResetMovementActions; }
    // Pausing must not retain a guard/sprint after an RMB/key release was consumed by the pause UI.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationPaused) ResetMovementActions(); }
    // Releases never start gameplay actions and must work over UI controls.
    // =========================================================
    public override void _Input(InputEvent input)
    {
        if (!GetTree().Paused && input is InputEventKey toggle && toggle.Pressed && !toggle.Echo)
        {
            if (input.IsActionPressed("player_inventory"))
            { EmitSignal(SignalName.InventoryToggleRequested); GetViewport().SetInputAsHandled(); return; }
            if (InventoryOpen && toggle.Keycode==Key.Escape)
            { EmitSignal(SignalName.InventoryCloseRequested); GetViewport().SetInputAsHandled(); return; }
        }
        if (input is InputEventMouseButton m && m.ButtonIndex==MouseButton.Right && !m.Pressed)
            EmitSignal(SignalName.UtilityRequested,false,Vector2.Zero);
        if (input is InputEventKey k && !k.Pressed && k.Location==KeyLocation.Right && (k.Keycode==Key.Shift || k.PhysicalKeycode==Key.Shift))
        { _shiftDown=false; SprintHeld=false; }
    }

    #region Input events
    // Mouse requests remain centralized; right Shift handles sprint and an optional dodge request.
    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (InventoryOpen) return;
        if (PickupInteractionEnabled && input.IsActionPressed("player_interact") && input is InputEventKey pickup && !pickup.Echo)
        { EmitSignal(SignalName.PickupRequested); GetViewport().SetInputAsHandled(); return; }
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            var player=GetParent<Node2D>(); Vector2 aim=player.GetGlobalMousePosition()-player.GlobalPosition;
            if (mouse.ButtonIndex==MouseButton.Left) EmitSignal(SignalName.ToolUseRequested,aim);
            else if (mouse.ButtonIndex==MouseButton.Middle) EmitSignal(SignalName.AlternateUseRequested,aim);
            else if (mouse.ButtonIndex==MouseButton.Right) EmitSignal(SignalName.UtilityRequested,true,aim);
            else return;
            GetViewport().SetInputAsHandled(); return;
        }
        if (input is InputEventKey select && select.Pressed && !select.Echo)
        {
            Key code=select.PhysicalKeycode==Key.None ? select.Keycode : select.PhysicalKeycode;
            if (HotbarSelectionEnabled)
            {
                int hotbar=code>=Key.Key1 && code<=Key.Key9 ? (int)code-(int)Key.Key1 : -1;
                if (hotbar>=0) { EmitSignal(SignalName.HotbarSelectionRequested,hotbar); GetViewport().SetInputAsHandled(); return; }
            }
            int slot=code==Key.Key1 ? 0 : code==Key.Key2 ? 1 : code==Key.Key3 ? 2 : -1;
            if (slot>=0) { EmitSignal(SignalName.StarterSelectionRequested,slot); GetViewport().SetInputAsHandled(); return; }
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
        Vector2 movementDirection = ReadMovement();
        if (movementDirection.LengthSquared() > 0.01f)
            EmitSignal(SignalName.DodgeRequested, movementDirection.Normalized());
    }

    // Clear held sprint/tap history when player dies or input becomes unavailable.
    // =========================================================
    public void ResetMovementActions()
    { SprintHeld = false; _shiftDown = false; _lastShiftPressMs = -100000; EmitSignal(SignalName.UtilityRequested,false,Vector2.Zero); }
    #endregion

    // Inventory transitions explicitly release held controls and notify the existing movement owner.
    // =========================================================
    public void SetInventoryOpen(bool open)
    {
        if (InventoryOpen==open) return;
        InventoryOpen=open; ResetMovementActions(); EmitSignal(SignalName.CommandSuppressionChanged,open);
    }

    #region Read intents
    // Cursor reads remain centralized; combat/tool code does not poll mouse independently.
    // =========================================================
    public Vector2 ReadToolAim(Vector2 worldOrigin) =>
        GetParent<Node2D>().GetGlobalMousePosition() - worldOrigin;

    // Normalize diagonals so they are not faster than cardinal movement.
    // =========================================================
    public Vector2 ReadMovement() => InventoryOpen ? Vector2.Zero : Input.GetVector("player_left", "player_right", "player_up", "player_down");
    #endregion
}
