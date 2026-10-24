// Tree-specific charred alignment and crossfade; collider and live wind/snow materials persist.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;
public partial class TreeFireAppearance : Node
{
    public TreeFireProfile Config => Receiver.Profile as TreeFireProfile;
    public Burnable Receiver { get; private set; }
    [Signal] public delegate void AppearanceChangedEventHandler();
    public BurnState State => Receiver.State;
    public float BurnProgress => Receiver.BurnProgress;
    private Sprite2D _sprite, _overlay;
    private Texture2D _healthy;
    private Vector2 _healthyOffset;
    private Color _healthyTint;
    private TreeHitResponse _hits;
    private float _elapsed;
    private bool _foliageErosion;

    #region Setup and lifecycle API
    // Cache only appearance data; never cache/replace the runtime snow/wind material.
    // =========================================================
    public override void _Ready()
    {
        Receiver = GetParent().GetNode<Burnable>("Burnable");
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _hits = GetParent().GetNodeOrNull<TreeHitResponse>("HitResponse");
        _healthy = Config?.HealthyTexture ?? _sprite.Texture;
        _healthyOffset = _sprite.Offset; _healthyTint = _sprite.SelfModulate;
        Receiver.Ignited += OnIgnited; Receiver.BurnCompleted += OnCompleted; Receiver.BurnReset += ResetAppearance;
        ProcessPriority = 5; SetProcess(false);
    }

    // One fresh ignition, independent of whether the caller is UI, lightning or future magic.
    // =========================================================
    private void OnIgnited(Vector2 worldImpactPosition, string source)
    {
        _hits?.SetHealthyLeaves(false);
        if (Config?.BurntTexture == null) return;
        // Offset is in source pixels; inherited Sprite transform preserves scale and impact wobble.
        _overlay = new Sprite2D {
            Name = "CharredBlend", Texture = Config.BurntTexture, Centered = _sprite.Centered,
            Offset = BurntOffset(), FlipH = _sprite.FlipH, FlipV = _sprite.FlipV,
            TextureFilter = _sprite.TextureFilter, SelfModulate = new Color(1f, 1f, 1f, 0f)
        };
        _sprite.AddChild(_overlay); SetProcess(true);

    }

    // FIRE2 may activate a GPU erosion shader over the live art; FIRE1 crossfade is fallback.
    // The charred overlay then draws BEHIND the healthy sprite so transparency reveals it.
    // =========================================================
    public void SetFoliageErosion(bool enabled)
    {
        if (State != BurnState.Burning || !GodotObject.IsInstanceValid(_overlay)) return;
        _foliageErosion = enabled;
        _overlay.ShowBehindParent = enabled;
        if (enabled)
        {
            _sprite.SelfModulate = _healthyTint;
            _overlay.SelfModulate = Colors.White;
        }
    }

    // Reset keeps whatever material the snow adapter currently owns, instead of restoring a stale one.
    // =========================================================
    private void ResetAppearance()
    {
        _foliageErosion = false;
        _sprite.SetInstanceShaderParameter("foliage_healthy", true);
        RemoveOverlay(); _sprite.Texture = _healthy; _sprite.Offset = _healthyOffset; _sprite.SelfModulate = _healthyTint;

        _hits?.SetHealthyLeaves(true); SetProcess(false);
        EmitSignal(SignalName.AppearanceChanged);
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
        Vector2 centeredDelta = _sprite.Centered ? (Config.BurntTexture.GetSize() - _healthy.GetSize()) * 0.5f : Vector2.Zero;
        return _healthyOffset + alive - dead + centeredDelta;
    }

    // Only active burns process; use elapsed seconds rather than the accelerated day/night clock.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("TreeFireAppearance.Process");
        float delay = Mathf.Max(0f, Config.IgnitionDelaySeconds), duration = Mathf.Max(0.1f, Config.TransitionSeconds);
        _elapsed = BurnProgress * (delay + duration);
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
    }

    // Commit the charred appearance before the system releases fire effects.
    // =========================================================
    private void OnCompleted()
    {
        _sprite.SetInstanceShaderParameter("foliage_healthy", false);
        if (Config?.BurntTexture != null)
        { _sprite.Texture = Config.BurntTexture; _sprite.Offset = BurntOffset(); _sprite.SelfModulate = _healthyTint; }
        // Without replacement art, retain the original texture/tint; only healthy foliage effects stop.
        _foliageErosion = false;
        RemoveOverlay(); SetProcess(false);
        EmitSignal(SignalName.AppearanceChanged);
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
        if (GodotObject.IsInstanceValid(Receiver))
        {
            Receiver.Ignited -= OnIgnited; Receiver.BurnCompleted -= OnCompleted; Receiver.BurnReset -= ResetAppearance;
        }
        if (GodotObject.IsInstanceValid(_sprite)) ResetAppearance();
    }
    #endregion
}
