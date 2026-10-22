// Reusable ignition lifecycle. Only actively burning receivers process time.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;
public partial class Burnable : Node
{
    [Export] public FireProfile Profile { get; set; }
    [Signal] public delegate void IgnitedEventHandler(Vector2 worldImpactPosition, string source);
    [Signal] public delegate void BurnCompletedEventHandler();
    [Signal] public delegate void BurnResetEventHandler();
    public BurnState State { get; private set; }
    public float BurnProgress { get; private set; }
    public Vector2 WorldImpactPosition { get; private set; }
    public string IgnitionSource { get; private set; } = "";
    private float _elapsed;
    // Register a receiver without allocating effects or running an idle timer.
    // =========================================================
    public override void _Ready() { AddToGroup("world_burnables"); SetProcess(false); }
    // A shared contract for UI, magic and later lightning callers.
    // =========================================================
    public bool TryIgnite(Vector2 impact, string source = "Interaction")
    {
        if (Profile == null || !Profile.Enabled || State == BurnState.Burning) return false;
        if (State == BurnState.Burnt) { if (!Profile.MayReignite) return false; ResetBurn(); }
        State = BurnState.Burning; BurnProgress = 0f; _elapsed = 0f;
        WorldImpactPosition = impact; IgnitionSource = source; SetProcess(true);
        EmitSignal(SignalName.Ignited, impact, source); return true;
    }
    // Reset is tooling/repair support, not an extinguishing simulation.
    // =========================================================
    public void ResetBurn()
    {
        State = BurnState.Healthy; BurnProgress = 0f; _elapsed = 0f; IgnitionSource = "";
        SetProcess(false); EmitSignal(SignalName.BurnReset);
    }
    // In-memory chunk restoration resumes the same lifecycle; durable storage belongs to SAVE/P4.
    // =========================================================
    public void RestoreProgress(float progress)
    {
        if (progress >= 1 && Profile != null)
        {
            State = BurnState.Burnt; BurnProgress = 1; SetProcess(false);
            WorldImpactPosition = ((Node2D)GetParent()).GlobalPosition; IgnitionSource = "ChunkRestore";
            EmitSignal(SignalName.Ignited,WorldImpactPosition,IgnitionSource); EmitSignal(SignalName.BurnCompleted); return;
        }
        if (progress <= 0 || !TryIgnite(((Node2D)GetParent()).GlobalPosition,"ChunkRestore")) return;
        _elapsed = progress * (Mathf.Max(0f,Profile.IgnitionDelaySeconds) + Mathf.Max(0.1f,Profile.TransitionSeconds));
        _Process(0);
    }
    // Advance using real seconds, independent of accelerated calendar time.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        BurnProgress = Mathf.Clamp(_elapsed / (Mathf.Max(0f, Profile.IgnitionDelaySeconds) + Mathf.Max(0.1f, Profile.TransitionSeconds)), 0f, 1f);
        if (BurnProgress < 1f) return;
        State = BurnState.Burnt; SetProcess(false); EmitSignal(SignalName.BurnCompleted);
    }
}
