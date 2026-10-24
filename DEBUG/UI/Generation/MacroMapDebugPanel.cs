// Removable F7 overview of biome geography and P2 feature reservations; never drives production generation or player movement.
using Godot;
using System.Linq;
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
        if (GetNodeOrNull<WorldScene>("../..") is not WorldScene world || world.MacroMap is not { } map) return;
        Layer = 30;
        _panel = new PanelContainer { Visible = false, OffsetLeft = 24, OffsetTop = 80, OffsetRight = 760, OffsetBottom = 580 };
        AddChild(_panel);
        var box = new VBoxContainer(); _panel.AddChild(box);
        box.AddChild(new Label { Text = "F7 — FINITE WORLD BLUEPRINT (finite world / reservations)" });
        box.AddChild(new Label { Text = $"Lakes: {world.WaterPlan?.Bodies.Count(b => b.River==null) ?? 0} / Rivers: {world.WaterPlan?.Bodies.Count(b => b.River!=null) ?? 0} (blue/cyan; {(world.Blueprint.SchemaVersion >= 5 ? "playable" : "metadata only")})" });
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
            Vector2 sample = map.Bounds.Position + new Vector2((x+0.5f)*map.CellSize,(y+0.5f)*map.CellSize);
            if (world.WaterPlan != null)
                foreach (var body in world.WaterPlan.Bodies)
                    if (body.Bounds.HasPoint(sample) && body.SignedDistance(sample) >= 0) colour = new Color(0.15f,0.55f,0.95f);
            image.SetPixel(x, y, colour);
        }
        if (world.WaterPlan!=null)
            foreach (var body in world.WaterPlan.Bodies)
                if (body.River!=null) DrawRiver(image,map,body.River);
        box.AddChild(new TextureRect { Texture = ImageTexture.CreateFromImage(image), CustomMinimumSize = new Vector2(700, 460),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        image.Dispose();
    }
    // Coarse cells can miss narrow water; draw the saved centerline explicitly for the removable F7 overview.
    // =========================================================
    private static void DrawRiver(Image image,MacroWorldMap map,PlannedRiverPath path)
    {
        Vector2 Pixel(Vector2 p) => (p-map.Bounds.Position)/map.CellSize;
        void Dot(Vector2 p,Color colour)
        {
            int x=Mathf.FloorToInt(p.X),y=Mathf.FloorToInt(p.Y);
            if (x>=0 && y>=0 && x<image.GetWidth() && y<image.GetHeight()) image.SetPixel(x,y,colour);
        }
        for (int i=1;i<path.Points.Count;i++)
        {
            var a=Pixel(path.Points[i-1]); var b=Pixel(path.Points[i]);
            int steps=Mathf.Max(1,Mathf.CeilToInt((b-a).Length()*2));
            for (int j=0;j<=steps;j++) Dot(a.Lerp(b,j/(float)steps),new Color(0.1f,0.85f,1f));
        }
        Dot(Pixel(path.Points[0]),Colors.White); Dot(Pixel(path.Points[^1]),new Color(0.2f,0.5f,1f));
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
