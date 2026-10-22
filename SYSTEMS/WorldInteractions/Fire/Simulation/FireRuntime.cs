// World-scoped event wiring and bounded active effects; no scans in the frame loop.
using Godot;
using System.Collections.Generic;
namespace FantasyGame.WorldInteractions.Fire;
public partial class FireRuntime : Node
{
    [Export] public FireMasterConfig Config { get; set; }
    private readonly Dictionary<Burnable, Burnable.IgnitedEventHandler> _ignition = new();
    private readonly Dictionary<TreeFireAppearance, TreeFireAppearance.AppearanceChangedEventHandler> _appearance = new();
    private readonly Dictionary<Burnable, BurningEffect> _active = new();
    private WindController _wind;
    public int ActiveEffectCount => _active.Count;
    // Bind after scenery, wind, snow and shadow caches exist; future content can register on spawn.
    // =========================================================
    public void Initialize(Node2D actors, EnvironmentRuntime environment)
    {
        Config ??= GD.Load<FireMasterConfig>("res://SYSTEMS/WorldInteractions/Fire/DefaultFireConfig.tres");
        _wind = environment.Wind;
        foreach (Node node in GetTree().GetNodesInGroup("world_burnables"))
            if (node is Burnable burnable && actors.IsAncestorOf(node)) Register(burnable, environment.DayNight.Shadows);
    }
    // Add exactly one handler per receiver; shadow refresh stays independent of developer controls.
    // =========================================================
    public void Register(Burnable receiver, SunShadowRenderer shadows)
    {
        if (_ignition.ContainsKey(receiver)) return;
        Burnable.IgnitedEventHandler ignite = (position, source) => SpawnEffects(receiver);
        receiver.Ignited += ignite; _ignition.Add(receiver, ignite);
        var art = receiver.GetParent().GetNodeOrNull<TreeFireAppearance>("FireAppearance");
        if (art == null) return;
        TreeFireAppearance.AppearanceChangedEventHandler changed = () => shadows.RefreshAppearance((Node2D)receiver.GetParent());
        art.AppearanceChanged += changed; _appearance.Add(art, changed);
    }
    // Remove subscriptions/effects before an owning chunk releases its live actors.
    // =========================================================
    public void Unregister(Burnable receiver)
    {
        if (_ignition.Remove(receiver,out var handler)) receiver.Ignited -= handler;
        var art = receiver.GetParent().GetNodeOrNull<TreeFireAppearance>("FireAppearance");
        if (art != null && _appearance.Remove(art,out var changed)) art.AppearanceChanged -= changed;
        if (_active.TryGetValue(receiver,out var effect)) { effect.Cancel(); _active.Remove(receiver); }
    }
    // Effects are optional and capped; lifecycle still completes when all presentation slots are used.
    // =========================================================
    private void SpawnEffects(Burnable receiver)
    {
        if (receiver.State != BurnState.Burning || !Config.Enabled || !Config.EnableVisualEffects || _active.ContainsKey(receiver)
            || _active.Count >= Mathf.Clamp(Config.MaxActiveEffects, 1, 16)) return;
        var effect = new BurningEffect { Name = "FireEffects", Tree = receiver, Config = Config, Wind = _wind };
        _active.Add(receiver, effect);
        effect.Finished += ended => { if (_active.TryGetValue(receiver, out var current) && current == ended) _active.Remove(receiver); };
        receiver.GetParent().AddChild(effect);
    }
    // A removed world/controller releases subscriptions, materials, lights and particle sets.
    // =========================================================
    public override void _ExitTree()
    {
        foreach (var pair in _ignition) if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.Ignited -= pair.Value;
        foreach (var pair in _appearance) if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.AppearanceChanged -= pair.Value;
        foreach (var effect in new List<BurningEffect>(_active.Values)) if (GodotObject.IsInstanceValid(effect)) effect.Cancel();
        _active.Clear(); _ignition.Clear(); _appearance.Clear();
    }
}
