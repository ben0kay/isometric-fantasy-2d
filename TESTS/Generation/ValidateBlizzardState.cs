// B0 lifecycle/state regression, independent of GPU rendering or test-scene-only world wiring.
using Godot;
using System;

public partial class ValidateBlizzardState : Node
{
    private int _checks;
    // Drive real runtime setters and transitions, checking changed-only notifications and sleep endpoints.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var snow = new SnowProfile(); var authored = new BlizzardProfile();
            var storm = new WorldBlizzard { Snow = snow, Config = authored }; AddChild(storm);
            Check(storm.Config != authored && !storm.IsProcessing() && storm.Intensity == 0, "default off/private tuning");
            int snowChanges = 0; snow.Changed += () => snowChanges++;
            snow.SnowfallRate = 0.65f; Check(snowChanges == 0, "unchanged setter");
            snow.Enabled = true; snow.SnowfallRate = 1f;
            Check(storm.TargetIntensity == 0 && !storm.IsProcessing(), "ordinary range unchanged");
            snow.SnowfallRate = 2f;
            Check(storm.TargetIntensity == 0.5f && storm.IsProcessing(), "extended range maps severity");
            storm._Process(0.75); Check(Mathf.IsEqualApprox(storm.Intensity, 0.25f), "real-time transition");
            storm._Process(3); Check(storm.Intensity == 0.5f && !storm.IsProcessing(), "nonzero endpoint sleeps");
            int notifications = storm.Notifications; snow.Coverage = 0.8f;
            Check(storm.Notifications == notifications && !storm.IsProcessing(), "coverage independent/no redundant events");
            snow.SnowfallRate = 3f; storm._Process(3);
            Check(storm.Intensity == 1 && !storm.IsProcessing(), "maximum endpoint sleeps");
            snow.Snowfall = false;
            Check(storm.Intensity == 0 && !storm.IsProcessing() && storm.DesiredIntensity == 1, "falling master gates/preserves request");
            Check(snow.Coverage == 0.8f, "coverage retained");
            snow.Snowfall = true; Check(storm.IsProcessing(), "reenable wakes"); storm._Process(3);
            snow.Enabled = false; Check(storm.Intensity == 0 && !storm.IsProcessing(), "snow master immediately clears");
            snow.Enabled = true; storm._Process(3); snow.SnowfallRate = 1;
            Check(storm.IsProcessing(), "ordinary range begins fade"); storm._Process(3);
            Check(storm.Intensity == 0 && !storm.IsProcessing(), "zero endpoint sleeps");
            storm.Config.TransitionSeconds = 0; snow.SnowfallRate = 3;
            Check(storm.Intensity == 1 && !storm.IsProcessing() && authored.TransitionSeconds == 3, "instant transition/private profile");
            snow.SnowfallRate = 0; Check(storm.Intensity == 0 && !storm.IsProcessing(), "zero rate shuts down");
            snow.SnowfallRate = 30; Check(snow.SnowfallRate == 3, "upper clamp");
            snow.SnowfallRate = float.NaN; Check(snow.SnowfallRate == 0 && storm.Intensity == 0, "invalid input shuts down");
            int shutdowns = 0; storm.IntensityChanged += value => { if (value == 0) shutdowns++; };
            snow.SnowfallRate = 3; storm.Free(); Check(shutdowns == 1, "teardown clears consumers");
            snow.SnowfallRate = 2; authored.TransitionSeconds = 5; // No freed-node subscriptions may run.
            GD.Print($"B0 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // Keep failures associated with the lifecycle requirement being tested.
    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
