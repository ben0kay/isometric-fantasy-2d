// Optional tree-local FIRE1 state machine; the same tree body/collider persists throughout.
using Godot;

public enum FireTreeState { Healthy, Burning, Burnt }

public partial class BurnableTree : Node
{
    [Export] public FireMasterConfig Config { get; set; }
    [Signal] public delegate void IgnitedEventHandler(Vector2 worldImpactPosition, string source);
    [Signal] public delegate void BurnCompletedEventHandler();
    [Signal] public delegate void BurnResetEventHandler();
    [Signal] public delegate void AppearanceChangedEventHandler();
    public FireTreeState State { get; private set; } = FireTreeState.Healthy;
    public float BurnProgress { get; private set; }
    public Vector2 WorldImpactPosition { get; private set; }
    public string IgnitionSource { get; private set; } = "";
    private Sprite2D _sprite, _overlay;
    private Texture2D _healthy;
    private Vector2 _healthyOffset;
    private Color _healthyTint;
    private TreeHitFeedback _hits;
    private float _elapsed;
    private bool _foliageErosion;

    #region Setup and lifecycle API
    // Cache only appearance data; never cache/replace the runtime snow/wind material.
    // =========================================================
    public override void _Ready()
    {
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _hits = GetParent().GetNodeOrNull<TreeHitFeedback>("HitFeedback");
        _healthy = Config?.HealthyTexture ?? _sprite.Texture;
        _healthyOffset = _sprite.Offset; _healthyTint = _sprite.SelfModulate;
        AddToGroup("debug_burnable_trees"); SetProcess(false);
    }

    // One fresh ignition, independent of whether the caller is UI, lightning or future magic.
    // =========================================================
    public bool TryIgnite(Vector2 worldImpactPosition, string source = "Debug")
    {
        if (Config == null || !Config.Enabled || Config.BurntTexture == null || State == FireTreeState.Burning) return false;
        if (State == FireTreeState.Burnt)
        {
            if (!Config.MayReignite) return false;
            ResetBurn();
        }
        WorldImpactPosition = worldImpactPosition; IgnitionSource = source;
        State = FireTreeState.Burning; BurnProgress = 0f; _elapsed = 0f;
        _hits?.SetHealthyLeaves(false);
        // Offset is in source pixels; inherited Sprite transform preserves scale and impact wobble.
        _overlay = new Sprite2D {
            Name = "Fire1CharredBlend", Texture = Config.BurntTexture, Centered = _sprite.Centered,
            Offset = BurntOffset(), FlipH = _sprite.FlipH, FlipV = _sprite.FlipV,
            TextureFilter = _sprite.TextureFilter, SelfModulate = new Color(1f, 1f, 1f, 0f)
        };
        _sprite.AddChild(_overlay); SetProcess(true);
        EmitSignal(SignalName.Ignited, worldImpactPosition, source); return true;
    }

    // FIRE2 may activate a GPU erosion shader over the live art; FIRE1 crossfade is fallback.
    // The charred overlay then draws BEHIND the healthy sprite so transparency reveals it.
    // =========================================================
    public void SetFoliageErosion(bool enabled)
    {
        if (State != FireTreeState.Burning || !GodotObject.IsInstanceValid(_overlay)) return;
        _foliageErosion = enabled;
        _overlay.ShowBehindParent = enabled;
        if (enabled)
        {
            _sprite.SelfModulate = _healthyTint;
            _overlay.SelfModulate = Colors.White;
        }
    }

    // Debug reset keeps whatever material SnowTest currently owns, instead of restoring a stale one.
    // =========================================================
    public void ResetBurn()
    {
        _foliageErosion = false;
        RemoveOverlay(); _sprite.Texture = _healthy; _sprite.Offset = _healthyOffset; _sprite.SelfModulate = _healthyTint;
        State = FireTreeState.Healthy; BurnProgress = 0f; _elapsed = 0f; IgnitionSource = "";
        _hits?.SetHealthyLeaves(true); SetProcess(false);
        EmitSignal(SignalName.AppearanceChanged); EmitSignal(SignalName.BurnReset);
    }
    #endregion

    #region Minimal transition
    // Mirrored art reflects anchors within each different source width, keeping ground contact stable.
    // =========================================================
    private Vector2 BurntOffset()
    {
        Vector2 alive = Config.HealthyAnchor, dead = Config.BurntAnchor;
        if (_sprite.FlipH) { alive.X = _healthy.GetWidth() - alive.X; dead.X = Config.BurntTexture.GetWidth() - dead.X; }
        if (_sprite.FlipV) { alive.Y = _healthy.GetHeight() - alive.Y; dead.Y = Config.BurntTexture.GetHeight() - dead.Y; }
        return _healthyOffset + alive - dead;
    }

    // Only active burns process; use elapsed seconds rather than the accelerated day/night clock.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float delay = Mathf.Max(0f, Config.IgnitionDelaySeconds), duration = Mathf.Max(0.1f, Config.TransitionSeconds);
        BurnProgress = Mathf.Clamp(_elapsed / (delay + duration), 0f, 1f);
        float blend = Mathf.Clamp((_elapsed - delay) / duration, 0f, 1f);
        blend = blend * blend * (3f - 2f * blend);
        if (_foliageErosion)
        {
            // Per-pixel erosion controls the alpha; keep the full charred art underneath.
            _sprite.SelfModulate = _healthyTint;
            _overlay.SelfModulate = Colors.White;
        }
        else
        {
            _sprite.SelfModulate = new Color(_healthyTint, _healthyTint.A * (1f - blend));
            _overlay.SelfModulate = new Color(1f, 1f, 1f, blend);
        }
        if (BurnProgress < 1f) return;
        _sprite.Texture = Config.BurntTexture; _sprite.Offset = BurntOffset(); _sprite.SelfModulate = _healthyTint;
        _foliageErosion = false;
        RemoveOverlay(); State = FireTreeState.Burnt; SetProcess(false);
        EmitSignal(SignalName.AppearanceChanged); EmitSignal(SignalName.BurnCompleted);
    }

    // One short-lived overlay per active oak, never a replacement tree or permanent duplicate sprite.
    // =========================================================
    private void RemoveOverlay()
    {
        if (!GodotObject.IsInstanceValid(_overlay)) return;
        _sprite.RemoveChild(_overlay); _overlay.QueueFree(); _overlay = null;
    }

    // Detaching the optional receiver restores appearance and healthy axe feedback.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_sprite)) ResetBurn();
    }
    #endregion
}
