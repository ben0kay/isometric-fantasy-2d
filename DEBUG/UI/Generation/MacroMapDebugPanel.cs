// Removable F7 overview of biome geography and P2 feature reservations; never drives production generation or player movement.
using Godot;
using System.Linq;
using System.Text;
public partial class MacroMapDebugPanel : CanvasLayer
{
    private PanelContainer _panel;
    #region Overview
    // Bind after world startup; absent/failed generation simply omits this optional panel.
    // =========================================================
    public override void _Ready() => CallDeferred(nameof(BuildPanel));
    // Build a single coarse image; no world scenery is instantiated to draw this overview.
    // =========================================================
    private void BuildPanel()
    {
        if (GetParent() is not WorldScene world || world.MacroMap is not { } map) return;
        Layer = 30;
        _panel = new PanelContainer { Visible = false, OffsetLeft = 24, OffsetTop = 80, OffsetRight = 760, OffsetBottom = 780 };
        AddChild(_panel);
        var box = new VBoxContainer(); _panel.AddChild(box);
        box.AddChild(new Label { Text = "F7 — FINITE WORLD BLUEPRINT (overview only; playable preview unchanged)" });
        var image = Image.CreateEmpty(map.Columns, map.Rows, false, Image.Format.Rgba8);
        for (int y = 0; y < map.Rows; y++) for (int x = 0; x < map.Columns; x++)
        {
            int region = map.RegionIdAtCell(x, y); Color colour = map.Biomes[map.Regions[region].BiomeIndex].Colour;
            if ((x > 0 && map.RegionIdAtCell(x - 1, y) != region) || (y > 0 && map.RegionIdAtCell(x, y - 1) != region)) colour = colour.Darkened(0.4f);
            if (world.FeaturePlan is { } plan)
            {
                Vector2 point = map.Bounds.Position + new Vector2((x + 0.5f) * map.CellSize, (y + 0.5f) * map.CellSize);
                foreach (var feature in plan.Reservations.Features)
                {
                    if (feature.OverlapsDisk(point, 0)) colour = colour.Lerp(feature.Colour, 0.75f);
                    int ax = Mathf.FloorToInt((feature.Anchor.X - map.Bounds.Position.X) / map.CellSize);
                    int ay = Mathf.FloorToInt((feature.Anchor.Y - map.Bounds.Position.Y) / map.CellSize);
                    if ((x == ax && Mathf.Abs(y - ay) <= 1) || (y == ay && Mathf.Abs(x - ax) <= 1)) colour = Colors.White;
                }
            }
            image.SetPixel(x, y, colour);
        }
        box.AddChild(new TextureRect { Texture = ImageTexture.CreateFromImage(image), CustomMinimumSize = new Vector2(700, 460),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        image.Dispose();
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
        var details = new ScrollContainer { CustomMinimumSize = new Vector2(700, 200),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        details.AddChild(new Label { Text = text.ToString(), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        box.AddChild(details);
    }
    // Toggle only the overview, keeping input and preview behaviour independent.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F7 } && _panel != null)
        { _panel.Visible = !_panel.Visible; GetViewport().SetInputAsHandled(); }
    }
    #endregion
}
