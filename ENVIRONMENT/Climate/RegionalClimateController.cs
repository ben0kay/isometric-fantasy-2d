// One position-aware weather resolver and bounded camera field; cell snow advances lazily on access.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public readonly record struct RegionalConditions(float Temperature,float RainFraction,float SnowFraction,float PermanentCover,float CoverMultiplier);
public partial class RegionalClimateController : Node
{
    public RegionalClimateMasterConfig Config { get; private set; }
    public bool Enabled { get; private set; } = true;
    public bool ManualOverride { get; private set; }
    public bool Active => _plan!=null && Enabled && !ManualOverride;
    public float Precipitation { get; private set; }
    public float MaximumCover { get; private set; }
    public float RainFraction { get; private set; }
    public float SnowFraction { get; private set; }
    public float RainTarget => Active ? Mathf.Clamp(Precipitation,0,1)*RainFraction : _weather.Config.RainIntensity;
    public float SnowRate => Active ? Precipitation*SnowFraction : _snow.SnowfallRate;
    public int VisitedCells => _cells.Count;
    public event Action Changed;
    private WorldClimatePlan _plan;
    private MacroWorldMap _map;
    private SeasonController _seasons;
    private WorldWeather _weather;
    private SnowProfile _snow;
    private readonly Dictionary<string,BiomeWeatherResponse> _responses=new(StringComparer.Ordinal);
    private sealed class CellState { public int Key; public float Cover; public double Updated; }
    private readonly Dictionary<int,CellState> _cells=new();
    private Image _image;
    private ImageTexture _texture;
    private Rect2 _fieldBounds;
    private double _seconds,_untilUpdate;
    private bool _dirty=true,_unsettled,_publishedEnabled;
    // Disabled Node callbacks: the existing EnvironmentRuntime camera pass supplies constant-time scheduling.
    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    // Bind one frozen world and shared clock/weather; no managers are created for individual biomes.
    // =========================================================
    public void Initialize(WorldClimatePlan plan,MacroWorldMap map,WorldGenerationMasterConfig generation,SeasonController seasons,WorldWeather weather,SnowProfile snow,RegionalClimateMasterConfig config)
    {
        _plan=plan; _map=map; _seasons=seasons; _weather=weather; _snow=snow;
        Config=(RegionalClimateMasterConfig)(config ?? new RegionalClimateMasterConfig()).Duplicate(); Config.Validate(); Enabled=Config.Enabled;
        if(generation?.Biomes!=null) foreach(var biome in generation.Biomes)
        { biome.WeatherResponse?.Validate(); _responses[biome.Id]=biome.WeatherResponse; }
        _seasons.Changed+=MarkDirty; _snow.Changed+=MarkDirty;
        if(_plan==null) return;
        _image=Image.CreateEmpty(Config.FieldResolution,Config.FieldResolution,false,Image.Format.Rgba8);
        _texture=ImageTexture.CreateFromImage(_image);
        RenderingServer.GlobalShaderParameterSet("regional_weather_field",_texture);
    }
    // Seasonal July maximum and January minimum share the canonical January-based year phase.
    // =========================================================
    public RegionalConditions ConditionsAt(Vector2 position)
    {
        if(_plan==null || !_plan.Bounds.HasPoint(position)) return new(15,0,0,0,0);
        var baseline=_plan.Sample(position);
        string id=_map.BiomeAt(position)?.Id;
        var response=id!=null && _responses.TryGetValue(id,out var value) ? value : null;
        return RegionalClimateMath.Resolve(baseline,_seasons.YearPosition,Config,response);
    }
    // Called by existing camera lifecycle; settled stationary dry weather does no field sampling/upload.
    // =========================================================
    public void Tick(double delta,Rect2 view)
    {
        if(_plan==null) return;
        if(!Active) { PublishEnabled(false); return; }
        _seconds+=Math.Max(0,delta); _untilUpdate-=delta;
        bool moved=!_fieldBounds.Grow(-Config.ViewMargin*0.5f).Encloses(view);
        if(!_dirty && !moved && !_unsettled && Precipitation<=0) return;
        if(_untilUpdate>0 && !moved) return;
        _untilUpdate=Config.UpdateSeconds; _dirty=false;
        if(moved) _fieldBounds=view.Grow(Config.ViewMargin);
        _unsettled=false; MaximumCover=0; RainFraction=0; SnowFraction=0;
        for(int y=0;y<Config.FieldResolution;y++) for(int x=0;x<Config.FieldResolution;x++)
        {
            Vector2 point=_fieldBounds.Position+new Vector2((x+0.5f)/Config.FieldResolution,(y+0.5f)/Config.FieldResolution)*_fieldBounds.Size;
            var condition=ConditionsAt(point);
            float cover=CoverAt(point);
            _image.SetPixel(x,y,new Color(cover,condition.RainFraction,condition.SnowFraction,1));
            MaximumCover=Mathf.Max(MaximumCover,cover); RainFraction=Mathf.Max(RainFraction,condition.RainFraction); SnowFraction=Mathf.Max(SnowFraction,condition.SnowFraction);
        }
        _texture.Update(_image);
        RenderingServer.GlobalShaderParameterSet("regional_weather_rect",new Vector4(_fieldBounds.Position.X,_fieldBounds.Position.Y,_fieldBounds.Size.X,_fieldBounds.Size.Y));
        PublishEnabled(true);
        Changed?.Invoke();
    }
    // State lives at macro-cell centres; bilinear queries give seam-safe cover at arbitrary positions.
    // =========================================================
    public float CoverAt(Vector2 point)
    {
        if(_map==null || !_map.Bounds.HasPoint(point)) return 0;
        Vector2 p=(point-_map.Bounds.Position)/_map.CellSize-Vector2.One*0.5f;
        int x=Mathf.FloorToInt(p.X),y=Mathf.FloorToInt(p.Y);
        return Mathf.Lerp(Mathf.Lerp(CellCover(x,y),CellCover(x+1,y),p.X-x),Mathf.Lerp(CellCover(x,y+1),CellCover(x+1,y+1),p.X-x),p.Y-y);
    }
    // Lazy elapsed-time integration touches only viewed coarse cells, once per update timestamp.
    // Unloaded intervals use present conditions as an explicit approximation, never full-world simulation.
    // =========================================================
    private float CellCover(int x,int y)
    {
        x=Math.Clamp(x,0,_map.Columns-1); y=Math.Clamp(y,0,_map.Rows-1); int key=y*_map.Columns+x;
        float left=x*_map.CellSize,top=y*_map.CellSize;
        Vector2 point=_map.Bounds.Position+new Vector2(left+Mathf.Min(_map.CellSize,_map.Bounds.Size.X-left)*0.5f,top+Mathf.Min(_map.CellSize,_map.Bounds.Size.Y-top)*0.5f);
        var conditions=ConditionsAt(point);
        if(!_cells.TryGetValue(key,out var cell))
        { cell=new CellState { Key=key,Cover=conditions.PermanentCover,Updated=_seconds }; _cells.Add(key,cell); }
        float elapsed=(float)Math.Min(86400,_seconds-cell.Updated);
        float cover=cell.Cover;
        if(conditions.Temperature>0) cover=Mathf.MoveToward(cover,conditions.PermanentCover,elapsed/Config.MeltSeconds*Mathf.Clamp(conditions.Temperature/5,0,4));
        else if(Precipitation>0 && conditions.SnowFraction>0) cover=Mathf.MoveToward(cover,1,elapsed/Config.AccumulationSeconds*Mathf.Clamp(Precipitation,0,1)*conditions.SnowFraction);
        cover=Mathf.Max(cover,conditions.PermanentCover);
        cell.Cover=cover; cell.Updated=_seconds;
        if((cover>conditions.PermanentCover && conditions.Temperature>0) || (cover<1 && Precipitation>0 && conditions.SnowFraction>0)) _unsettled=true;
        return cover*conditions.CoverMultiplier;
    }
    // Debug commands wake field work directly and leave authored Resources untouched.
    // =========================================================
    public void SetPrecipitation(float value) { Precipitation=float.IsFinite(value) ? Mathf.Clamp(value,0,3) : 0; MarkDirty(); RefreshMode(); }
    public void SetEnabled(bool enabled) { Enabled=enabled; MarkDirty(); RefreshMode(); }
    public void SetManualOverride(bool value) { ManualOverride=value; MarkDirty(); RefreshMode(); }
    private void MarkDirty() { _dirty=true; _untilUpdate=0; }
    private void RefreshMode()
    {
        if(!Active) PublishEnabled(false);
        Changed?.Invoke();
    }
    // Global enable changes are explicit, not rewritten by the existing camera pass while disabled.
    // =========================================================
    private void PublishEnabled(bool enabled)
    {
        if(_publishedEnabled==enabled) return;
        _publishedEnabled=enabled; RenderingServer.GlobalShaderParameterSet("regional_weather_enabled",enabled);
    }
    // Checkpoint owns precipitation and visited pack; no scene or Resource references cross SAVE boundaries.
    // =========================================================
    public SavedRegionalClimate Capture() => new(_seconds,Precipitation,Enabled,false,_cells.Values.OrderBy(c=>c.Key).Select(c=>new SavedSnowCell(c.Key,c.Cover,c.Updated)).ToArray());
    public void Restore(SavedRegionalClimate state)
    {
        if(state==null || _map==null) return; state.Validate(_map.Columns*_map.Rows);
        _seconds=state.Seconds; Precipitation=state.Precipitation; Enabled=state.Enabled; ManualOverride=state.ManualOverride;
        _cells.Clear(); foreach(var cell in state.Cells) _cells.Add(cell.Key,new CellState { Key=cell.Key,Cover=cell.Cover,Updated=cell.Updated }); MarkDirty(); RefreshMode();
    }
    // Clear world-scoped shader state before releasing its texture; PreviewMap remains unaffected.
    // =========================================================
    public override void _ExitTree()
    {
        if(_seasons!=null) _seasons.Changed-=MarkDirty; if(_snow!=null) _snow.Changed-=MarkDirty;
        if(_plan!=null) { PublishEnabled(false); RenderingServer.GlobalShaderParameterSet("regional_weather_field",default); }
        _image?.Dispose(); _texture?.Dispose(); _cells.Clear(); Changed=null;
    }
}
