// Removable tooling only: production fire remains active without this panel.
using Godot;
using FantasyGame.WorldInteractions.Fire;
public partial class WorldFireDebugPanel : CanvasLayer
{
    private Node2D _actors, _player;
    private Label _status;
    private VBoxContainer _body;
    // The World parent finishes generation before this child accepts button presses.
    // =========================================================
    public override void _Ready()
    {
        var world = GetNode<WorldScene>("../.."); _actors = world.ActorsRoot; _player = world.Player;
        SetProcess(false);
    }
    // The shared F9 Environment drawer owns placement/visibility; no standalone overlay remains.
    // =========================================================
    public void AttachTo(VBoxContainer parent)
    {
        if(_body!=null) return;
        var panel=new VBoxContainer(); _body=panel; parent.AddChild(panel);
        panel.AddChild(new Label { Text = "Production fire — FIRE1 / FIRE2" });
        var ignite = new Button { Text = "Ignite nearest tree", FocusMode = Control.FocusModeEnum.None }; panel.AddChild(ignite); ignite.Pressed += IgniteNearest;
        var reset = new Button { Text = "Reset burnt / burning trees", FocusMode = Control.FocusModeEnum.None }; panel.AddChild(reset); reset.Pressed += ResetAll;
        _status = new Label { Text = "Walk within 320 pixels of a tree." }; panel.AddChild(_status);
    }
    // Removing this optional tool also removes its externally hosted widgets.
    // =========================================================
    public override void _ExitTree() { if(GodotObject.IsInstanceValid(_body)) _body.QueueFree(); }
    // Only this world's registered receivers are eligible; distance uses stable tree roots.
    // =========================================================
    public void IgniteNearest()
    {
        Burnable nearest = null; float best = 320f * 320f;
        foreach (Node node in GetTree().GetNodesInGroup("world_burnables"))
        {
            if (node is not Burnable burnable || !_actors.IsAncestorOf(node) || burnable.State != BurnState.Healthy) continue;
            float distance = _player.GlobalPosition.DistanceSquaredTo(((Node2D)node.GetParent()).GlobalPosition);
            if (distance > best) continue; nearest = burnable; best = distance;
        }
        bool lit = nearest != null && nearest.TryIgnite(((Node2D)nearest.GetParent()).GlobalPosition + new Vector2(0, -80), "DeveloperPanel");
        _status.Text = lit ? "Burning: " + nearest.GetParent().Name : "No eligible tree within 320 pixels.";
    }
    // Reset all receivers in this world without touching the independent TopDownTest scene.
    // =========================================================
    public void ResetAll()
    {
        foreach (Node node in GetTree().GetNodesInGroup("world_burnables"))
            if (node is Burnable burnable && _actors.IsAncestorOf(node)) burnable.ResetBurn();
        _status.Text = "All trees restored.";
    }
}
