// Reserved production coordinator for the one shared wind simulation.
// =========================================================
// Not connected to a scene and intentionally contains no behaviour.
// When implemented, it should publish WindState for all consumers instead
// of letting tree, grass, rain or fog controllers invent their own wind.
using Godot;

public partial class WindController : Node
{
}
