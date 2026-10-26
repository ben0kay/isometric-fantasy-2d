// One authoritative polygon supplies rendering distance, placement exclusion and wading depth.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldLiquidRegion : Node2D
{
    [Export] public WorldLiquidMasterConfig Config { get; set; }
    [Export] public WorldLiquidDefinition Definition { get; set; }
    public PlannedWaterBody PlannedBody { get; set; }
    public bool PresentationActive { get; set; } = true;
    public bool Active => Config?.Enabled == true;
    private readonly List<LiquidSurfaceTile> _tiles = new();
    public Vector2[] Boundary { get; private set; }
    public Rect2 SurfaceBounds { get; private set; } // World-space rectangle enclosing the fixed lake and its bank.
    private Vector2[] _edgeDelta;
    private float[] _inverseLength;
    private ShaderMaterial _material;
    private MeshInstance2D _surface;
    private float _time;
    private Vector2 _windDrift;

    #region Startup geometry
    // Register before scenery generation; the parent test scene is placed before Actors.
    // =========================================================
    public override void _EnterTree() => AddToGroup("world_liquid_regions");

    // Moderate harmonic perimeter perturbation produces a smooth, stable non-self-crossing lake.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WorldLiquidMasterConfig(); Definition ??= new WorldLiquidDefinition();
        Position = PlannedBody == null ? Config.Centre : Vector2.Zero;
        Boundary = PlannedBody?.CopyPolygon() ?? WorldLiquidFootprint.Boundary(Config);
        _edgeDelta = new Vector2[Boundary.Length]; _inverseLength = new float[Boundary.Length];
        for (int i = 0; i < Boundary.Length; i++)
        {
            _edgeDelta[i] = Boundary[(i + 1) % Boundary.Length] - Boundary[i];
            _inverseLength[i] = 1f / Mathf.Max(0.001f, _edgeDelta[i].LengthSquared());
        }
        Rect2 bounds = WorldLiquidFootprint.LocalBounds(Boundary);
        // Rain-impact candidates can be sampled in this small region instead of the full world.
        SurfaceBounds = new Rect2(ToGlobal(bounds.Position), ToGlobal(bounds.End) - ToGlobal(bounds.Position)).Abs();
        if (PlannedBody != null) { SetProcess(false); return; }
        Texture2D field = BuildDistanceField(bounds);
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Liquids/Surfaces/Shaders/LiquidSurface.gdshader") };
        _material.SetShaderParameter("distance_field", field);
        _material.SetShaderParameter("surface_noise", GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres"));
        _surface = new MeshInstance2D { Name = "Surface", Position = bounds.GetCenter(), Mesh = new QuadMesh { Size = bounds.Size }, Material = _material, ZIndex = -6 };
        AddChild(_surface);
    }

    // Rasterize only once: shaders sample distance directly, never loop over shoreline segments.
    // =========================================================
    private Texture2D BuildDistanceField(Rect2 bounds)
    {
        const int width = 384, height = 256;
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rf);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            Vector2 p = bounds.Position + new Vector2((x + 0.5f) / width, (y + 0.5f) / height) * bounds.Size;
            float distance = SignedLocalDistance(p);
            image.SetPixel(x, y, new Color(Mathf.Clamp(0.5f + distance / 512f, 0f, 1f), 0, 0));
        }
        return ImageTexture.CreateFromImage(image);
    }
    #endregion

    #region Shared footprint queries
    // Positive distances are underwater; negative distances lie outside the authoritative polygon.
    // =========================================================
    public float SignedDistance(Vector2 world) => Boundary == null ? -100000f : SignedLocalDistance(ToLocal(world));

    // Margin reserves ground outside the water too, for tree roots, rock gravel and shoreline grass.
    // =========================================================
    public bool Contains(Vector2 world, float margin = 0f) => world.IsFinite() && SurfaceBounds.Grow(Mathf.Max(0f,margin)).HasPoint(world) && SignedDistance(world) >= -Mathf.Max(0f, margin);

    // Ray crossings and nearest edge distance share one bounded loop with no query allocations.
    // =========================================================
    private float SignedLocalDistance(Vector2 p)
    {
        bool inside = false;
        float minimum = float.MaxValue;
        for (int i = 0; i < Boundary.Length; i++)
        {
            Vector2 a = Boundary[i], b = Boundary[(i + 1) % Boundary.Length], delta = _edgeDelta[i];
            float t = Mathf.Clamp((p - a).Dot(delta) * _inverseLength[i], 0f, 1f);
            minimum = Mathf.Min(minimum, p.DistanceSquaredTo(a + delta * t));
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((double)b.X - a.X) * (p.Y - a.Y) / ((double)b.Y - a.Y) + a.X) inside = !inside;
        }
        return Mathf.Sqrt(minimum) * (inside ? 1f : -1f);
    }
    #endregion

    #region Presentation
    // Read shared weather without modifying it; geometry and masks never rebuild each frame.
    // =========================================================
    public void UpdateSurface(double delta, float wind)
    {
        Visible = Active;
        if (Config.Animate)
        {
            _time += (float)delta * Definition.WaveSpeed;
            _windDrift.X += wind * (float)delta * 8f;
        }
        if (_material != null) ApplySurface(_material,wind);
        foreach (var tile in _tiles) ApplySurface((ShaderMaterial)tile.Material,wind);
    }
    // Static settings write only on changes; active animation advances one clock per body, not per tile.
    // =========================================================
    private readonly Dictionary<ShaderMaterial,Dictionary<string,Variant>> _uniforms = new();
    private void ApplySurface(ShaderMaterial material,float wind)
    {
        if (!_uniforms.TryGetValue(material,out var values)) _uniforms.Add(material,values=new());
        void Set(string key,Variant value)
        {
            if (values.TryGetValue(key,out var previous) && previous.Equals(value)) return;
            values[key]=value; material.SetShaderParameter(key,value);
        }
        Set("wind_drift",_windDrift); Set("surface_time",_time); Set("wind_strength",wind);
        Set("ripples",Config.RippleStrength); Set("shore_width",Config.Shoreline ? Mathf.Clamp(Config.ShoreWidth,0,28) : 0f);
        Set("shallow_colour",Definition.ShallowColour); Set("deep_colour",Definition.DeepColour);
        Set("highlight_colour",Definition.HighlightColour); Set("bank_colour",Definition.BankColour);
        Set("depth_distance",Mathf.Max(10,Definition.DepthDistance));
    }
    // World-aligned 4-unit texels plus a halo make neighboring tile samples identical at their seam.
    // Yield after eight rows so the existing streamer can budget CPU field work before publication.
    // =========================================================
    public IEnumerable<int> PrepareTile(Rect2 bounds,Action<LiquidSurfaceTile> complete)
    {
        Vector2 origin=(bounds.Position/4f).Floor()*4f-Vector2.One*4f;
        Vector2 end=(bounds.End/4f).Ceil()*4f+Vector2.One*4f;
        var fieldBounds=new Rect2(origin,end-origin);
        int width=Mathf.RoundToInt(fieldBounds.Size.X/4),height=Mathf.RoundToInt(fieldBounds.Size.Y/4);
        using var image=Image.CreateEmpty(width,height,false,Image.Format.Rf);
        // Dense banks do not require scanning the entire river for each texel. Distances saturate at 256 units.
        var edges=new List<int>(); var crossings=new List<float>(Boundary.Length);
        for (int i=0;i<Boundary.Length;i++)
            if (new Rect2(Boundary[i],_edgeDelta[i]).Abs().Grow(256f).Intersects(fieldBounds,true)) edges.Add(i);
        for (int y=0;y<height;y++)
        {
            float py=origin.Y+(y+0.5f)*4f; crossings.Clear();
            for (int i=0;i<Boundary.Length;i++)
            {
                Vector2 a=Boundary[i],b=Boundary[(i+1)%Boundary.Length];
                if ((a.Y>py)!=(b.Y>py)) crossings.Add(a.X+(b.X-a.X)*(py-a.Y)/(b.Y-a.Y));
            }
            crossings.Sort(); int crossing=0;
            for (int x=0;x<width;x++)
            {
                Vector2 p=new(origin.X+(x+0.5f)*4f,py);
                while (crossing<crossings.Count && crossings[crossing]<p.X) crossing++;
                float minimum=256f*256f;
                foreach (int i in edges)
                {
                    Vector2 a=Boundary[i],delta=_edgeDelta[i];
                    float t=Mathf.Clamp((p-a).Dot(delta)*_inverseLength[i],0,1);
                    minimum=Mathf.Min(minimum,p.DistanceSquaredTo(a+delta*t));
                }
                float distance=Mathf.Sqrt(minimum)*((crossing&1)==1 ? 1f : -1f);
                image.SetPixel(x,y,new Color(Mathf.Clamp(0.5f+distance/512f,0,1),0,0));
            }
            if (y%8==7) yield return 0;
        }
        var field=ImageTexture.CreateFromImage(image);
        var material=new ShaderMaterial { Shader=GD.Load<Shader>("res://WORLD/Liquids/Surfaces/Shaders/LiquidSurface.gdshader") };
        material.SetShaderParameter("distance_field",field);
        material.SetShaderParameter("world_distance_field",true);
        material.SetShaderParameter("field_origin",origin); material.SetShaderParameter("field_size",fieldBounds.Size);
        material.SetShaderParameter("surface_noise",GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres"));
        var tile=new LiquidSurfaceTile { Position=bounds.GetCenter(),Mesh=new QuadMesh { Size=bounds.Size },Material=material,
            Field=field,FieldBounds=fieldBounds,TileBounds=bounds,Visible=false,ZIndex=-6,PhysicsInterpolationMode=PhysicsInterpolationModeEnum.Off };
        _tiles.Add(tile); AddChild(tile); ApplySurface(material,0); complete(tile);
    }
    // Release all tile-owned GPU resources and uniform caches at chunk unload.
    // =========================================================
    public void ReleaseTile(LiquidSurfaceTile tile)
    {
        _tiles.Remove(tile); _uniforms.Remove((ShaderMaterial)tile.Material); tile.Free();
    }
    #endregion
}
