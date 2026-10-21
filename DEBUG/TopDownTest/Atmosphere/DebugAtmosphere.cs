// One bounded 2D renderer handles haze, pollen, leaves and torch emissions.
using Godot;
using System.Collections.Generic;

public partial class DebugAtmosphere : Node2D
{
    [Export] public AtmosphereMasterConfig Config { get; set; }
    [Export] public Texture2D SmokeTexture { get; set; }
    private DebugDayNight _day;
    private DebugWeather _weather;
    private Node2D _torch;
    private Polygon2D _haze;
    private ShaderMaterial _mistMaterial;
    private readonly List<Vector2> _trees = new();
    private static readonly Vector2[] LeafShape = { new(-3, 0), new(0, -1.5f), new(3, 0), new(0, 1.5f) };
    private double _time;
    private Vector2 _drift;
    private Rect2 _worldBounds;
    private readonly Vector2[] _leafDrift = new Vector2[40], _smokeDrift = new Vector2[40], _emberDrift = new Vector2[40];
    private readonly float[] _leafAge = new float[40], _smokeAge = new float[40], _emberAge = new float[40];

    #region Setup
    // Cache emitters once and add controls beside the existing polish switches.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AtmosphereMasterConfig();
        _day = GetNode<DebugDayNight>("../DayNight");
        _weather = GetNode<DebugWeather>("../Weather");
        _torch = GetNode<Node2D>("../Actors/Torch");
        _haze = GetNode<Polygon2D>("Haze");
        _mistMaterial = (ShaderMaterial)_haze.Material;
        // Scenery is generated in Actors._Ready before this atmosphere node is ready.
        _worldBounds = GetNode<DebugWorldScatter>("../Actors").Config.WorldBounds;
        _mistMaterial.SetShaderParameter("world_min", _worldBounds.Position);
        _mistMaterial.SetShaderParameter("world_max", _worldBounds.End);
        foreach (Node child in GetNode<Node2D>("../Actors").GetChildren())
            if (child is Node2D tree && child.Name.ToString().StartsWith("Tree")) _trees.Add(tree.Position);
        VBoxContainer controls = GetNode<VBoxContainer>("UI/Panel/Controls");
        AddSwitch(controls, "Ground haze", Config.Haze, value => Config.Haze = value);
        AddSwitch(controls, "Pollen / dust", Config.Pollen, value => Config.Pollen = value);
        AddSwitch(controls, "Falling leaves", Config.Leaves, value => Config.Leaves = value);
        AddSwitch(controls, "Torch smoke", Config.Smoke, value => Config.Smoke = value);
        AddSwitch(controls, "Torch embers", Config.Embers, value => Config.Embers = value);
    }

    // Independent switches make it easy to compare atmosphere against the baseline.
    // =========================================================
    private static void AddSwitch(VBoxContainer parent, string text, bool value, System.Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = value };
        toggle.Toggled += enabled => changed(enabled);
        parent.AddChild(toggle);
    }
    #endregion

    #region Simulation and drawing
    // Integrate drift so changing wind does not teleport the mist.
    // =========================================================
    public override void _Process(double delta)
    {
        _time += delta;
        _drift = _weather.Displacement;
        IntegrateParticleWind(_leafDrift, _leafAge, 14f, 5.37f, 0.5f, (float)delta);
        IntegrateParticleWind(_smokeDrift, _smokeAge, 6f, 0.43f, 0.7f, (float)delta);
        IntegrateParticleWind(_emberDrift, _emberAge, 2.2f, 0.31f, 0.3f, (float)delta);
        _haze.Visible = Config.Haze && Config.HazeDensity > 0;
        _mistMaterial.SetShaderParameter("density", Config.HazeDensity);
        _mistMaterial.SetShaderParameter("patch_size", Config.PatchSize);
        _mistMaterial.SetShaderParameter("haze_colour", Config.HazeColour);
        _mistMaterial.SetShaderParameter("daylight", _day.Daylight);
        _mistMaterial.SetShaderParameter("drift", -_drift);
        QueueRedraw();
    }

    // Integrate per-cycle drift so wind changes move existing particles without position jumps.
    // =========================================================
    private void IntegrateParticleWind(Vector2[] drift, float[] previousAge, float period, float phase, float response, float delta)
    {
        for (int i = 0; i < drift.Length; i++)
        {
            float age = Mathf.PosMod((float)(_time % 100000) + i * phase, period);
            drift[i] = age < previousAge[i] ? Vector2.Zero : drift[i] + _weather.Velocity * delta * response;
            previousAge[i] = age;
        }
    }

    // Fixed-count deterministic trajectories avoid spawning nodes or allocating per particle.
    // =========================================================
    public override void _Draw()
    {
        if (_day == null) return;
        if (Config.Pollen)
            for (int i = 0; i < Mathf.Clamp(Config.PollenCount, 0, 100); i++) DrawPollen(i);
        if (Config.Leaves && _trees.Count > 0)
            for (int i = 0; i < Mathf.Clamp(Config.LeafCount, 0, 40); i++) DrawLeaf(i);
        if (_torch.IsVisibleInTree())
        {
            if (Config.Smoke && SmokeTexture != null)
                for (int i = 0; i < Mathf.Clamp(Config.SmokeCount, 0, 40); i++) DrawSmoke(i);
            if (Config.Embers)
                for (int i = 0; i < Mathf.Clamp(Config.EmberCount, 0, 40); i++) DrawEmber(i);
        }
    }

    // Slow dust drifts across the clearing, becoming much less visible at night.
    // =========================================================
    private void DrawPollen(int i)
    {
        float phase = i * 2.39996f;
        Vector2 p = new(
            _worldBounds.Position.X + Mathf.PosMod(i * 137.4f + _drift.X + Mathf.Sin((float)_time * 0.4f + phase) * 6f, _worldBounds.Size.X),
            _worldBounds.Position.Y + Mathf.PosMod(i * 83.7f + _drift.Y + Mathf.Sin((float)_time * 0.3f + phase) * 4f, _worldBounds.Size.Y));
        float alpha = (0.10f + 0.35f * _day.Daylight) * (0.6f + 0.4f * Mathf.Sin((float)_time + phase));
        DrawCircle(p, i % 4 == 0 ? 0.8f : 0.5f, new Color(0.82f, 0.79f, 0.57f, alpha));
    }

    // Leaves start under tree crowns and tumble down; they fade before looping.
    // =========================================================
    private void DrawLeaf(int i)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 5.37f, 14f);
        if (age > 9f) return;
        Vector2 start = _trees[(i * 17) % _trees.Count] + new Vector2(Mathf.Sin(i * 7.1f) * 28f, -100f);
        Vector2 p = start + new Vector2(_leafDrift[i].X + Mathf.Sin(age * 2f + i) * 7f, age * 11f);
        float alpha = Mathf.Min(age, 1f) * Mathf.Clamp((9f - age) / 2f, 0f, 1f);
        alpha *= 0.25f + 0.65f * _day.Daylight;
        DrawSetTransform(p, age * 2f + i, new Vector2(1f, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(age * 3f + i))));
        DrawColoredPolygon(LeafShape, new Color(0.63f, 0.48f, 0.25f, alpha));
        DrawSetTransform(Vector2.Zero);
    }

    // A small radial texture produces soft expanding smoke without external image assets.
    // =========================================================
    private void DrawSmoke(int i)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 0.43f, 6f);
        Vector2 p = _torch.Position + new Vector2(0, -43) +
            new Vector2(_smokeDrift[i].X + Mathf.Sin(age + i) * age, -age * 12f);
        float size = 7f + age * 4f;
        float alpha = Mathf.Min(age * 2f, 1f) * (1f - age / 6f) * 0.13f;
        alpha *= 0.4f + 0.6f * _day.Daylight;
        DrawTextureRect(SmokeTexture, new Rect2(p - Vector2.One * size / 2f, Vector2.One * size), false,
            new Color(0.65f, 0.66f, 0.64f, alpha));
    }

    // Tiny warm sparks lift from the flame and fade within two seconds.
    // =========================================================
    private void DrawEmber(int i)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 0.31f, 2.2f);
        Vector2 p = _torch.Position + new Vector2(0, -42) +
            new Vector2(Mathf.Sin(age * 3f + i) * 3f + _emberDrift[i].X, -age * (14f + i % 4));
        float alpha = Mathf.Min(age * 5f, 1f) * (1f - age / 2.2f) * 0.75f;
        DrawCircle(p, 0.6f, new Color(1f, 0.48f + age * 0.1f, 0.10f, alpha));
    }
    #endregion
}
