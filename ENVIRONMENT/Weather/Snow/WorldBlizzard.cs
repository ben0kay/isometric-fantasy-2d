// One world-owned event-driven severity transition. B1/B2 consume changes; B0 allocates no effects.
using Godot;
using System;

public partial class WorldBlizzard : Node
{
    public SnowProfile Snow { get; set; }
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
        SetProcess(Intensity != TargetIntensity);
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
        Intensity = value; Notifications++; IntensityChanged?.Invoke(value);
    }

    // Remove subscriptions and publish shutdown before releasing future consumer references.
    // =========================================================
    public override void _ExitTree()
    {
        Snow.Changed -= Refresh; Config.Changed -= Refresh;
        TargetIntensity = 0f; SetIntensity(0f); SetProcess(false); IntensityChanged = null;
    }
}
