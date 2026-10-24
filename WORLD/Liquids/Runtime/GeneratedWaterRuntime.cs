// World-owned liquid presentation: immutable plan queries outlive bounded chunk tiles and resident region effects.
using Godot;
using System;
using System.Collections.Generic;

public partial class GeneratedWaterRuntime : Node2D
{
    private sealed class Entry { public WorldLiquidRegion Region; public LiquidSurfaceTile Tile; public bool Published; }
    private sealed class Resident { public WorldLiquidRegion Region; public WorldLiquidRainSplashes Rain; public int Owners,Published; public readonly List<LiquidSurfaceTile> Tiles=new(); }
    private readonly Dictionary<ChunkCoord,List<Entry>> _chunks=new();
    private readonly Dictionary<string,Resident> _regions=new(StringComparer.Ordinal);
    private readonly Dictionary<string,WorldLiquidDefinition> _definitions=new(StringComparer.Ordinal);
    private WaterBodyPlan _plan;
    private WorldLiquidMasterConfig _config;
    private WorldWeather _weather;
    public int TileCount { get; private set; }
    public int ResidentCount => _regions.Count;
    public int PreparedChunkCount => _chunks.Count;

    #region Setup and queries
    // Resolve canonical types once, before any scenery or liquid chunks appear; unknown IDs fail explicitly.
    // =========================================================
    public void Initialize(WaterBodyPlan plan,WorldLiquidMasterConfig config)
    {
        _plan=plan; _config=config ?? throw new ArgumentException("Generated water requires shared presentation settings.");
        var water=GD.Load<WorldLiquidDefinition>("res://WORLD/Liquids/Definitions/Water/Water.tres");
        _definitions.Add(water.Id,water);
        foreach (var body in plan.Bodies)
            if (!_definitions.ContainsKey(body.LiquidId)) throw new ArgumentException("No canonical generated liquid definition: "+body.LiquidId);
        _config.Changed+=Refresh; SetProcess(false);
    }
    // Weather is composed after initial streaming; one event wakes shared rain/cosmetics without dry polling.
    // =========================================================
    public void Bind(WorldWeather weather)
    {
        _weather=weather; _weather.RainStateChanged+=Refresh;
        foreach (var resident in _regions.Values) AttachRain(resident);
        Refresh();
    }
    // Placement checks use immutable full geometry, including unloaded neighboring chunks.
    // =========================================================
    public bool Contains(Vector2 point,float margin=0)
    {
        foreach (var body in _plan.Bodies)
            if (body.Intersects(new Rect2(point,Vector2.Zero),Mathf.Max(0,margin)) && body.SignedDistance(point)>=-margin) return true;
        return false;
    }
    // The single player immersion controller only selects regions with published nearby presentation.
    // =========================================================
    public WorldLiquidRegion RegionAt(Vector2 point)
    {
        foreach (var resident in _regions.Values)
            if (resident.Published>0 && resident.Region.Contains(point)) return resident.Region;
        return null;
    }
    #endregion

    #region Chunk lifecycle
    // Create each field in bounded row slices under the streamer's existing application budget.
    // =========================================================
    public IEnumerable<int> PrepareChunkSteps(ChunkCoord coord,Rect2 bounds)
    {
        if (_chunks.ContainsKey(coord)) yield break;
        var entries=new List<Entry>(); _chunks.Add(coord,entries);
        foreach (var body in _plan.Bodies)
        {
            if (!body.Intersects(bounds,40)) continue;
            if (!_regions.TryGetValue(body.Id,out var resident))
            {
                var region=new WorldLiquidRegion { Name=body.Id.Replace('.','_'),PlannedBody=body,Config=_config,
                    Definition=_definitions[body.LiquidId],PresentationActive=false,Visible=false };
                AddChild(region); resident=new Resident { Region=region }; _regions.Add(body.Id,resident);
                if (_weather!=null) AttachRain(resident);
            }
            resident.Owners++;
            var entry=new Entry { Region=resident.Region }; entries.Add(entry);
            // Crop the quad to its chunk; distance samples still use the complete uncut shoreline.
            foreach (int step in resident.Region.PrepareTile(bounds,tile => entry.Tile=tile)) yield return step;
        }
    }
    // A field stays hidden until its ground/scenery chunk is fully ready to enter.
    // =========================================================
    public void PublishChunk(ChunkCoord coord)
    {
        if (!_chunks.TryGetValue(coord,out var entries)) return;
        foreach (var entry in entries)
        {
            if (entry.Published || entry.Tile==null) continue;
            entry.Published=true; entry.Tile.Visible=true; TileCount++;
            var resident=_regions[entry.Region.PlannedBody.Id]; resident.Published++; resident.Tiles.Add(entry.Tile);
        }
        Refresh();
    }
    // Cancellation and unload share one release path; no duplicate body remains after travel/return.
    // =========================================================
    public void ReleaseChunk(ChunkCoord coord)
    {
        if (!_chunks.Remove(coord,out var entries)) return;
        foreach (var entry in entries)
        {
            var resident=_regions[entry.Region.PlannedBody.Id]; resident.Owners--;
            if (entry.Published) { resident.Published--; resident.Tiles.Remove(entry.Tile); TileCount--; }
            if (entry.Tile!=null) resident.Region.ReleaseTile(entry.Tile);
            if (resident.Owners==0)
            { _regions.Remove(entry.Region.PlannedBody.Id); resident.Region.Free(); }
        }
        Refresh();
    }
    // A pooled splash node belongs to each resident body, never each chunk or water tile.
    // =========================================================
    private void AttachRain(Resident resident)
    {
        resident.Rain=new WorldLiquidRainSplashes { Name="RainSplashes",Region=resident.Region,Config=_config,Weather=_weather,ZIndex=-4 };
        resident.Region.AddChild(resident.Rain);
    }
    #endregion

    #region Presentation lifetime
    // No resident water, disabled water, or settled static/dry water means no shared update callback.
    // =========================================================
    private void Refresh()
    {
        foreach (var resident in _regions.Values)
        {
            bool active=resident.Published>0 && _config.Enabled;
            resident.Region.Visible=active; resident.Region.PresentationActive=active;
            resident.Region.UpdateSurface(0,_weather?.WindStrength ?? 0);
            resident.Rain?.ApplyRainState();
        }
        SetProcess(TileCount>0 && _config.Enabled && _weather!=null && (_config.Animate || _weather.RainActive));
    }
    // Animate only visible resident bodies; a body owns one time/flow phase across every chunk section.
    // =========================================================
    public override void _Process(double delta)
    {
        Rect2 view=_weather.VisibleWorldRect().Grow(48);
        foreach (var resident in _regions.Values)
        {
            bool active=resident.Published>0 && _config.Enabled && VisibleIn(resident,view);
            if (resident.Region.PresentationActive!=active)
            {
                resident.Region.PresentationActive=active; resident.Region.Visible=active;
                resident.Rain?.ApplyRainState();
            }
            if (active) resident.Region.UpdateSurface(delta,_weather.WindStrength);
        }
    }
    // Long rivers can enclose dry land in their overall AABB; cull against indexed shoreline sections and loaded tiles.
    // =========================================================
    private static bool VisibleIn(Resident resident,Rect2 view)
    {
        if (!resident.Region.PlannedBody.Intersects(view,40)) return false;
        foreach (var tile in resident.Tiles) if (tile.TileBounds.Intersects(view)) return true;
        return false;
    }
    // Scene teardown unregisters events before external weather/config owners disappear.
    // =========================================================
    public override void _ExitTree()
    {
        if (_config!=null) _config.Changed-=Refresh;
        if (GodotObject.IsInstanceValid(_weather)) _weather.RainStateChanged-=Refresh;
        _chunks.Clear(); _regions.Clear(); _definitions.Clear(); TileCount=0;
    }
    #endregion
}
