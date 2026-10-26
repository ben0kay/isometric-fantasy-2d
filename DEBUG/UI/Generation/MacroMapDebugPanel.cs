// Removable on-demand F7 overview of canonical biome/climate/geography outcomes; no generation ownership.
using Godot;
using System.Linq;
public partial class MacroMapDebugPanel : CanvasLayer
{
    private PanelContainer _panel;
    private WorldScene _world;
    private TextureRect _view;
    private Label _status;
    private Button _toggle;
    private Image _image;
    private ImageTexture _biomes,_climate;
    private bool _showClimate;
    private int _row;
    #region Overview
    // Set up controls only; hidden F7 never allocates a map raster or starts callbacks.
    // =========================================================
    public override void _Ready() { SetProcess(false); CallDeferred(nameof(BuildPanel)); }
    // Bind the optional panel after production startup; preserve no-map startup failure behavior.
    // =========================================================
    private void BuildPanel()
    {
        _world=GetNodeOrNull<WorldScene>("../..");
        if(_world?.MacroMap==null) return;
        Layer=30;
        _panel=new PanelContainer { Visible=false,AnchorRight=1,AnchorBottom=1,OffsetLeft=24,OffsetTop=64,OffsetRight=-24,OffsetBottom=-24 };
        AddChild(_panel);
        var box=new VBoxContainer(); _panel.AddChild(box);
        box.AddChild(new Label { Text="F7 — WHOLE WORLD / frozen geography and climate",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        var map=_world.MacroMap;
        box.AddChild(new Label { Text=$"{map.Bounds.Size.X:N0} × {map.Bounds.Size.Y:N0} units | {map.Columns} × {map.Rows} cells | {_world.WaterPlan?.Bodies.Count(b=>b.IsOcean) ?? 0} ocean(s)",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        _toggle=new Button { Text="Show climate",Disabled=_world.ClimatePlan==null };
        _toggle.Pressed+=ToggleClimate; box.AddChild(_toggle);
        box.AddChild(new Label { Text=_world.ClimatePlan==null ? "Legacy world: no frozen climate; current preset is not applied." : "Climate: blue = cold / green = temperate / orange = hot / purple = enchanted. Baseline only; biome affinity and weather remain BC2/BC3.",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        _status=new Label(); box.AddChild(_status);
        _view=new TextureRect { SizeFlagsVertical=Control.SizeFlags.ExpandFill,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered };
        box.AddChild(_view);
    }
    // Select cached output or start bounded raster work only when the panel is visible.
    // =========================================================
    private void ShowLayer()
    {
        _image?.Dispose(); _image=null; SetProcess(false);
        var cached=_showClimate ? _climate : _biomes;
        _view.Texture=cached;
        _toggle.Text=_showClimate ? "Show biomes" : "Show climate";
        if(cached!=null) { _status.Text=_showClimate ? $"Frozen climate hash: {_world.ClimatePlan.Hash}" : $"Frozen biome hash: {_world.MacroMap.Hash}"; return; }
        _image=Image.CreateEmpty(_world.MacroMap.Columns,_world.MacroMap.Rows,false,Image.Format.Rgba8);
        _row=0; _status.Text="Preparing coarse overview…"; SetProcess(_panel.Visible);
    }
    // Climate toggle is session-only and never changes a blueprint, weather setting or scene chunk.
    // =========================================================
    private void ToggleClimate() { _showClimate=!_showClimate; ShowLayer(); }
    // Small row batches avoid a hidden startup raster. One row may exceed the soft 2ms budget.
    // =========================================================
    public override void _Process(double delta)
    {
        if(_image==null || !_panel.Visible) { SetProcess(false); return; }
        var map=_world.MacroMap; ulong start=Time.GetTicksUsec(); int rows=0;
        while(_row<map.Rows && rows++<8)
        {
            for(int x=0;x<map.Columns;x++) _image.SetPixel(x,_row,CellColour(x,_row));
            _row++;
            if(Time.GetTicksUsec()-start>=2000) break;
        }
        _status.Text=$"Preparing coarse overview: {_row*100/map.Rows}%";
        if(_row<map.Rows) return;
        if(_world.WaterPlan!=null) foreach(var body in _world.WaterPlan.Bodies) if(body.River!=null) DrawRiver(_image,map,body.River);
        var texture=ImageTexture.CreateFromImage(_image);
        if(_showClimate) _climate=texture; else _biomes=texture;
        _view.Texture=texture; _image.Dispose(); _image=null; SetProcess(false);
        _status.Text=_showClimate ? $"Frozen climate hash: {_world.ClimatePlan.Hash}" : $"Frozen biome hash: {map.Hash}";
    }
    // Palette sampling shares the authoritative water and climate query contracts, including edge-cell clipping.
    // =========================================================
    private Color CellColour(int x,int y)
    {
        var map=_world.MacroMap;
        Vector2 sample=(map.Bounds.Position+new Vector2(x+0.5f,y+0.5f)*map.CellSize).Clamp(map.Bounds.Position,map.Bounds.End-Vector2.One*0.01f);
        int region=map.RegionIdAtCell(x,y);
        Color colour=region<0 ? new Color(0.045f,0.2f,0.36f) : map.Biomes[map.Regions[region].BiomeIndex].Colour;
        if(_showClimate)
        {
            var climate=_world.ClimatePlan.Sample(sample);
            float t=Mathf.Clamp((climate.TemperatureCelsius+24)/62,0,1);
            colour=t<0.5f ? new Color(0.25f,0.55f,0.9f).Lerp(new Color(0.35f,0.65f,0.35f),t*2)
                : new Color(0.35f,0.65f,0.35f).Lerp(new Color(0.95f,0.45f,0.15f),(t-0.5f)*2);
            colour=colour.Lerp(new Color(0.65f,0.25f,0.8f),climate.Enchantment);
        }
        else if((x>0 && map.RegionIdAtCell(x-1,y)!=region) || (y>0 && map.RegionIdAtCell(x,y-1)!=region)) colour=colour.Darkened(0.4f);
        if(_world.WaterPlan!=null) foreach(var body in _world.WaterPlan.Bodies)
            if(body.Bounds.HasPoint(sample) && body.SignedDistance(sample)>=0) colour=body.IsOcean ? new Color(0.045f,0.2f,0.36f) : new Color(0.15f,0.55f,0.95f);
        if(_world.FeaturePlan!=null) foreach(var feature in _world.FeaturePlan.Reservations.Features)
        {
            if(feature.OverlapsDisk(sample,0)) colour=colour.Lerp(feature.Colour,0.75f);
            int ax=Mathf.FloorToInt((feature.Anchor.X-map.Bounds.Position.X)/map.CellSize),ay=Mathf.FloorToInt((feature.Anchor.Y-map.Bounds.Position.Y)/map.CellSize);
            if((x==ax && Mathf.Abs(y-ay)<=1) || (y==ay && Mathf.Abs(x-ax)<=1)) colour=Colors.White;
        }
        return colour;
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
    // Pause partial work while hidden and resume directly on F7; completed cached layers remain dormant.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if(@event is InputEventKey { Pressed:true,Echo:false,Keycode:Key.F7 } && _panel!=null)
        {
            _panel.Visible=!_panel.Visible;
            if(_panel.Visible && _view.Texture==null && _image==null) ShowLayer();
            else SetProcess(_panel.Visible && _image!=null);
            GetViewport().SetInputAsHandled();
        }
    }
    // Release an unfinished image and panel-owned texture caches when the World unloads.
    // =========================================================
    public override void _ExitTree() { SetProcess(false); _image?.Dispose(); _image=null; _biomes?.Dispose(); _climate?.Dispose(); }
    #endregion
}
