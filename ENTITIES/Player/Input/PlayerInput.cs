// Reads existing arrow-key actions without knowing about the test player's physics.
using Godot;

public partial class PlayerInput : Node
{
    // Input.GetVector limits diagonals so they are not faster than cardinal movement.
    // =========================================================
    public Vector2 ReadMovement() => Input.GetVector("player_left", "player_right", "player_up", "player_down");
}
