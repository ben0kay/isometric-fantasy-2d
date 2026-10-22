// Reads existing arrow-key actions without knowing about the test player's physics.
using Godot;

public partial class PlayerInput : Node
{
    [Signal] public delegate void ToolUseRequestedEventHandler(Vector2 worldAim);

    // Centralized tool input; equipped tools never read buttons directly.
    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        {
            var player = GetParent<Node2D>();
            Vector2 aim = player.GetGlobalMousePosition() - player.GlobalPosition;
            EmitSignal(SignalName.ToolUseRequested, aim);
            GetViewport().SetInputAsHandled();
        }
    }

    // Input.GetVector limits diagonals so they are not faster than cardinal movement.
    // =========================================================
    public Vector2 ReadMovement() => Input.GetVector("player_left", "player_right", "player_up", "player_down");
}
