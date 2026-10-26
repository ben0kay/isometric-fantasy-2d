// Shared coarse map viewer for F7 and the standalone PreviewMap; never creates gameplay chunks or replans worlds.
using Godot;
public partial class MacroMapView : VBoxContainer
{
    private MacroWorldMap _map;
    private WaterBodyPlan _water;
    private WorldFeaturePlan _features;
    private WorldClimatePlan _climatePlan;
    private TextureRect _view;
    private Label _status,_legend,_dimensions;
    private OptionButton _selector;
    private Image _image;
    private readonly ImageTexture[] _textures=new ImageTexture[3];
    private int _row,_layer;
    private bool _pendingLayer;

    // Hidden/settled viewers are dormant; visibility changes directly resume incomplete work.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false);
        VisibilityChanged+=OnVisibilityChanged;
    }
    // Bind immutable results from either caller; replacing plans releases all obsolete raster caches.
    // =========================================================
    public void Bind(MacroWorldMap map,WaterBodyPlan water,WorldFeaturePlan features,WorldClimatePlan climate)
    {
        ReleaseImages(); _map=map; _water=water; _features=features; _climatePlan=climate;
        if(_view==null) BuildControls();
        _selector.SetItemDisabled(1,climate==null);
        if(climate==null && _layer==1) _layer=0;
        _selector.Select(_layer);
        _dimensions.Text=$"{map.Bounds.Size.X:N0} × {map.Bounds.Size.Y:N0} units | {map.Columns} × {map.Rows} macro cells | {map.Regions.Count} regions | generator {map.GenerationVersion}";
        ShowLayer();
    }
    // Controls belong to the viewer; neither host owns duplicate palette or water drawing code.
    // =========================================================
    private void BuildControls()
    {
        _dimensions=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_dimensions);
        _selector=new OptionButton(); _selector.AddItem("Biomes"); _selector.AddItem("Climate"); _selector.AddItem("Water");
        _selector.ItemSelected+=index=> { _layer=(int)index; ShowLayer(); }; AddChild(_selector);
        _legend=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_legend);
        _status=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_status);
        _view=new TextureRect { SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered };
        AddChild(_view);
    }
    // Start raster work only for the selected layer; keep just three completed coarse textures.
    // =========================================================
    private void ShowLayer()
    {
        _image?.Dispose(); _image=null; SetProcess(false);
        _view.Texture=_textures[_layer];
        _legend.Text=_layer==1 ? "Cold blue → temperate green → hot orange; enchanted purple. Baseline climate, not live weather." : _layer==2 ? "Ocean navy / lakes blue / rivers cyan; white crosses = reserved anchors." : BiomeLegend();
        if(_textures[_layer]!=null) { _status.Text=LayerStatus(); return; }
        if(!IsVisibleInTree()) { _pendingLayer=true; return; }
        _pendingLayer=false;
        _image=Image.CreateEmpty(_map.Columns,_map.Rows,false,Image.Format.Rgba8);
        _row=0; _status.Text="Preparing coarse overview…"; SetProcess(IsVisibleInTree());
    }
    // Stable legend identities/colours come from the same frozen map displayed in the raster.
    // =========================================================
    private string BiomeLegend()
    {
        var text=new System.Text.StringBuilder("Biomes: ");
        foreach(var biome in _map.Biomes) text.Append($"{biome.Name} #{biome.Colour.ToHtml(false)}   ");
        return text.ToString();
    }
    // Hashes describe real canonical outcomes; no debug-generated world identity is introduced.
    // =========================================================
    private string LayerStatus() => _layer==1 ? $"Frozen climate hash: {_climatePlan.Hash}" : _layer==2 ? $"Frozen water hash: {_water?.Hash ?? "none"}" : $"Frozen biome hash: {_map.Hash}";
    // Small row batches avoid a hidden startup raster. One row may exceed the soft 2ms budget.
    // =========================================================
    public override void _Process(double delta)
    {
        if(_image==null || !IsVisibleInTree()) { SetProcess(false); return; }
        var map=_map; ulong start=Time.GetTicksUsec(); int rows=0;
        while(_row<map.Rows && rows++<8)
        {
            for(int x=0;x<map.Columns;x++) _image.SetPixel(x,_row,CellColour(x,_row));
            _row++;
            if(Time.GetTicksUsec()-start>=2000) break;
        }
        _status.Text=$"Preparing coarse overview: {_row*100/map.Rows}%";
        if(_row<map.Rows) return;
        if(_water!=null) foreach(var body in _water.Bodies) if(body.River!=null) DrawRiver(_image,map,body.River);
        var texture=ImageTexture.CreateFromImage(_image);
        _textures[_layer]=texture;
        _view.Texture=texture; _image.Dispose(); _image=null; SetProcess(false);
        _status.Text=LayerStatus();
    }
    // Palette sampling shares the authoritative water and climate query contracts, including edge-cell clipping.
    // =========================================================
    private Color CellColour(int x,int y)
    {
        var map=_map;
        Vector2 sample=(map.Bounds.Position+new Vector2(x+0.5f,y+0.5f)*map.CellSize).Clamp(map.Bounds.Position,map.Bounds.End-Vector2.One*0.01f);
        int region=map.RegionIdAtCell(x,y);
        Color colour=region<0 ? new Color(0.045f,0.2f,0.36f) : map.Biomes[map.Regions[region].BiomeIndex].Colour;
        if(_layer==2 && region>=0) colour=new Color(0.22f,0.28f,0.22f);
        if(_layer==1)
        {
            var climate=_climatePlan.Sample(sample);
            float t=Mathf.Clamp((climate.TemperatureCelsius+24)/62,0,1);
            colour=t<0.5f ? new Color(0.25f,0.55f,0.9f).Lerp(new Color(0.35f,0.65f,0.35f),t*2)
                : new Color(0.35f,0.65f,0.35f).Lerp(new Color(0.95f,0.45f,0.15f),(t-0.5f)*2);
            colour=colour.Lerp(new Color(0.65f,0.25f,0.8f),climate.Enchantment);
        }
        else if(_layer==0 && ((x>0 && map.RegionIdAtCell(x-1,y)!=region) || (y>0 && map.RegionIdAtCell(x,y-1)!=region))) colour=colour.Darkened(0.4f);
        if(_water!=null) foreach(var body in _water.Bodies)
            if(body.Bounds.HasPoint(sample) && body.SignedDistance(sample)>=0) colour=body.IsOcean ? new Color(0.045f,0.2f,0.36f) : new Color(0.15f,0.55f,0.95f);
        if(_features!=null) foreach(var feature in _features.Reservations.Features)
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
    // Hide pauses incomplete work; showing resumes it without restarting generation or allocating new chunks.
    // =========================================================
    private void OnVisibilityChanged()
    {
        if(IsVisibleInTree() && _pendingLayer) { ShowLayer(); return; }
        SetProcess(IsVisibleInTree() && _image!=null);
    }
    // Release owned images/textures when rebinding or leaving the scene.
    // =========================================================
    private void ReleaseImages()
    {
        SetProcess(false); _pendingLayer=false; if(_view!=null) _view.Texture=null;
        _image?.Dispose(); _image=null;
        for(int i=0;i<_textures.Length;i++) { _textures[i]?.Dispose(); _textures[i]=null; }
    }
    // No incomplete raster job or texture cache survives scene unload.
    // =========================================================
    public override void _ExitTree() => ReleaseImages();
}
