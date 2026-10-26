// One world-owned severity transition; lazy B1 GPU effects share normal snow texture and global wind.
using Godot;
using System;

public partial class WorldBlizzard : Node
{
    public SnowProfile Snow { get; set; }
    public WorldSnow SnowSystem { get; set; }
    public WorldWeather Weather { get; set; }
    public SnowfallVisualLayers Visuals { get; private set; }
    public BlizzardProfile Config { get; set; }
    public float Intensity { get; private set; }
    public float DesiredIntensity => Mathf.Clamp((Snow.SnowfallRate - 1f) / 2f, 0f, 1f);
    public float TargetIntensity { get; private set; }
    public event Action<float> IntensityChanged;
    public int Notifications { get; private set; }

    // Clone tuning so F9 never edits the authored resource; snow remains the existing runtime profile.
    // =========================================================
    public override void _Ready()
    {
        if (Snow == null) throw new InvalidOperationException("Blizzard requires the world's runtime snow profile.");
        Config = (BlizzardProfile)(Config ?? GD.Load<BlizzardProfile>("res://ENVIRONMENT/Weather/Snow/BlizzardProfile.tres")).Duplicate(true);
        Snow.Changed += Refresh; Config.Changed += Refresh;
        SetProcess(false); Refresh();
    }

    // Existing snow/falling masters gate every future storm consumer, retaining the requested slider value.
    // Master OFF clears immediately; ordinary rate reductions blend in real time.
    // =========================================================
    private void Refresh()
    {
        bool allowed = Snow.Enabled && Snow.Snowfall && Snow.SnowfallRate > 0f;
        TargetIntensity = allowed ? DesiredIntensity : 0f;
        if (!allowed || Config.TransitionSeconds == 0f) SetIntensity(TargetIntensity);
        RefreshVisuals(); SetProcess(Intensity != TargetIntensity);
    }

    // Only this transition callback runs, then sleeps at both zero and nonzero endpoints.
    // =========================================================
    public override void _Process(double delta)
    {
        SetIntensity(Mathf.MoveToward(Intensity, TargetIntensity, (float)delta / Mathf.Max(0.001f, Config.TransitionSeconds)));
        SetProcess(Intensity != TargetIntensity);
    }

    // Consumers receive changed values only, with no repeated shader writes at steady severity.
    // =========================================================
    private void SetIntensity(float value)
    {
        if (Intensity == value) return;
        Intensity = value; RefreshVisuals(); Notifications++; IntensityChanged?.Invoke(value);
    }

    // Optional effects exist only during positive storm severity; ordinary snowfall keeps its owner.
    // =========================================================
    private void RefreshVisuals()
    {
        bool active = Intensity > 0f && Snow.Enabled && Snow.Snowfall && Snow.SnowfallRate > 0f
            && Weather != null && SnowSystem?.FallingTexture != null
            && ((Config.LayeredSnow && Config.ExtraFlakes > 0) || (Config.Spindrift && Config.DriftParticles > 0));
        if (!active)
        {
            if (Visuals != null) { Visuals.Free(); Visuals = null; }
            return;
        }
        if (Visuals == null)
        {
            Visuals = new SnowfallVisualLayers { Name = "StormLayers", Storm = this, Weather = Weather, Flakes = SnowSystem.FallingTexture };
            AddChild(Visuals);
        }
        Visuals.ApplySettings();
    }

    // Remove subscriptions and publish shutdown before releasing future consumer references.
    // =========================================================
    public override void _ExitTree()
    {
        Snow.Changed -= Refresh; Config.Changed -= Refresh;
        TargetIntensity = 0f; SetIntensity(0f); SetProcess(false); IntensityChanged = null;
    }
}
