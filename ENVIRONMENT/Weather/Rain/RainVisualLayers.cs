// One bounded, camera-centred GPU rain system with distant, main and foreground layers.
// Total MaxDrops is partitioned, not repeated; all emitters use one wind and intensity.
using Godot;
using System.Collections.Generic;

public partial class RainVisualLayers : Node2D
{
    public WeatherProfile Config { get; set; }
    private readonly List<Layer> _layers = new();
    private float _lastRain=float.NaN, _lastWind, _lastForeground, _lastSpeed, _lastSlant, _lastVariation;
    private Rect2 _lastView;
    public int AppliedUpdates { get; private set; }
    private sealed class Layer
    {
        public GpuParticles2D Emitter;
        public ParticleProcessMaterial Motion;
        public float Speed;
        public bool Foreground, Initialized;
    }

    #region Initialization
    // Three small soft streaks share one texture; per-particle scale, alpha and speed vary.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WeatherProfile();
        Texture2D art = MakeStreak(Mathf.Clamp(Config.DropSoftness, 0f, 1f));
        Texture2D opacity = MakeOpacityRamp(Mathf.Clamp(Config.DropVariation, 0f, 1f));
        int total = Mathf.Clamp(Config.MaxDrops, 100, 4000);
        if (Config.LayeredRain)
        {
            int far = Mathf.RoundToInt(total * 0.30f), front = Mathf.RoundToInt(total * 0.19f);
            AddLayer("DistantRain", far, 0.68f, 0.55f, 0.78f, 0.14f, -1, art, opacity);
            AddLayer("MainRain", total - far - front, 1f, 0.85f, 1.20f, 0.27f, 6, art, opacity);
            AddLayer("ForegroundRain", front, 1.24f, 1.12f, 1.68f, 0.23f, 8, art, opacity, true);
        }
        else AddLayer("Rain", total, 1f, 0.77f, 1.30f, 0.30f, 6, art, opacity);
    }

    // Feather the horizontal edges and both ends, instead of a hard 1-pixel white line.
    // =========================================================
    private static Texture2D MakeStreak(float softness)
    {
        const int width = 5, height = 22;
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        for (int y = 0; y < height; y++)
        {
            float lengthAlpha = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.Pi * (y + 0.5f) / height)),
                Mathf.Lerp(1.5f, 0.80f, softness));
            for (int x = 0; x < width; x++)
            {
                float edge = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
                float widthAlpha = Mathf.Pow(Mathf.Max(0f, 1f - edge),
                    Mathf.Lerp(3f, 1.05f, softness));
                image.SetPixel(x, y, new Color(0.88f, 0.94f, 1f,
                    widthAlpha * lengthAlpha * 0.82f));
            }
        }
        Texture2D result = ImageTexture.CreateFromImage(image);
        image.Dispose();
        return result;
    }

    // Random starting alpha per GPU particle prevents every droplet looking equally bright.
    // =========================================================
    private static Texture2D MakeOpacityRamp(float variation)
    {
        var gradient = new Gradient
        {
            Offsets = new float[] { 0f, 1f },
            Colors = new Color[] {
                new(1f, 1f, 1f, Mathf.Lerp(1f, 0.48f, variation)),
                new(1f, 1f, 1f, 1f)
            }
        };
        return new GradientTexture1D { Width = 64, Gradient = gradient };
    }

    // One GPU emitter per depth, with a shared shader and controlled random variation.
    // =========================================================
    private void AddLayer(string name, int count, float speed, float minScale, float maxScale,
        float opacity, int z, Texture2D art, Texture2D alphaRamp, bool foreground = false)
    {
        float variation = Mathf.Clamp(Config.DropVariation, 0f, 1f);
        var process = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Gravity = Vector3.Zero, Direction = Vector3.Down,
            Spread = 1.5f + 7f * variation,
            InitialVelocityMin = 100f, InitialVelocityMax = 100f,
            ScaleMin = minScale * (1f - variation * 0.35f),
            ScaleMax = maxScale * (1f + variation * 0.40f),
            Color = new Color(0.70f, 0.80f, 0.88f, opacity),
            ColorInitialRamp = alphaRamp
        };
        process.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        process.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.AlignYToVelocity, true);
        var emitter = new GpuParticles2D
        {
            Name = name, Amount = Mathf.Max(1, count), Lifetime = 0.65f,
            Preprocess = 0.65f, LocalCoords = false, Emitting = false,
            ZIndex = z, Texture = art, ProcessMaterial = process,
            TextureFilter = TextureFilterEnum.Linear,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        AddChild(emitter);
        _layers.Add(new Layer { Emitter = emitter, Motion = process, Speed = speed, Foreground = foreground });
    }
    #endregion

    #region Active camera rendering
    // Update only three GPU emitters and their shared wind velocity, never each raindrop.
    // =========================================================
    public void UpdateRain(float rain, float windStrength, Rect2 view)
    {
        float intensity = Mathf.Clamp(rain, 0f, 1f);
        bool intensityChanged = intensity != _lastRain || Config.ForegroundRainStrength != _lastForeground;
        bool motionChanged = windStrength != _lastWind || Config.FallSpeed != _lastSpeed || Config.SlantPerStrength != _lastSlant || Config.DropVariation != _lastVariation;
        bool viewChanged = view != _lastView;
        if (!intensityChanged && !motionChanged && !viewChanged) return;
        _lastRain=intensity; _lastForeground=Config.ForegroundRainStrength; _lastWind=windStrength;
        _lastSpeed=Config.FallSpeed; _lastSlant=Config.SlantPerStrength; _lastVariation=Config.DropVariation; _lastView=view;
        AppliedUpdates++;
        if (intensity <= 0.001f)
        {
            foreach (Layer layer in _layers) layer.Emitter.Emitting = false;
            return;
        }
        float lateral = Config.SlantPerStrength * Mathf.Clamp(windStrength, -3f, 3f);
        Vector2 windFall = new(lateral, Mathf.Max(100f, Config.FallSpeed));
        float variation = Mathf.Clamp(Config.DropVariation, 0f, 1f);
        foreach (Layer layer in _layers)
        {
            GpuParticles2D emitter = layer.Emitter;
            float contribution = layer.Foreground ? Mathf.Clamp(Config.ForegroundRainStrength, 0f, 1f) : 1f;
            float ratio = intensity * contribution;
            bool emitting = ratio > 0.001f;
            if (intensityChanged)
            { emitter.AmountRatio = Mathf.Max(0.001f,ratio); emitter.Emitting = emitting; }
            if (!emitting) continue;

            float maxSpeed = windFall.Length() * layer.Speed * (1f + variation * 0.19f);
            float margin = 160f + maxSpeed * (float)emitter.Lifetime;
            Vector2 half = view.Size / 2f + Vector2.One * margin;
            if (viewChanged || motionChanged || !layer.Initialized)
            {
                emitter.GlobalPosition = view.GetCenter();
                emitter.VisibilityRect = new Rect2(-half*2f,half*4f);
                layer.Motion.EmissionBoxExtents = new Vector3(half.X,half.Y,1f);
            }
            if (motionChanged || !layer.Initialized)
            {
                layer.Motion.Direction = new Vector3(windFall.X,windFall.Y,0f).Normalized();
                layer.Motion.InitialVelocityMin = maxSpeed*(1f-variation*0.30f);
                layer.Motion.InitialVelocityMax = maxSpeed;
            }
            layer.Initialized = true;
        }
    }
    #endregion
}
