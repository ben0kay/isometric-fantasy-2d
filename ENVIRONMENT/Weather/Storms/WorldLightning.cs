// Rare visual-only lightning: broad nearby illumination or a visible branched ground strike.
using Godot;
using System.Collections.Generic;

public partial class WorldLightning : Node2D
{
    public WorldWeather Weather { get; set; }
    public float Flash { get; private set; }
    private readonly RandomNumberGenerator _random = new();
    private readonly List<Vector2[]> _branches = new();
    private PointLight2D _impactLight;
    private Vector2[] _bolt = System.Array.Empty<Vector2>();
    private Vector2 _impact;
    private float _age = 1f, _strength, _remaining;
    private bool _visibleStrike, _storm, _processing=true;
    private float _lastFlash=float.NaN;
    private Color _lastTint;

    #region Setup and triggering
    // Create one reusable impact light; bolt geometry is generated only when a strike occurs.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = -9;
        _random.Randomize();
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        var gradient = new Gradient
        {
            Offsets = new float[] { 0f, 0.3f, 1f },
            Colors = new Color[] { Colors.White, new(1, 1, 1, 0.55f), new(1, 1, 1, 0) }
        };
        _impactLight = new PointLight2D
        {
            Name = "ImpactLight", Enabled = false, Color = Weather.Config.LightningTint,
            Texture = new GradientTexture2D
            {
                Gradient = gradient, Width = 256, Height = 256,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f)
            },
            TextureScale = 1.5f
        };
        AddChild(_impactLight);
        ScheduleNext();
    }

    // Manual previews bypass the rain threshold; the master toggle and brief cooldown still apply.
    // =========================================================
    public void Trigger(bool visibleStrike)
    {
        if (!Weather.Config.Lightning || _age < 0.8f) return;
        _visibleStrike = visibleStrike;
        _branches.Clear();
        _bolt = System.Array.Empty<Vector2>();
        if (visibleStrike)
        {
            Rect2 view = Weather.VisibleWorldRect();
            Rect2 target = new(view.Position + view.Size * new Vector2(0.25f, 0.45f), view.Size * new Vector2(0.5f, 0.35f));
            // If the view is completely covered with props, fall back to a nearby flash.
            _visibleStrike = Weather.GroundQueries.TryGroundPoint(target, _random, out _impact);
            if (_visibleStrike) BuildBolt(view);
        }
        _strength = Mathf.Clamp(_visibleStrike ? Weather.Config.StrikeFlashStrength : Weather.Config.NearbyFlashStrength, 0f, 1f);
        _age = 0f; _processing=true; SetProcess(true);
        UpdateFlash();
        ScheduleNext();
    }

    // No storm clock or redraw loop while dry/idle; manual flashes wake themselves.
    // =========================================================
    public void RefreshProcessing()
    {
        if (!Weather.Config.Lightning) { _age = 1f; _storm = false; UpdateFlash(); }
        bool active = Weather.Config.Lightning && (Weather.RainActive || _age < 0.8f);
        if (active != _processing) { _processing=active; SetProcess(active); }
    }
    // Keep automatic nearby flashes irregular; visible strikes default to only four percent of events.
    // =========================================================
    private void ScheduleNext()
    {
        float low = Mathf.Max(2f, Mathf.Min(Weather.Config.LightningIntervalMin, Weather.Config.LightningIntervalMax));
        float high = Mathf.Max(low, Mathf.Max(Weather.Config.LightningIntervalMin, Weather.Config.LightningIntervalMax));
        _remaining = _random.RandfRange(low, high);
    }

    // Use stable, world-anchored geometry for the entire event rather than jittering every frame.
    // =========================================================
    private void BuildBolt(Rect2 view)
    {
        Vector2 start = new(_impact.X + _random.RandfRange(-70f, 70f), view.Position.Y - 30f);
        _bolt = new Vector2[15];
        for (int i = 0; i < _bolt.Length; i++)
        {
            float t = i / (float)(_bolt.Length - 1);
            _bolt[i] = start.Lerp(_impact, t);
            if (i > 0 && i < _bolt.Length - 1) _bolt[i].X += _random.RandfRange(-16f, 16f);
        }
        for (int branch = 0; branch < 3; branch++)
        {
            Vector2 origin = _bolt[4 + branch * 3];
            float side = branch % 2 == 0 ? -1f : 1f;
            var points = new Vector2[5];
            for (int i = 0; i < points.Length; i++)
                points[i] = origin + new Vector2(side * i * 13f + (i == 0 ? 0 : _random.RandfRange(-8, 8)), i * 15f);
            _branches.Add(points);
        }
        _impactLight.GlobalPosition = _impact;
    }
    #endregion

    #region Simulation and presentation
    // Storm timing uses real elapsed seconds, not the prototype's accelerated day clock.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLightning.Process");
        _age += (float)delta;
        if (!Weather.Config.Lightning)
        {
            _age = 1f; _storm = false;
            UpdateFlash();
            return;
        }
        bool storm = Weather.Rain >= Mathf.Max(0.01f, Weather.Config.LightningRainThreshold);
        if (storm && !_storm) ScheduleNext();
        _storm = storm;
        if (storm)
        {
            _remaining -= (float)delta;
            if (_remaining <= 0f) Trigger(_random.Randf() < Mathf.Clamp(Weather.Config.VisibleStrikeChance, 0f, 1f));
        }
        UpdateFlash(); RefreshProcessing();
    }

    // One sharp pulse, a weaker return stroke, then a soft tail; always returns exactly to zero.
    // =========================================================
    private void UpdateFlash()
    {
        float pulse = 0f;
        if (Weather.Config.Lightning && _age < 0.65f)
        {
            pulse = Mathf.Exp(-_age * 19f);
            if (_age >= 0.17f) pulse = Mathf.Max(pulse, 0.48f * Mathf.Exp(-(_age - 0.17f) * 13f));
            pulse *= Mathf.Clamp((0.65f - _age) / 0.15f, 0f, 1f);
        }
        Flash = pulse * _strength;
        if (Flash == _lastFlash && Weather.Config.LightningTint == _lastTint) return;
        _lastFlash = Flash; _lastTint=Weather.Config.LightningTint;
        _impactLight.Enabled = _visibleStrike && Flash > 0.002f;
        _impactLight.Color = Weather.Config.LightningTint;
        _impactLight.Energy = Flash * 1.7f;
        QueueRedraw();
    }

    // Layered translucent strokes approximate a glow without adding fullscreen bloom.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLightning.Draw");
        if (!_visibleStrike || Flash <= 0.002f || _bolt.Length < 2) return;
        Color glow = new(0.45f, 0.65f, 1f, Flash * 0.18f);
        Color core = new(0.94f, 0.98f, 1f, Mathf.Min(1f, Flash * 1.8f));
        DrawPolyline(_bolt, glow, 10f, true);
        DrawPolyline(_bolt, new Color(0.65f, 0.80f, 1f, Flash * 0.5f), 4f, true);
        DrawPolyline(_bolt, core, 1.3f, true);
        foreach (Vector2[] branch in _branches) DrawPolyline(branch, new Color(core, core.A * 0.65f), 0.75f, true);
        DrawSetTransform(_impact, 0f, new Vector2(1f, 0.4f));
        DrawCircle(Vector2.Zero, 3f + _age * 12f, new Color(0.65f, 0.80f, 1f, Flash * 0.3f));
        DrawSetTransform(Vector2.Zero);
    }
    #endregion
}
