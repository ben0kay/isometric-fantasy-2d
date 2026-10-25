// Removable right-side world diagnostics owned by the F9 controls; never drives streaming or generation.
using Godot;
using System.Linq;
using System.Text;
public partial class WorldDiagnosticsPanel : PanelContainer
{
    public WorldScene World { get; set; }
    public EnvironmentRuntime Environment { get; set; }
    private Label _live, _trace, _fps;
    private double _statusTimer;

    // Use viewport anchors so the diagnostic column stays on the right when the window resizes.
    // =========================================================
    public override void _Ready()
    {
        VisibilityChanged+=()=>SetProcess(IsVisibleInTree());
        AnchorLeft = 1; AnchorRight = 1; AnchorBottom = 1;
        OffsetLeft = -456; OffsetRight = -16; OffsetTop = 16; OffsetBottom = -16;
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(scroll);
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(box);
        box.AddChild(new Label { Text = "World diagnostics — F9 to hide" });
        var culling = new CheckButton { Text = "Cull off-screen scenery cosmetics", FocusMode = Control.FocusModeEnum.None,
            ButtonPressed = Environment.Config.CullOffscreenSceneryEffects };
        culling.Toggled += enabled => Environment.Config.CullOffscreenSceneryEffects = enabled;
        box.AddChild(culling);
        _fps = new Label { Text = "FPS —", HorizontalAlignment = HorizontalAlignment.Center };
        _fps.AddThemeFontSizeOverride("font_size", 23);
        box.AddChild(_fps);
        _live = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_live);
        var tracing = new CheckButton { Text = "Trace C# callbacks", FocusMode = Control.FocusModeEnum.None };
        tracing.Toggled += enabled =>
        {
            if (enabled) RuntimeFrameTrace.Start(GetInstanceId());
            else RuntimeFrameTrace.Stop(GetInstanceId());
        };
        box.AddChild(tracing);
        var resetTrace = new Button { Text = "Reset trace capture", FocusMode = Control.FocusModeEnum.None };
        resetTrace.Pressed += () => RuntimeFrameTrace.Reset(); box.AddChild(resetTrace);
        var copyTrace = new Button { Text = "Copy trace report", FocusMode = Control.FocusModeEnum.None };
        copyTrace.Pressed += () => DisplayServer.ClipboardSet(RuntimeFrameTrace.Report()); box.AddChild(copyTrace);
        _trace = new Label { Text = "Enable tracing, walk until a hitch, then copy the report. Tracing continues while F9 is hidden.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_trace);
        var world = World;
        if (world.MacroMap is not { } map) return;
        var text = new StringBuilder($"Seed {map.Seed} | v{map.GenerationVersion} | {map.Columns}×{map.Rows} cells | {map.Regions.Count} regions\nBounds {map.Bounds} | cell {map.CellSize} | {map.NonLandCells} ocean cells / all remaining land assigned\nHash {map.Hash[..16]} | World {world.Blueprint.WorldId[..12]}\n");
        for (int b = 0; b < map.Biomes.Count; b++)
        {
            var regions = map.Regions.Where(r => r.BiomeIndex == b).ToArray();
            int cells = regions.Sum(r => r.CellCount);
            text.AppendLine($"{map.Biomes[b].Name}: {regions.Length} regions, {100.0 * cells / (map.Columns * map.Rows):F1}% cells; actual region cells {(regions.Length == 0 ? 0 : regions.Min(r => r.CellCount))}–{(regions.Length == 0 ? 0 : regions.Max(r => r.CellCount))}");
        }
        if (world.FeaturePlan is { } features)
        {
            text.AppendLine($"Reservations {features.Reservations.Features.Count} | hash {features.Hash[..12]} | tint includes clearance");
            foreach (var feature in features.Reservations.Features.Take(4))
                text.AppendLine($"{feature.Name} [{feature.Id}] — {(feature.Required ? "required" : "preferred")}, priority {feature.Priority}");
            if (features.Reservations.Features.Count > 4) text.AppendLine($"+ {features.Reservations.Features.Count - 4} more (see generation log)");
            foreach (string warning in features.Warnings.Take(2)) text.AppendLine(warning);
        }
        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = text.ToString(), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
    }
    // Refresh four times per second only while F9 diagnostics are visible, even when F7 is closed.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldDiagnosticsPanel.Process");
        if (!Visible || _live == null) return;
        _statusTimer += delta; if (_statusTimer < 0.25) return; _statusTimer = 0;
        float fps = (float)Engine.GetFramesPerSecond();
        _fps.Text = $"{fps} FPS";
        _fps.AddThemeColorOverride("font_color", FpsColour(fps));
        _trace.Text = RuntimeFrameTrace.Report();
        if (World.Stream == null) { _live.Text = "Compatibility preview — chunk streaming disabled"; return; }
        var stream = World.Stream;
        var environment = Environment;
        var shadows = environment.DayNight.Shadows;
        _live.Text = $"Player chunk {stream.PlayerChunk} | loaded {stream.LoadedCount} | pending {stream.PendingCount}\n"
            + $"Engine process monitor (sampled) {Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:F1}ms | physics {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)*1000:F1}ms\n"
            + $"Stream last/peak {stream.LastFrameMilliseconds:F2}/{stream.PeakFrameMilliseconds:F2}ms | data last/peak {stream.LastDataMilliseconds:F2}/{stream.PeakDataMilliseconds:F2}ms\n"
            + $"Last item {stream.LastApplyMilliseconds:F2}ms | chunk bind total {stream.LastBindMilliseconds:F2}ms | nav {stream.LastNavigationMilliseconds:F2}ms | unload {stream.LastUnloadMilliseconds:F2}ms\n"
            + $"Cosmetic chunks {environment.CosmeticChunkCount}/{environment.RegisteredChunkCount} | sun casters relevant {shadows.UpdatedCasterCount}/{shadows.TotalCasterCount} | projections {shadows.LastFrameProjectionWrites} | sun {shadows.LastProjectionMilliseconds:F2}ms\n"
            + $"Published {stream.PublishedChunks} / released {stream.ReleasedChunks} | {stream.PlayerBiome}";
    }
    // Smooth warning colours: green at 60+, yellow at 45, orange at 30, red at 15 or below.
    // =========================================================
    private static Color FpsColour(float fps)
    {
        Color green = new(0.30f, 0.90f, 0.40f), yellow = new(1f, 0.88f, 0.20f);
        Color orange = new(1f, 0.50f, 0.12f), red = new(1f, 0.23f, 0.23f);
        if (fps >= 60) return green;
        if (fps >= 45) return yellow.Lerp(green, (fps - 45f) / 15f);
        if (fps >= 30) return orange.Lerp(yellow, (fps - 30f) / 15f);
        return red.Lerp(orange, Mathf.Clamp((fps - 15f) / 15f, 0f, 1f));
    }

    // A removed world/debug panel cannot leave global tracing active in another scene.
    // =========================================================
    public override void _ExitTree() => RuntimeFrameTrace.Release(GetInstanceId());
}
