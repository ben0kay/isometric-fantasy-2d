// Reads action intent only; physics and visual presentation live elsewhere.
using Godot;

public partial class PlayerInput : Node
{
    // Read a circularly limited movement vector from the project's Input Map.
    // =========================================================
    public Vector2 ReadMovement() => Godot.Input.GetVector("player_left", "player_right", "player_up", "player_down");
}
