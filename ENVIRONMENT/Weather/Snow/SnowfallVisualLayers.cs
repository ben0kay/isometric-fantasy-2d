// Optional camera-local GPU storm layers. Normal snowfall's 1300 + at most 1300 extra flakes.
// One active helper updates cached bounds and throttled gusts; no CPU per-particle simulation.
using Godot;
using System.Collections.Generic;

public partial class SnowfallVisualLayers : Node2D
{
    public WorldBlizzard Storm { get; set; }
    public WorldWeather Weather { get; set; }
    public Texture2D Flakes { get; set; }
    private readonly List<Layer> _layers = new();
    private Texture2D _driftTexture;
    private Rect2 _lastView;
    private float _time, _motionTimer, _lastIntensity = -1f;
    private int _flakeBudget = -1, _driftBudget = -1;
    private bool _dirty = true;
    public int AllocatedFlakes { get; private set; }
    public int AllocatedSpindrift { get; private set; }
    private sealed class Layer
    {
        public GpuParticles2D Emitter;
        public ParticleProcessMaterial Motion;
        public float Speed, Alpha;
        public bool Ground;
        public Vector2 LastVelocity = new(float.NaN,float.NaN);
    }

    // One setup per activation; the normal emitter's generated flake atlas is reused directly.
    // =========================================================
    public override void _Ready() => ApplySettings();

    // Rebuild only when an authored budget/toggle changes; transitions only change active amount/opacity.
    // =========================================================
    public void ApplySettings()
    {
        var config = Storm.Config;
        int flakes = config.LayeredSnow ? config.ExtraFlakes : 0;
        int drift = config.Spindrift ? config.DriftParticles : 0;
        if (flakes != _flakeBudget || drift != _driftBudget)
        {
            foreach (var layer in _layers) layer.Emitter.Free();
            _layers.Clear(); _flakeBudget = flakes; _driftBudget = drift;
            AllocatedFlakes = flakes; AllocatedSpindrift = drift;
            int far = flakes * 40 / 100, near = flakes * 18 / 100;
            AddLayer("FarSnow", far, 0.65f, 0.28f, 0.55f, 0.38f, -1, false);
            AddLayer("MidSnow", flakes - far - near, 1f, 0.55f, 1.05f, 0.70f, 6, false);
            AddLayer("NearSnow", near, 1.6f, 1.0f, 1.7f, 0.65f, 8, false);
            AddLayer("GroundSpindrift", drift, 0.8f, 0.65f, 1.6f, 0.20f, -4, true);
            _lastView = default; _lastIntensity = -1f;
        }
        _dirty = true; UpdateView(); UpdateMotion();
    }

    // Fixed GPU emitters share atlas/art, fade lifetime and explicit world-space travel margins.
    // =========================================================
    private void AddLayer(string name, int count, float speed, float scaleMin, float scaleMax, float alpha, int z, bool ground)
    {
        if (count == 0) return;
        var fade = new Gradient { Offsets = new[] { 0f, 0.18f, 0.78f, 1f }, Colors = new[] { new Color(1,1,1,0), Colors.White, Colors.White, new Color(1,1,1,0) } };
        var motion = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Gravity = Vector3.Zero, Spread = ground ? 9f : 24f,
            ScaleMin = scaleMin, ScaleMax = scaleMax,
            AnimOffsetMin = 0f, AnimOffsetMax = 0.999f,
            ColorRamp = new GradientTexture1D { Gradient = fade }
        };
        motion.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        if (ground) _driftTexture ??= MakeDriftTexture();
        var emitter = new GpuParticles2D
        {
            Name = name, Amount = count, Lifetime = ground ? 2f : speed > 1.5f ? 1.8f : 3f,
            Preprocess = 0, LocalCoords = false, Emitting = false, ZIndex = z,
            Texture = ground ? _driftTexture : Flakes, ProcessMaterial = motion,
            TextureFilter = ground || speed < 0.7f ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
                ParticlesAnimation = !ground, ParticlesAnimHFrames = ground ? 1 : 4, ParticlesAnimVFrames = 1 }
        };
        AddChild(emitter); _layers.Add(new Layer { Emitter = emitter, Motion = motion, Speed = speed, Alpha = alpha, Ground = ground });
    }

    // Cheap feathered wisp art is generated once per activation, with no textures reallocated per frame.
    // =========================================================
    private static Texture2D MakeDriftTexture()
    {
        var image = Image.CreateEmpty(48, 8, false, Image.Format.Rgba8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 48; x++)
        {
            float u = (x + 0.5f) / 48f, v = (y + 0.5f) / 8f;
            float alpha = Mathf.Pow(Mathf.Sin(u * Mathf.Pi), 0.7f) * Mathf.Pow(Mathf.Sin(v * Mathf.Pi), 1.5f);
            image.SetPixel(x,y,new Color(0.86f,0.92f,1f,alpha));
        }
        var texture = ImageTexture.CreateFromImage(image); image.Dispose(); return texture;
    }

    // Camera follows every active frame; continuous gust parameters update at most ten times per second.
    // =========================================================
    public override void _Process(double delta)
    {
        _time += (float)delta; _motionTimer += (float)delta;
        UpdateView();
        if (_dirty || _motionTimer >= 0.1f) { _motionTimer = 0f; UpdateMotion(); }
    }

    // Conservative maximum travel bounds also cover camera zoom/resizes and signed horizontal wind.
    // =========================================================
    private void UpdateView()
    {
        Rect2 view = Weather.VisibleWorldRect(); if (view == _lastView) return;
        _lastView = view; GlobalPosition = view.GetCenter();
        Vector2 half = view.Size * 0.5f + new Vector2(180, 120);
        foreach (var layer in _layers)
        {
            layer.Motion.EmissionBoxExtents = new Vector3(half.X,half.Y,1);
            layer.Emitter.VisibilityRect = new Rect2(-half - new Vector2(1400,500), (half + new Vector2(1400,500)) * 2f);
        }
    }

    // Gust amplitude never reverses nonzero user wind or writes back into the shared WindController.
    // Zero wind uses bounded alternating flurries rather than forcing a permanent direction.
    // =========================================================
    private void UpdateMotion()
    {
        float severity = Storm.Intensity, wind = Mathf.Clamp(Weather.WindStrength,-3f,3f);
        var config = Storm.Config;
        float phase = _time * Mathf.Tau / config.GustSeconds;
        float gust = 1f + config.GustStrength * (0.7f * Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 1.73f + 0.8f));
        float lateral = Mathf.Abs(wind) > 0.01f ? Mathf.Sign(wind) * (60f + Mathf.Abs(wind) * 54f) * gust
            : 50f * config.GustStrength * Mathf.Sin(phase * 0.63f);
        foreach (var layer in _layers)
        {
            if (_lastIntensity != severity)
            {
                layer.Emitter.AmountRatio = Mathf.Max(0.001f,severity);
                layer.Motion.Color = new Color(0.94f,0.97f,1f,layer.Alpha * severity);
            }
            var velocity = new Vector2(lateral * severity, layer.Ground ? 4f : Mathf.Lerp(45f,20f,severity)) * layer.Speed;
            if (velocity != layer.LastVelocity)
            {
                layer.LastVelocity = velocity;
                layer.Motion.Direction = new Vector3(velocity.X,velocity.Y,0).Normalized();
                layer.Motion.InitialVelocityMin = velocity.Length() * 0.55f;
                layer.Motion.InitialVelocityMax = velocity.Length();
            }
            if (!layer.Emitter.Emitting) layer.Emitter.Emitting = true;
        }
        _lastIntensity = severity; _dirty = false;
    }
}
