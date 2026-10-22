// FIRE2: event-driven, temporary per-burning-tree GPU flame/smoke/ember effects.
// Spawned only when an oak ignites; one light; no CPU particle simulation or idle tree process.
using Godot;
using System;
namespace FantasyGame.WorldInteractions.Fire;

public partial class BurningEffect : Node2D
{
    public Burnable Tree { get; set; }
    public FireMasterConfig Config { get; set; }
    public WindController Wind { get; set; }
    public event Action<BurningEffect> Finished;

    private TreeFireAppearance _appearance;
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
        if (Tree == null || Config == null || Wind == null) { QueueFree(); return; }
        ProcessPriority = 15;
        EnsureArtwork();
        Node2D root = (Node2D)Tree.GetParent();
        Sprite2D sprite = root.GetNode<Sprite2D>("Sprite");
        _appearance = root.GetNodeOrNull<TreeFireAppearance>("FireAppearance");
        if (_appearance != null && _erosion.Attach(sprite, _burnNoise))
            _appearance?.SetFoliageErosion(true);

        // First build fallback box emitters; a cached alpha-mask turns them into
        // silhouette-matched emission points for this exact tree texture/shape.
        Vector2 impact = root.ToLocal(Tree.WorldImpactPosition);
        _flames = MakeEmitter("Flames", _flameArt, Mathf.Clamp(Config.FlameParticles, 1, 180), 0.62f,
            new Vector2(Mathf.Clamp(impact.X * 0.3f, -18f, 18f),
                        Mathf.Clamp(impact.Y, -135f, -50f)), new Vector3(23f, 35f, 1f),
            new Color(1f, 0.94f, 0.85f, 1f), out _flameMotion);
        float puff = Mathf.Clamp(Config.FlamePuffiness, 0.85f, 1.35f);
        _flameMotion.ScaleMin = 0.62f * puff;
        _flameMotion.ScaleMax = 1.32f * puff;
        _flameMotion.Spread = 24f;

        _smoke = MakeEmitter("Smoke", _smokeArt, Mathf.Clamp(Config.SmokeParticles, 1, 130), 2.15f,
            new Vector2(0f, -115f), new Vector3(22f, 14f, 1f),
            new Color(0.80f, 0.78f, 0.75f, 0.48f), out _smokeMotion);
        _smokeMotion.ScaleMin = 0.55f; _smokeMotion.ScaleMax = 1.65f; _smokeMotion.Spread = 18f;

        _embers = MakeEmitter("Embers", _emberArt, Mathf.Clamp(Config.EmberParticles, 1, 120), 1.55f,
            new Vector2(0f, -93f), new Vector3(28f, 30f, 1f),
            new Color(1f, 0.76f, 0.42f, 0.95f), out _emberMotion);
        _emberMotion.ScaleMin = 0.40f; _emberMotion.ScaleMax = 1.1f; _emberMotion.Spread = 37f;

        // One GPU emission-point mask per unique sprite shape/flip/profile.
        // Emitters follow the artwork's outer edges rather than a centre rectangle.
        if (CanopyFireEmissionMask.TryGet(sprite, _appearance?.Config, out CanopyFireMask canopy))
        {
            CanopyFireEmissionMask.Apply(canopy, _flames, _flameMotion);
            CanopyFireEmissionMask.Apply(canopy, _smoke, _smokeMotion);
            CanopyFireEmissionMask.Apply(canopy, _embers, _emberMotion);
        }
        else if ((_appearance?.Config?.UseCanopyEmissionMask ?? false))
            GD.PushWarning("FIRE2: foliage mask unavailable; keeping box emitter for this tree.");

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

        // Slightly rounded, overlapping lobes instead of the old sharp triangles.
        // The glow/alpha gradient preserves warmth without turning flames into large clouds.
        var flame = Image.CreateEmpty(20, 28, false, Image.Format.Rgba8);
        flame.Fill(Colors.Transparent);
        for (int y = 0; y < 28; y++)
            for (int x = 0; x < 20; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float puff = Mathf.Max(
                    Mathf.Max(Puff(p, new Vector2(10f, 19.5f), new Vector2(8f, 8.0f)),
                              Puff(p, new Vector2(7f, 12.8f), new Vector2(5.6f, 7.0f))),
                    Mathf.Max(Puff(p, new Vector2(13.2f, 11.5f), new Vector2(5.2f, 7.2f)),
                              Puff(p, new Vector2(10.5f, 5.7f), new Vector2(3.6f, 5.1f))));
                if (puff <= 0f) continue;
                float edge = Mathf.Clamp(puff * 4f, 0f, 1f);
                float core = Mathf.Clamp(puff * 1.45f, 0f, 1f);
                float heat = Mathf.Clamp(y / 28f * 0.62f + core * 0.50f, 0f, 1f);
                flame.SetPixel(x, y, new Color(
                    1f, 0.24f + 0.71f * heat, 0.025f + 0.17f * heat,
                    edge * (0.55f + core * 0.40f)));
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
        _burnNoise = GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres");
    }

    // Soft elliptical lobes make a flame look slightly fuller without a second particle system.
    // =========================================================
    private static float Puff(Vector2 pixel, Vector2 center, Vector2 radii)
    {
        Vector2 n = (pixel - center) / radii;
        return Mathf.Max(0f, 1f - n.Length());
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
    // Update only an active receiver; noise erosion lives entirely on the existing sprite GPU shader.
    // =========================================================
    public override void _Process(double delta)
    {
        if (_ended) return;
        _time += (float)delta;
        RefreshWind();

        if (_burning && Tree.State == BurnState.Burning)
        {
            float full = Mathf.Max(0.1f, Tree.Profile.TransitionSeconds);
            float visual = Mathf.Clamp(
                (Tree.BurnProgress * (Tree.Profile.IgnitionDelaySeconds + full) - Tree.Profile.IgnitionDelaySeconds) / full, 0f, 1f);
            if (_erosion.IsActive && !_erosion.Update(visual, Wind.Strength, Config.CharTint))
            {
                _erosion.Restore();
                _appearance?.SetFoliageErosion(false); // Fall back to FIRE1 crossfade.
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
        float wind = Wind.Strength;
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
    // The appearance adapter already performed the final art swap and shadow refresh.
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

    // Removing the owning fire runtime cancels any FX immediately without resetting tree state.
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
        if (Tree != null && GodotObject.IsInstanceValid(Tree) && Tree.State == BurnState.Burning)
            _appearance?.SetFoliageErosion(false);
        Finished?.Invoke(this);
        QueueFree();
    }

    // Subscriptions and material ownership must not survive a removed effect.
    // =========================================================
    public override void _ExitTree()
    {
        _erosion.Restore();
        if (!_ended) { _ended = true; Finished?.Invoke(this); }
        if (GodotObject.IsInstanceValid(Tree))
        {
            Tree.BurnCompleted -= OnBurnCompleted;
            Tree.BurnReset -= OnBurnReset;
        }
    }
    #endregion
}
