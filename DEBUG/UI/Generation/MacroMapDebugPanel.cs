// Removable F7 host; the same metadata-only viewer is used by DEBUG/PreviewMap.
using Godot;
public partial class MacroMapDebugPanel : CanvasLayer
{
    private PanelContainer _panel;
    // Bind after production startup without generating hidden raster textures.
    // =========================================================
    public override void _Ready() => CallDeferred(nameof(BuildPanel));
    // Keep the normal World composition dependency in this host, outside the reusable viewer.
    // =========================================================
    private void BuildPanel()
    {
        var world=GetNodeOrNull<WorldScene>("../..");
        if(world?.MacroMap==null) return;
        Layer=30;
        _panel=new PanelContainer { Visible=false,AnchorRight=1,AnchorBottom=1,OffsetLeft=24,OffsetTop=64,OffsetRight=-24,OffsetBottom=-24 };
        AddChild(_panel);
        var box=new VBoxContainer(); _panel.AddChild(box);
        var metrics=world.PlanningMetrics;
        box.AddChild(new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart,
            Text=metrics==null ? "Frozen world restored — generation timings unavailable (no replanning)." : $"Creation: water {metrics.WaterMilliseconds:F1} ms / biomes {metrics.BiomesMilliseconds:F1} ms / peak frontier {metrics.PeakGrowthQueue:N0} / palette estimate {metrics.EstimatedPaletteBytes/1048576d:F1} MiB" });
        var view=new MacroMapView { SizeFlagsVertical=Control.SizeFlags.ExpandFill,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        box.AddChild(view); view.Bind(world.MacroMap,world.WaterPlan,world.FeaturePlan,world.ClimatePlan,world.Config.Generation,world.Environment?.RegionalClimate?.Config);
    }
    // The viewer handles pause/resume through visibility events and stays asleep when hidden.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if(@event is InputEventKey { Pressed:true,Echo:false,Keycode:Key.F7 } && _panel!=null)
        { _panel.Visible=!_panel.Visible; GetViewport().SetInputAsHandled(); }
    }
}
