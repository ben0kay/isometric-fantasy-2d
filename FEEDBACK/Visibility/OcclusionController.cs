// Shared spatial occlusion: nearby detection and a small active-fade set, without per-prop ticking.
using Godot;
using System.Collections.Generic;
using FantasyGame.World.Trees;
public partial class OcclusionController : Node
{
    [Export(PropertyHint.Range,"0.03,0.5,0.01")] public float CheckIntervalSeconds { get; set; }=0.10f;
    [Export] public Vector2 PlayerVisibilityOffset { get; set; }=new(0,-16);
    private Node2D _player;
    private float _untilCheck;
    private bool _dirty=true, _hasPlayer;
    private Vector2 _lastFeet,_lastOffset;
    private readonly Dictionary<Sprite2D,Entry> _registered=new();
    private readonly SpatialBoundsIndex<Sprite2D> _index=new(256);
    private readonly HashSet<Sprite2D> _candidates=new();
    private readonly HashSet<Entry> _obstructing=new(), _fading=new(), _dynamic=new();
    private readonly List<Entry> _finished=new();
    public int RegisteredCount => _registered.Count;
    public int ActiveFadeCount => _fading.Count;
    public long DetectionPasses { get; private set; }
    public long CandidateChecks { get; private set; }
    public long FadeWrites { get; private set; }
    private sealed class Entry
    {
        public Sprite2D Visual; public Node2D Owner; public OcclusionProfile Profile; public TreeHitResponse Hits;
        public Rect2 Bounds; public Transform2D OwnerTransform,VisualTransform;
        public float BaseAlpha,CurrentAlpha; public bool Obstructing;
        public System.Action Changed,Gone;
    }
    // Startup group discovery runs once; streamed props use explicit register/unregister.
    // =========================================================
    public void Initialize(Node2D player)
    {
        _player=player; _dirty=true;
        foreach(Node node in GetTree().GetNodesInGroup("world_trees"))
            if(node is FantasyGame.World.Trees.Tree tree && tree.Definition?.VisibilityProfile is { Enabled:true } profile)
                Register(tree,tree.GetNodeOrNull<Sprite2D>("Sprite"),profile);
        SetProcess(_registered.Count>0);
    }
    // Other props reuse this registration contract; bounds include offsets/mirroring/scale/padding.
    // =========================================================
    public void Register(Node2D owner,Sprite2D visual,OcclusionProfile profile)
    {
        if(owner==null || visual==null || profile==null || !profile.Enabled || _registered.ContainsKey(visual)) return;
        var entry=new Entry { Visual=visual,Owner=owner,Profile=profile,BaseAlpha=visual.Modulate.A,CurrentAlpha=visual.Modulate.A };
        entry.Changed=()=>Refresh(visual); entry.Gone=()=>Unregister(visual);
        _registered.Add(visual,entry);
        visual.VisibilityChanged+=entry.Changed; visual.ItemRectChanged+=entry.Changed;
        visual.TreeExiting+=entry.Gone; owner.TreeExiting+=entry.Gone; profile.Changed+=entry.Changed;
        entry.Hits=owner.GetNodeOrNull<TreeHitResponse>("HitResponse");
        if(entry.Hits!=null) entry.Hits.VisualTransformChanged+=entry.Changed;
        if(owner is not StaticBody2D) _dynamic.Add(entry);
        Refresh(visual); SetProcess(true);
    }
    // Explicit static transform/appearance edits and active wobble refresh just one entry.
    // =========================================================
    public void Refresh(Sprite2D visual)
    {
        if(!_registered.TryGetValue(visual,out var entry)) return;
        if(!GodotObject.IsInstanceValid(visual) || !GodotObject.IsInstanceValid(entry.Owner)) { Unregister(visual); return; }
        entry.Bounds=SceneryVisualBounds.Sprite(visual).Grow(entry.Profile.VisualPadding);
        entry.OwnerTransform=entry.Owner.GlobalTransform; entry.VisualTransform=visual.Transform;
        _index.Set(visual,entry.Bounds); _dirty=true;
    }
    public void Unregister(Sprite2D visual)
    {
        if(visual==null || !_registered.Remove(visual,out var entry)) return;
        _index.Remove(visual); _obstructing.Remove(entry); _fading.Remove(entry); _dynamic.Remove(entry);
        entry.Profile.Changed-=entry.Changed;
        if(GodotObject.IsInstanceValid(visual))
        {
            visual.VisibilityChanged-=entry.Changed; visual.ItemRectChanged-=entry.Changed; visual.TreeExiting-=entry.Gone;
            Color color=visual.Modulate; if(color.A!=entry.BaseAlpha) { color.A=entry.BaseAlpha; visual.Modulate=color; }
        }
        if(GodotObject.IsInstanceValid(entry.Owner)) entry.Owner.TreeExiting-=entry.Gone;
        if(GodotObject.IsInstanceValid(entry.Hits)) entry.Hits.VisualTransformChanged-=entry.Changed;
        _dirty=true; if(_registered.Count==0) { _candidates.Clear(); _finished.Clear(); SetProcess(false); }
    }
    // A shared position probe remains; only movement/source changes query nearby candidates.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace=RuntimeFrameTrace.Measure("OcclusionController.Process");
        if(!GodotObject.IsInstanceValid(_player)) return;
        float dt=(float)delta; _untilCheck-=dt;
        Vector2 feet=_player.GlobalPosition;
        bool teleport=_hasPlayer && feet.DistanceSquaredTo(_lastFeet)>256*256;
        if(_untilCheck<=0 || _dirty || teleport)
        {
            _untilCheck=Mathf.Max(0.03f,CheckIntervalSeconds);
            foreach(var entry in _dynamic)
                if(entry.Owner.GlobalTransform!=entry.OwnerTransform || entry.Visual.Transform!=entry.VisualTransform) Refresh(entry.Visual);
            if(_dirty || !_hasPlayer || feet!=_lastFeet || PlayerVisibilityOffset!=_lastOffset)
            {
                DetectionPasses++; _candidates.Clear();
                Vector2 point=feet+PlayerVisibilityOffset;
                _index.Query(new Rect2(point-new Vector2(0.001f,0.001f),new Vector2(0.002f,0.002f)),_candidates);
                foreach(var entry in _obstructing) _candidates.Add(entry.Visual);
                foreach(var sprite in _candidates)
                {
                    var entry=_registered[sprite]; CandidateChecks++;
                    bool obstructing=entry.Profile.Enabled && sprite.IsVisibleInTree() && sprite.Texture!=null
                        && feet.Y<=entry.Owner.GlobalPosition.Y+entry.Profile.BehindAnchorTolerance
                        && point.X>=entry.Bounds.Position.X && point.X<=entry.Bounds.End.X
                        && point.Y>=entry.Bounds.Position.Y && point.Y<=entry.Bounds.End.Y;
                    entry.Obstructing=obstructing;
                    if(obstructing) _obstructing.Add(entry); else _obstructing.Remove(entry);
                    float target=Target(entry);
                    if(!Mathf.IsEqualApprox(entry.CurrentAlpha,target)) _fading.Add(entry);
                }
                _dirty=false; _hasPlayer=true; _lastFeet=feet; _lastOffset=PlayerVisibilityOffset;
            }
        }
        _finished.Clear();
        foreach(var entry in _fading)
        {
            float target=Target(entry),duration=target<entry.CurrentAlpha?entry.Profile.FadeOutSeconds:entry.Profile.FadeInSeconds;
            entry.CurrentAlpha=Mathf.MoveToward(entry.CurrentAlpha,target,dt/Mathf.Max(0.05f,duration)*entry.BaseAlpha);
            if(Mathf.IsEqualApprox(entry.CurrentAlpha,target)) { entry.CurrentAlpha=target; _finished.Add(entry); }
            Color color=entry.Visual.Modulate;
            if(color.A!=entry.CurrentAlpha) { color.A=entry.CurrentAlpha; entry.Visual.Modulate=color; FadeWrites++; }
        }
        foreach(var entry in _finished) _fading.Remove(entry);
    }
    private static float Target(Entry entry) => entry.Obstructing?entry.BaseAlpha*Mathf.Clamp(entry.Profile.MinimumAlpha,0.05f,1):entry.BaseAlpha;
    // Exact alpha restoration and signal/index cleanup on unload/world replacement.
    // =========================================================
    public override void _ExitTree()
    {
        foreach(var sprite in new List<Sprite2D>(_registered.Keys)) Unregister(sprite);
        _index.Clear(); _candidates.Clear(); _finished.Clear();
    }
}
