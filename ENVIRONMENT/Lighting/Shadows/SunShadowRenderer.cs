// Cached projected sun silhouettes copied from the debug renderer, independent of clock and controls.
using Godot;
using System.Collections.Generic;
public partial class SunShadowRenderer : Node2D
{
    private sealed class Caster
    {
        public Node2D Body; public Sprite2D Source, Shadow; public Polygon2D Contact;
        public Vector2 Origin; public float Reach, Opacity = float.NaN;
        public bool Static, Culled, Dirty = true, Initialized, Shown, ContactShown;
        public System.Action Invalidate; public TreeHitResponse Hits;
    }
    private readonly List<Caster> _casters = new(), _moving = new();
    private readonly List<SunShadowRenderer> _chunks = new();
    private bool _dirty = true, _initialized, _lastCull;
    private Rect2 _lastView;
    private Vector2 _appliedProjection;
    private float _maxReach = 1, _lastOpacity = float.NaN, _lastWind = float.NaN, _lastSoftness = float.NaN;
    private int _staticRelevant;
    private long _writes, _visits, _materialWrites;
    public long ProjectionWrites { get { long n=_writes; foreach(var chunk in _chunks) n+=chunk.ProjectionWrites; return n; } }
    public long StaticCasterVisits { get { long n=_visits; foreach(var chunk in _chunks) n+=chunk.StaticCasterVisits; return n; } }
    public long MaterialWrites { get { long n=_materialWrites; foreach(var chunk in _chunks) n+=chunk.MaterialWrites; return n; } }

    // Cache renderer children once, preserving streamed registration without per-frame GetChildren arrays.
    // =========================================================
    public override void _Ready()
    {
        ChildEnteredTree += RegisterRenderer; ChildExitingTree += UnregisterRenderer;
        foreach(Node child in GetChildren()) RegisterRenderer(child);
    }
    private void RegisterRenderer(Node node) { if(node is SunShadowRenderer chunk) _chunks.Add(chunk); }
    private void UnregisterRenderer(Node node) { if(node is SunShadowRenderer chunk) _chunks.Remove(chunk); }

    private readonly List<ShaderMaterial> _windMaterials = new();
    private readonly HashSet<ShaderMaterial> _shadowMaterials = new();
    public WindController Wind { get; set; }
    public bool CullOffscreen { get; set; } = true;
    public float PrepareMargin { get; set; } = 160f;
    public int UpdatedCasterCount { get; private set; }
    public int TotalCasterCount { get; private set; }
    public int LastFrameProjectionWrites { get; private set; }
    public double LastProjectionMilliseconds { get; private set; }
    // Capture the fully generated production actors once; grass opts out through content metadata.
    // =========================================================
    public void Attach(Node2D actors)
    {
        Node2D shadows = this;
        Shader flatShader = GD.Load<Shader>("res://ENVIRONMENT/Lighting/Shadows/Silhouette.gdshader");
        var flatMaterial = new ShaderMaterial { Shader = flatShader };
        var sharedWindShadows = new Dictionary<Material, ShaderMaterial>();
        foreach (Node child in WorldActorSources.Initial(actors))
        {
            if (child is not Node2D body || (body.HasMeta("environment_shadows") && !body.GetMeta("environment_shadows").AsBool())) continue;
            Sprite2D source = body.GetNodeOrNull<Sprite2D>("Sprite");
            if (source == null) continue;
            Material shadowMaterial = flatMaterial;
            if (source.Material is ShaderMaterial original)
            {
                ShaderMaterial litWind = original;
                if (!sharedWindShadows.TryGetValue(original, out ShaderMaterial windShadow))
                {
                    windShadow = (ShaderMaterial)litWind.Duplicate();
                    windShadow.SetShaderParameter("shadow_only", true);
                    sharedWindShadows.Add(original, windShadow);
                    _windMaterials.Add(windShadow);
                }
                shadowMaterial = windShadow;
            }
            if (shadowMaterial is ShaderMaterial cachedShadow) _shadowMaterials.Add(cachedShadow);
            var shadow = new Sprite2D
            {
                Name = body.Name + "SunShadow", Texture = source.Texture,
                Centered = source.Centered, Offset = source.Offset,
                FlipH = source.FlipH, FlipV = source.FlipV,
                TextureFilter = source.TextureFilter, Material = shadowMaterial,
                Visible = false, PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off
            };
            shadows.AddChild(shadow);

            var caster = new Caster { Body = body, Source = source, Shadow = shadow, Static = body is StaticBody2D }; CacheExtent(caster); _maxReach=Mathf.Max(_maxReach,caster.Reach); _casters.Add(caster);
            if (!caster.Static) _moving.Add(caster);
            caster.Invalidate = () => { caster.Dirty = true; _dirty = true; };
            source.VisibilityChanged += caster.Invalidate; source.ItemRectChanged += caster.Invalidate;
            source.TreeExiting += caster.Invalidate; body.TreeExiting += caster.Invalidate;
            caster.Hits = body.GetNodeOrNull<TreeHitResponse>("HitResponse");
            if(caster.Hits != null) caster.Hits.VisualTransformChanged += caster.Invalidate;
            _dirty = true;
            // The player already has a contact shadow; trees and rocks get matching grounding.
            if (body.GetNodeOrNull<Polygon2D>("Shadow") == null)
                caster.Contact = AddContactShadow(body, shadows);
        }
    }
    // Refresh one changed sprite without rebuilding shared shadow materials or unrelated casters.
    // =========================================================
    public void RefreshAppearance(Node2D body)
    {
        foreach (var chunk in _chunks) chunk.RefreshAppearance(body);
        foreach (Caster caster in _casters)
        {
            if (caster.Body != body) continue;
            caster.Shadow.Texture = caster.Source.Texture; caster.Shadow.Offset = caster.Source.Offset;
            caster.Shadow.Centered = caster.Source.Centered;
            caster.Shadow.FlipH = caster.Source.FlipH; caster.Shadow.FlipV = caster.Source.FlipV;
            CacheExtent(caster); _maxReach=Mathf.Max(_maxReach,caster.Reach); caster.Invalidate();
            return;
        }
    }
    // Add a low-cost, stationary oval under a static tree or rock.
    // =========================================================
    private Polygon2D AddContactShadow(Node2D body, Node2D shadows)
    {
        var points = new Vector2[16];
        // Root scaling is used for variations, so also scale the generated contact oval.
        Vector2 radius = body.HasMeta("environment_contact") ? body.GetMeta("environment_contact").AsVector2() * body.Scale.Abs() : Vector2.Zero;
        if (radius == Vector2.Zero) return null;
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            points[i] = new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        var contact = new Polygon2D
        {
            Name = body.Name + "Contact", Position = ToLocal(body.GlobalPosition) + new Vector2(0, -3),
            PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off, Visible = false,
            Polygon = points, Color = new Color(0.055f, 0.075f, 0.06f, 0.23f)
        };
        shadows.AddChild(contact); return contact;
    }

    // Use the original flat-ground projection and preserve actual root/sprite transforms.
    // =========================================================
    private static void CacheExtent(Caster caster)
    {
        caster.Origin = caster.Body.GlobalPosition;
        Rect2 artwork = SceneryVisualBounds.Sprite(caster.Source);
        caster.Reach = (artwork.Position-caster.Origin).Abs().Max((artwork.End-caster.Origin).Abs()).Length() + 64;
    }
    // Share one view rectangle across all chunk renderers; static caster broad bounds stay cached.
    // =========================================================
    public void UpdateProjection(float angle, float length, float opacity, float softness, float tolerancePixels = 0, bool force = false)
    {
        Transform2D canvas=GetViewport().GetCanvasTransform();
        Rect2 view=SceneryVisualBounds.Transform(GetViewportRect(),canvas.AffineInverse());
        // Frobenius norm conservatively bounds screen error even with rotated/nonuniform views.
        float pixelsPerWorld=Mathf.Sqrt(canvas.X.LengthSquared()+canvas.Y.LengthSquared());
        UpdateVisibleProjection(-Vector2.FromAngle(angle)*Mathf.Max(0.01f,length),length,opacity,softness,
            view.Grow(PrepareMargin),CullOffscreen,Mathf.Max(0,tolerancePixels)/Mathf.Max(0.001f,pixelsPerWorld),force);
    }
    private void UpdateVisibleProjection(Vector2 requested, float length, float opacity, float softness, Rect2 view, bool cull, float tolerance, bool force)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        long previousWrites=_writes; LastFrameProjectionWrites=0;
        UpdatedCasterCount = 0; TotalCasterCount = _casters.Count;
        foreach (var chunk in _chunks)
        {
            chunk.UpdateVisibleProjection(requested,length,opacity,softness,view,cull,tolerance,force);
            UpdatedCasterCount += chunk.UpdatedCasterCount; TotalCasterCount += chunk.TotalCasterCount;
            LastFrameProjectionWrites+=chunk.LastFrameProjectionWrites;
        }
        bool geometry = !_initialized || _dirty || force || (requested-_appliedProjection).Length()*_maxReach > tolerance;
        if (geometry) _appliedProjection=requested;
        bool refresh = geometry || _dirty || view != _lastView || cull != _lastCull || opacity != _lastOpacity;
        var projection = new Transform2D(Vector2.Right,_appliedProjection,Vector2.Zero);
        int dynamicRelevant = 0;
        if (refresh)
        {
            _staticRelevant=0;
            for(int i=_casters.Count-1;i>=0;i--)
            {
                var caster=_casters[i]; if(caster.Static) _visits++;
                if(!GodotObject.IsInstanceValid(caster.Body) || !GodotObject.IsInstanceValid(caster.Source))
                {
                    Disconnect(caster); caster.Shadow.QueueFree();
                    if(GodotObject.IsInstanceValid(caster.Contact)) caster.Contact.QueueFree();
                    _casters.RemoveAt(i); _moving.Remove(caster); continue;
                }
                bool relevant=UpdateCaster(caster,projection,requested,Mathf.Max(Mathf.Abs(length),_appliedProjection.Length()),opacity,view,cull,geometry);
                if(relevant) { if(caster.Static) _staticRelevant++; else dynamicRelevant++; }
            }
        }
        else foreach(var caster in _moving)
            if(UpdateCaster(caster,projection,requested,Mathf.Max(Mathf.Abs(length),_appliedProjection.Length()),opacity,view,cull,false)) dynamicRelevant++;
        UpdatedCasterCount += _staticRelevant+dynamicRelevant;
        if(_staticRelevant+dynamicRelevant > 0 && opacity > 0.001f)
        {
            if(_lastWind != Wind.Strength)
            { _lastWind=Wind.Strength; foreach(var material in _windMaterials) { material.SetShaderParameter("wind_multiplier",_lastWind); _materialWrites++; } }
            if(_lastSoftness != softness)
            { _lastSoftness=softness; foreach(var material in _shadowMaterials) { material.SetShaderParameter("softness_uv",softness); _materialWrites++; } }
        }
        _initialized=true; _dirty=false; _lastView=view; _lastCull=cull; _lastOpacity=opacity;
        LastFrameProjectionWrites+=(int)(_writes-previousWrites);
        LastProjectionMilliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }
    // Static silhouettes reuse geometry; moving actors and hit wobble remain independently responsive.
    // =========================================================
    private bool UpdateCaster(Caster caster, Transform2D projection, Vector2 requested, float length, float opacity, Rect2 view, bool cull, bool geometry)
    {
        if(!caster.Static || caster.Dirty) { CacheExtent(caster); _maxReach=Mathf.Max(_maxReach,caster.Reach); }
        float reach=caster.Reach*(1f+Mathf.Abs(length));
        bool relevant=!cull || view.Intersects(new Rect2(caster.Origin-Vector2.One*reach,Vector2.One*reach*2));
        if(!relevant)
        {
            if(caster.Shown) { caster.Shadow.Visible=false; caster.Shown=false; }
            if(caster.ContactShown) { caster.Contact.Visible=false; caster.ContactShown=false; }
            caster.Culled=true; return false;
        }
        bool visible=caster.Source.IsVisibleInTree();
        if(caster.Contact != null && caster.ContactShown != visible) { caster.Contact.Visible=visible; caster.ContactShown=visible; }
        if(geometry || caster.Dirty || caster.Culled || !caster.Initialized || !caster.Static)
        {
            Transform2D basis=caster.Body.GlobalTransform; basis.Origin=Vector2.Zero;
            if(!caster.Static) projection=new Transform2D(Vector2.Right,requested,Vector2.Zero);
            Transform2D transform=new Transform2D(0f,caster.Body.GlobalPosition)*projection*basis*caster.Source.Transform;
            if(!caster.Initialized || caster.Shadow.GlobalTransform != transform) { caster.Shadow.GlobalTransform=transform; _writes++; }
            caster.Initialized=true;
        }
        if(caster.Opacity != opacity) { caster.Shadow.Modulate=new Color(1,1,1,opacity); caster.Opacity=opacity; }
        bool show=opacity>0.001f && visible;
        if(caster.Shown != show) { caster.Shadow.Visible=show; caster.Shown=show; }
        caster.Culled=false; caster.Dirty=false; return true;
    }
    // Removed chunk adapters release source signals rather than retaining old forests.
    // =========================================================
    private static void Disconnect(Caster caster)
    {
        if(GodotObject.IsInstanceValid(caster.Source))
        { caster.Source.VisibilityChanged-=caster.Invalidate; caster.Source.ItemRectChanged-=caster.Invalidate; caster.Source.TreeExiting-=caster.Invalidate; }
        if(GodotObject.IsInstanceValid(caster.Body)) caster.Body.TreeExiting-=caster.Invalidate;
        if(GodotObject.IsInstanceValid(caster.Hits)) caster.Hits.VisualTransformChanged-=caster.Invalidate;
    }
    public override void _ExitTree()
    {
        ChildEnteredTree-=RegisterRenderer; ChildExitingTree-=UnregisterRenderer;
        foreach(var caster in _casters) Disconnect(caster);
        _casters.Clear(); _moving.Clear(); _chunks.Clear();
    }
}
