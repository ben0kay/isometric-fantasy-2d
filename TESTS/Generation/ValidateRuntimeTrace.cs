// Validate tracing against controlled nested work, actual production callbacks and removable UI ownership.
using Godot;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
public partial class ValidateRuntimeTrace : Node
{
    public override async void _Ready()
    {
        try { await Run(); GD.Print("RUNTIME TRACE PASS"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // A deliberate 40ms nested scope must be attributed once, then remain in the retained slow frame.
    // =========================================================
    private async Task Run()
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
        AddChild(world); await Frames(220);
        var panel = world.GetNode("DebugUIRoot/EnvironmentDebugPanel/WorldDiagnostics");
        var toggle = panel.FindChildren("*","CheckButton",true,false).OfType<CheckButton>().Single(c => c.Text == "Trace C# callbacks");
        if (RuntimeFrameTrace.Enabled) throw new Exception("Tracing enabled without opting in");
        toggle.ButtonPressed = true; await Frames(4);
        using (RuntimeFrameTrace.Measure("Controlled parent"))
        {
            Thread.Sleep(10);
            using (RuntimeFrameTrace.Measure("Controlled child")) Thread.Sleep(30);
        }
        await Frames(4);
        string report = RuntimeFrameTrace.Report();
        double Read(string name) => double.Parse(Regex.Match(report,Regex.Escape(name)+@": ([\d.]+)ms").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
        if (Read("Controlled child") < 25 || Read("Controlled parent") >= Read("Controlled child"))
            throw new Exception("Nested work was not attributed exclusively: "+report);
        if (!report.Contains("GC 0/1/2") || !report.Contains("Worst slow frame")) throw new Exception("Missing slow-frame indicators");
        await Frames(20);
        if (!RuntimeFrameTrace.Report().Contains("Controlled child")) throw new Exception("Slow frame was overwritten by ordinary frames");
        int before = RuntimeFrameTrace.CapturedFrames;
        ((Control)panel).Visible = false; await Frames(8);
        if (RuntimeFrameTrace.CapturedFrames <= before) throw new Exception("Hiding diagnostics stopped capture");
        GD.Print(report);
        toggle.ButtonPressed = false; before = RuntimeFrameTrace.CapturedFrames; await Frames(5);
        if (RuntimeFrameTrace.CapturedFrames != before) throw new Exception("Disabled tracing still records");
        RuntimeFrameTrace.Reset();
        if (RuntimeFrameTrace.CapturedFrames != 0) throw new Exception("Reset retained old frame counters");
        toggle.ButtonPressed = true; await Frames(3);
        world.QueueFree(); await Frames(3);
        if (RuntimeFrameTrace.Enabled) throw new Exception("Removed diagnostic panel left tracing active");
    }
    private async Task Frames(int count) { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
}
