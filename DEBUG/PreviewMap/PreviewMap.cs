// Standalone blueprint-only world preview: never instantiates World, chunks, players, scenery, navigation or effects.
using Godot;
using System;
public partial class PreviewMap : Control
{
    [Export] public PreviewMapMasterConfig Config { get; set; }
    private SpinBox _seed;
    private Button _regenerate,_random;
    private Label _status;
    private MacroMapView _view;
    private bool _busy;

    // Build a small debug shell, then generate only canonical managed planning data.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        var background=new ColorRect { Color=new Color(0.055f,0.065f,0.08f),MouseFilter=MouseFilterEnum.Ignore };
        AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin=new MarginContainer(); AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach(string side in new[] {"left","top","right","bottom"}) margin.AddThemeConstantOverride("margin_"+side,20);
        var box=new VBoxContainer(); margin.AddChild(box);
        box.AddChild(new Label { Text="WORLD PREVIEW — blueprint only / gameplay chunks: 0",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        var tools=new HBoxContainer(); box.AddChild(tools);
        tools.AddChild(new Label { Text="Seed" });
        _seed=new SpinBox { MinValue=int.MinValue,MaxValue=int.MaxValue,Step=1,Value=Config?.Seed ?? 213876,CustomMinimumSize=new Vector2(190,0) }; tools.AddChild(_seed);
        _regenerate=new Button { Text="Regenerate" }; _regenerate.Pressed+=RequestGeneration; tools.AddChild(_regenerate);
        _random=new Button { Text="Random seed" }; _random.Pressed+=RandomSeed; tools.AddChild(_random);
        _status=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; box.AddChild(_status);
        _view=new MacroMapView { SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill }; box.AddChild(_view);
        RequestGeneration();
    }
    // Schedule the synchronous macro planner after UI setup; no always-running callbacks or background scenes.
    // =========================================================
    private void RequestGeneration()
    {
        if(_busy) return;
        _busy=true; _regenerate.Disabled=true; _random.Disabled=true; _seed.Editable=false;
        _status.Text="Generating canonical macro plan… (current synchronous planner; no gameplay chunks)";
        CallDeferred(nameof(Generate));
    }
    // Independent seed selection changes only session UI, never the saved Resource or campaign state.
    // =========================================================
    private void RandomSeed()
    {
        using var rng=new RandomNumberGenerator(); rng.Randomize(); _seed.Value=(int)(rng.Randi()&0x7fffffff); RequestGeneration();
    }
    // Reuse exactly the production pipeline and pond exclusion; the view consumes results without world Nodes.
    // =========================================================
    private void Generate()
    {
        int seed=(int)_seed.Value;
        var timer=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if(Config?.Generation==null) throw new ArgumentException("PreviewMap requires canonical Generation settings.");
            var config=(WorldGenerationMasterConfig)Config.Generation.Duplicate(true);
            Rect2? pond=Config.AuthoredPond==null ? null : WorldLiquidFootprint.WorldBounds(Config.AuthoredPond);
            var result=WorldGenerationPipeline.Build(config,seed,pond);
            timer.Stop();
            _view.Bind(result.Map,result.Geography.Water,result.Features,result.Climate,config);
            string warnings=string.Join(" | ",result.Features.Warnings)+" "+string.Join(" | ",result.Geography.Water.Warnings);
            var metrics=result.Metrics;
            _status.Text=$"Seed {seed} | total {timer.Elapsed.TotalMilliseconds:F1} ms | water {metrics.WaterMilliseconds:F1} / biomes {metrics.BiomesMilliseconds:F1} ms | peak frontier {metrics.PeakGrowthQueue:N0} | cell IDs {metrics.EstimatedFrozenBytes/1048576d:F2} MiB / palette estimate {metrics.EstimatedPaletteBytes/1048576d:F1} MiB | chunks: 0. {warnings}";
        }
        catch(Exception error)
        {
            _status.Text=$"Seed {seed}: generation failed — {error.Message}. Previous successful map, if any, is retained.";
            GD.PushError(error.ToString());
        }
        finally { _busy=false; _regenerate.Disabled=false; _random.Disabled=false; _seed.Editable=true; }
    }
}
