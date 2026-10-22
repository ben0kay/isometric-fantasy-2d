// FIRE2: event-driven, temporary per-burning-tree GPU flame/smoke/ember effects.
// Spawned only when an oak ignites; one light; no CPU particle simulation or idle tree process.
using Godot;
using System;

public partial class BurningTreeEffect : Node2D
{
    public BurnableTree Tree { get; set; }
    public FireMasterConfig Config { get; set; }
    public DebugWeather Weather { get; set; }
    public event Action<BurningTreeEffect> Finished;

    private readonly FireErosionAdapter _erosion = new();
    private GpuParticles2D _flames, _smoke, _embers;
    private ParticleProcessMaterial _flameMotion, _smokeMotion, _emberMotion;
    private PointLight2D _light;
    private float _lastWind = float.NaN, _time, _tailRemaining;
    private bool _burning = true, _ended;

    private static Texture2D _flameArt, _smokeArt, _emberArt;
    private static GradientTexture2D _lightArt;
    private static Texture2D _burnNoise;

    #region Startup
    // Read shared resources once, attach under the existing tree root, then listen for ending.
    // =========================================================
    public override void _Ready()
    {
        if (Tree == null || Config == null || Weather == null) { QueueFree(); return; }
        ProcessPriority = 15;
        EnsureArtwork();
        Node2D root = (Node2D)Tree.GetParent();
        Sprite2D sprite = root.GetNode<Sprite2D>("Sprite");
        if (_erosion.Attach(sprite, _burnNoise))
            Tree.SetFoliageErosion(true);

        // A canopy-spanning band of small flames; the base of the rock-solid trunk stays visible.
        Vector2 impact = root.ToLocal(Tree.WorldImpactPosition);
        _flames = MakeEmitter("Flames", _flameArt, Mathf.Clamp(Config.FlameParticles, 1, 180), 0.62f,
            new Vector2(Mathf.Clamp(impact.X * 0.3f, -18f, 18f),
                        Mathf.Clamp(impact.Y, -135f, -50f)), new Vector3(23f, 35f, 1f),
            new Color(1f, 0.94f, 0.85f, 1f), out _flameMotion);
        _flameMotion.ScaleMin = 0.68f; _flameMotion.ScaleMax = 1.4f; _flameMotion.Spread = 24f;

        _smoke = MakeEmitter("Smoke", _smokeArt, Mathf.Clamp(Config.SmokeParticles, 1, 130), 2.15f,
            new Vector2(0f, -115f), new Vector3(22f, 14f, 1f),
            new Color(0.80f, 0.78f, 0.75f, 0.48f), out _smokeMotion);
        _smokeMotion.ScaleMin = 0.55f; _smokeMotion.ScaleMax = 1.65f; _smokeMotion.Spread = 18f;

        _embers = MakeEmitter("Embers", _emberArt, Mathf.Clamp(Config.EmberParticles, 1, 120), 1.55f,
            new Vector2(0f, -93f), new Vector3(28f, 30f, 1f),
            new Color(1f, 0.76f, 0.42f, 0.95f), out _emberMotion);
        _emberMotion.ScaleMin = 0.40f; _emberMotion.ScaleMax = 1.1f; _emberMotion.Spread = 37f;

        _light = new PointLight2D {
            Name = "TemporaryFirelight", Position = new Vector2(0f, -82f), Texture = _lightArt,
            TextureScale = Mathf.Clamp(Config.FireLightRadius, 0.1f, 4f),
            Color = Config.FireLightColour, Energy = 0f, Enabled = Config.FireLightEnergy > 0f
        };
        AddChild(_light);

        Tree.BurnCompleted += OnBurnCompleted;
        Tree.BurnReset += OnBurnReset;
        RefreshWind(true);
    }

    // Every effect uses the same generated textures, not one bitmap per burning tree.
    // =========================================================
    private static void EnsureArtwork()
    {
        if (_flameArt != null) return;

        var flame = Image.CreateEmpty(14, 24, false, Image.Format.Rgba8);
        flame.Fill(Colors.Transparent);
        for (int y = 0; y < 24; y++)
        {
            float t = y / 23f;
            float halfWidth = 0.6f + 5.5f * t * t;
            float curve = Mathf.Sin((1f - t) * Mathf.Pi * 1.5f) * 1.4f;
            for (int x = 0; x < 14; x++)
            {
                float distance = Mathf.Abs(x - 6.5f - curve) / halfWidth;
                if (distance >= 1f) continue;
                float alpha = Mathf.Clamp((1f - distance) * 1.75f, 0f, 1f) * (0.48f + 0.52f * t);
                flame.SetPixel(x, y, new Color(1f, 0.25f + 0.7f * t, 0.025f + 0.09f * t, alpha));
            }
        }
        _flameArt = ImageTexture.CreateFromImage(flame);

        var smoke = Image.CreateEmpty(28, 28, false, Image.Format.Rgba8);
        smoke.Fill(Colors.Transparent);
        for (int y = 0; y < 28; y++) for (int x = 0; x < 28; x++)
        {
            float distance = new Vector2(x - 13.5f, y - 13.5f).Length() / 13.5f;
            float alpha = Mathf.Pow(Mathf.Max(0f, 1f - distance * distance), 2f) * 0.29f;
            smoke.SetPixel(x, y, new Color(0.18f, 0.17f, 0.18f, alpha));
        }
        _smokeArt = ImageTexture.CreateFromImage(smoke);

        var ember = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8);
        ember.Fill(Colors.Transparent);
        ember.SetPixel(2, 1, new Color(1f, 0.9f, 0.55f));
        ember.SetPixel(1, 2, new Color(1f, 0.43f, 0.08f, 0.8f));
        ember.SetPixel(2, 2, new Color(1f, 0.94f, 0.6f));
        ember.SetPixel(3, 2, new Color(1f, 0.48f, 0.12f, 0.8f));
        ember.SetPixel(2, 3, new Color(1f, 0.57f, 0.16f, 0.8f));
        _emberArt = ImageTexture.CreateFromImage(ember);

        var gradient = new Gradient {
            Offsets = new float[] { 0f, 0.20f, 1f },
            Colors = new Color[] { new(1f, 0.82f, 0.56f, 1f), new(1f, 0.5f, 0.2f, 0.75f), new(1f, 0.32f, 0.08f, 0f) }
        };
        _lightArt = new GradientTexture2D {
            Width = 256, Height = 256, Gradient = gradient,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f)
        };
        _burnNoise = GD.Load<Texture2D>("res://DEBUG/TopDownTest/Ground/Noise/GroundNoise.tres");
    }

    // One GPU emitter with a distinct motion profile; no per-particle nodes or timers.
    // =========================================================
    private GpuParticles2D MakeEmitter(string name, Texture2D art, int count, float life,
        Vector2 localOrigin, Vector3 emissionExtents, Color tint, out ParticleProcessMaterial motion)
    {
        motion = new ParticleProcessMaterial {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = emissionExtents, Gravity = Vector3.Zero,
            Direction = Vector3.Up, Spread = 25f,
            InitialVelocityMin = 20f, InitialVelocityMax = 45f,
            Color = tint
        };
        motion.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        var emitter = new GpuParticles2D {
            Name = name, Position = localOrigin, Amount = count, Lifetime = life,
            Preprocess = 0f, LocalCoords = false, Emitting = false, AmountRatio = 0.001f,
            Texture = art, TextureFilter = TextureFilterEnum.Nearest, ProcessMaterial = motion,
            ZIndex = 1,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        AddChild(emitter);
        return emitter;
    }
    #endregion

    #region Runtime presentation
    // Update only a burning oak; noise erosion lives entirely on the existing sprite GPU shader.
    // =========================================================
    public override void _Process(double delta)
    {
        if (_ended) return;
        _time += (float)delta;
        RefreshWind();

        if (_burning && Tree.State == FireTreeState.Burning)
        {
            float full = Mathf.Max(0.1f, Config.TransitionSeconds);
            float visual = Mathf.Clamp(
                (Tree.BurnProgress * (Config.IgnitionDelaySeconds + full) - Config.IgnitionDelaySeconds) / full, 0f, 1f);
            if (_erosion.IsActive && !_erosion.Update(visual, Weather.WindStrength, Config.CharTint))
            {
                _erosion.Restore();
                Tree.SetFoliageErosion(false); // Fall back to FIRE1 crossfade.
            }

            float ignition = Mathf.Clamp(_time / 0.6f, 0f, 1f);
            float burnout = 1f - EaseRange(0.67f, 1f, visual);
            float intensity = ignition * burnout;
            SetEmission(_flames, intensity * Config.FlameIntensity);
            SetEmission(_smoke, Mathf.Max(0.1f, intensity) * Config.SmokeIntensity);
            SetEmission(_embers, intensity * Config.EmberIntensity);
            _light.Enabled = Config.FireLightEnergy > 0f && intensity > 0.001f;
            float flicker = 0.82f + 0.12f * Mathf.Sin(_time * 19f)
                + 0.06f * Mathf.Sin(_time * 39.7f + 1.3f);
            _light.Energy = Mathf.Max(0f, Config.FireLightEnergy) * intensity * Mathf.Max(0.1f, flicker);
        }
        else if (!_burning)
        {
            _tailRemaining -= (float)delta;
            float tailDuration = Mathf.Max(0f, Config.SmokeTailSeconds);
            float smokeFade = Mathf.Clamp((_tailRemaining - 2.2f) / Mathf.Max(0.01f, tailDuration), 0f, 1f);
            SetEmission(_smoke, smokeFade * Config.SmokeIntensity * 0.38f);
            _light.Energy = Mathf.Max(0f, _light.Energy - (float)delta * Config.FireLightEnergy * 3f);
            if (_tailRemaining <= 0f) Finish();
        }
    }

    // Keep velocity from the single shared wind source; changes affect newly emitted particles.
    // =========================================================
    private void RefreshWind(bool force = false)
    {
        float wind = Weather.WindStrength;
        if (!force && Mathf.IsEqualApprox(_lastWind, wind)) return;
        _lastWind = wind;
        ApplyMotion(_flameMotion, 42f, wind * Config.FlameWindResponse);
        ApplyMotion(_smokeMotion, 23f, wind * Config.SmokeWindResponse);
        ApplyMotion(_emberMotion, 54f, wind * Config.EmberWindResponse);
    }

    // Edge easing rather than Mathf.SmoothStep's between-values interpolation.
    // =========================================================
    private static float EaseRange(float min, float max, float value)
    {
        float t = Mathf.Clamp((value - min) / Mathf.Max(0.0001f, max - min), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    // Motion direction is an angle and speed; no duplicated local wind simulation.
    // =========================================================
    private static void ApplyMotion(ParticleProcessMaterial motion, float rise, float push)
    {
        Vector2 v = new(push, -rise);
        motion.Direction = new Vector3(v.X, v.Y, 0f).Normalized();
        motion.InitialVelocityMin = v.Length() * 0.55f;
        motion.InitialVelocityMax = v.Length() * 1.3f;
    }

    // A zero ratio also stops emission; existing GPU particles can finish their lifetimes.
    // =========================================================
    private static void SetEmission(GpuParticles2D emitter, float amount)
    {
        float ratio = Mathf.Clamp(amount, 0f, 1f);
        emitter.AmountRatio = Mathf.Max(0.001f, ratio);
        emitter.Emitting = ratio > 0.002f;
    }
    #endregion

    #region Completion and removal
    // FIRE1 already performed the final art swap and cached shadow update.
    // Keep smoke alive briefly while flames and embers stop.
    // =========================================================
    private void OnBurnCompleted()
    {
        if (_ended) return;
        _erosion.Restore();
        _burning = false;
        _tailRemaining = Mathf.Max(0f, Config.SmokeTailSeconds) + 2.2f;
        SetEmission(_flames, 0f); SetEmission(_embers, 0f);
    }

    // Removing the debug FireTest node cancels any FX immediately without resetting tree state.
    // =========================================================
    public void Cancel() => Finish();

    // Reset is immediate: no lingering light, smoke or erosion over a restored healthy oak.
    // =========================================================
    private void OnBurnReset() => Finish();

    // Inform the owner so effect-slot accounting can release a completed or cancelled tree.
    // =========================================================
    private void Finish()
    {
        if (_ended) return;
        _ended = true;
        _erosion.Restore();
        if (Tree != null && GodotObject.IsInstanceValid(Tree) && Tree.State == FireTreeState.Burning)
            Tree.SetFoliageErosion(false);
        Finished?.Invoke(this);
        QueueFree();
    }

    // Subscriptions and material ownership must not survive a removed effect.
    // =========================================================
    public override void _ExitTree()
    {
        _erosion.Restore();
        if (GodotObject.IsInstanceValid(Tree))
        {
            Tree.BurnCompleted -= OnBurnCompleted;
            Tree.BurnReset -= OnBurnReset;
        }
    }
    #endregion
}
