// Removable right-side world diagnostics owned by the F9 controls; never drives streaming or generation.
using Godot;
using System.Linq;
using System.Text;
public partial class WorldDiagnosticsPanel : PanelContainer
{
    public WorldScene World { get; set; }
    public EnvironmentRuntime Environment { get; set; }
    private Label _live;
    private double _statusTimer;

    // Use viewport anchors so the diagnostic column stays on the right when the window resizes.
    // =========================================================
    public override void _Ready()
    {
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
        _live = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_live);
        var world = World;
        if (world.MacroMap is not { } map) return;
        var text = new StringBuilder($"Seed {map.Seed} | v{MacroWorldMap.GeneratorVersion} | {map.Columns}×{map.Rows} cells | {map.Regions.Count} regions\nBounds {map.Bounds} | cell {map.CellSize} | 100% land assigned\nHash {map.Hash[..16]} | World {world.Blueprint.WorldId[..12]}\n");
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
        if (!Visible || _live == null) return;
        _statusTimer += delta; if (_statusTimer < 0.25) return; _statusTimer = 0;
        if (World.Stream == null) { _live.Text = "Compatibility preview — chunk streaming disabled"; return; }
        var stream = World.Stream;
        var environment = Environment;
        var shadows = environment.DayNight.Shadows;
        _live.Text = $"Player chunk {stream.PlayerChunk} | loaded {stream.LoadedCount} | pending {stream.PendingCount}\n"
            + $"{Engine.GetFramesPerSecond()} FPS | engine process {Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:F1}ms | physics {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)*1000:F1}ms\n"
            + $"Stream last/peak {stream.LastFrameMilliseconds:F2}/{stream.PeakFrameMilliseconds:F2}ms | data last/peak {stream.LastDataMilliseconds:F2}/{stream.PeakDataMilliseconds:F2}ms\n"
            + $"Last item {stream.LastApplyMilliseconds:F2}ms | chunk bind total {stream.LastBindMilliseconds:F2}ms | nav {stream.LastNavigationMilliseconds:F2}ms | unload {stream.LastUnloadMilliseconds:F2}ms\n"
            + $"Cosmetic chunks {environment.CosmeticChunkCount}/{environment.RegisteredChunkCount} | sun casters updated {shadows.UpdatedCasterCount}/{shadows.TotalCasterCount} | sun {shadows.LastProjectionMilliseconds:F2}ms\n"
            + $"Published {stream.PublishedChunks} / released {stream.ReleasedChunks} | {stream.PlayerBiome}";
    }
}
