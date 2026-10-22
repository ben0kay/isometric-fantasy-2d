// Production smoothed weather signal; a single bounded helper owns layered GPU rain and overcast.
using Godot;

public partial class WorldWeather : Node2D
{
    [Export] public WeatherProfile Config { get; set; }
    public WindController Wind { get; set; }
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    public SceneryArtworkIndex GroundQueries { get; set; } = new();
    public bool RainActive => Config.RainIntensity > 0f;
    public event System.Action RainStateChanged;
    public float WindStrength => Wind.Strength;
    public float Rain { get; private set; }
    public float CloudCover => Config.Overcast ? Rain : 0f;
    public WorldLightning Lightning { get; private set; }
    private RainVisualLayers _rainLayers;
    private GroundRainSplashes _splashes;
    #region Setup
    // Weather runs before its consumers, independently of the accelerated day/night clock.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WeatherProfile();
        ProcessPriority = -10;
        GroundQueries.Initialize(Actors,WorldBounds);
        _splashes = new GroundRainSplashes { Name = "GroundSplashes", ZIndex = -4, Weather = this, Actors = Actors, WorldBounds = WorldBounds };
        AddChild(_splashes);
        Lightning = new WorldLightning { Name = "Lightning", Weather = this, ZIndex = 7 };
        AddChild(Lightning);
        Config.Changed += ApplyRainState; ApplyRainState();
    }

    // Resource changes wake a sleeping controller; future scheduling can set the same intensity property.
    // =========================================================
    private void ApplyRainState()
    {
        if (RainActive)
        {
            if (_rainLayers == null)
            { _rainLayers = new RainVisualLayers { Name="RainLayers", Config=Config }; AddChild(_rainLayers); }
            SetProcess(true);
        }
        else
        {
            Rain = 0; SetProcess(false);
            if (_rainLayers != null) { _rainLayers.Free(); _rainLayers = null; }
        }
        _splashes.SetRainActive(RainActive);
        Lightning.RefreshProcessing(); RainStateChanged?.Invoke();
    }
    public override void _ExitTree() { Config.Changed -= ApplyRainState; GroundQueries.Clear(); }
    #endregion

    #region Simulation
    // Integrating displacement avoids fog/cloud jumps when the wind slider changes.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldWeather.Process");
        Rain = Mathf.MoveToward(Rain, Mathf.Clamp(Config.RainIntensity, 0f, 1f), (float)delta / Mathf.Max(0.1f, Config.TransitionSeconds));
        _rainLayers.UpdateRain(Rain, WindStrength, VisibleWorldRect());
    }

    // The canvas transform includes camera zoom and clamping, unlike the player's position.
    // =========================================================
    public Rect2 VisibleWorldRect()
    {
        Transform2D inverse = GetViewport().GetCanvasTransform().AffineInverse();
        Vector2 start = inverse * Vector2.Zero;
        Vector2 end = inverse * GetViewportRect().Size;
        return new Rect2(start, end - start).Abs();
    }
    #endregion
}
