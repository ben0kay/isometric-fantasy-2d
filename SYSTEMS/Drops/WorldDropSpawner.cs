// World-owned logical drops with chunk-local materialization and event-driven collection; offscreen drops are records only.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class WorldDropSpawner : Node2D
{
    public ItemCatalog Catalog { get; private set; }
    public string ScopeId { get; private set; }
    public int RecordCount => _records.Count;
    public int LiveCount => _reservedLive;
    public int MaterializedCount => _nodes.Count;
    public Func<Vector2,Vector2,bool> SegmentAllowed { get; set; }
    public Func<string,bool> SourceCompleted { get; set; }
    public float CollectionRadius => _settings.CollectionRadius;
    public Func<Vector2,bool> LandingAllowed { get; set; }
    private Rect2 _bounds;
    private DropScatterValues _settings;
    private PackedScene _scene;
    private int _reservedLive;
    private bool _collecting;
    private int _chunkSize,_activeReservations;
    private readonly Dictionary<ChunkCoord,HashSet<string>> _owners=new();
    private readonly Dictionary<ChunkCoord,Node2D> _activeRoots=new();
    private readonly Dictionary<string,WorldDropRecord> _records=new(StringComparer.Ordinal);
    private readonly Dictionary<string,string[]> _events=new(StringComparer.Ordinal);
    private readonly Dictionary<string,DroppedItem> _nodes=new(StringComparer.Ordinal);
    private readonly Dictionary<Vector2I,HashSet<string>> _buckets=new();
    private readonly CircleShape2D _circle=new() { Radius=5 };
    private readonly PhysicsShapeQueryParameters2D _shape=new() { CollisionMask=1,CollideWithAreas=false,CollideWithBodies=true };
    private readonly PhysicsRayQueryParameters2D _ray=new() { CollisionMask=1,CollideWithAreas=false,CollideWithBodies=true,HitFromInside=true };
    private readonly Godot.Collections.Array<Rid> _exclude=new();
    // No automatic WorldScene discovery or production activation; owner supplies scope, bounds and shared defaults.
    // =========================================================
    public void Initialize(ItemCatalog catalog,string scopeId,Rect2 bounds,DropScatterProfile scatter)
    {
        if (Catalog!=null || catalog==null || string.IsNullOrWhiteSpace(scopeId) || !bounds.Position.IsFinite() || !bounds.Size.IsFinite() || bounds.Size.X<=0 || bounds.Size.Y<=0)
            throw new InvalidOperationException("Invalid drop scope.");
        Catalog=catalog; ScopeId=scopeId; _bounds=bounds; _settings=scatter.Snapshot();
        _scene=GD.Load<PackedScene>("res://SYSTEMS/Drops/DroppedItem.tscn"); _shape.Shape=_circle; _shape.Exclude=_exclude; _ray.Exclude=_exclude;
        YSortEnabled=true; SetProcess(false); SetPhysicsProcess(false);
    }
    // Atomically reserve final quantities/landing/IDs before the source is removed or any fall animation begins.
    // =========================================================
    public bool TryReserve(string eventId,Vector2 origin,DropReward[] rewards,CollisionObject2D source=null,string sourceId="")
    {
        if (Catalog==null || !IsInsideTree() || !origin.IsFinite() || !_bounds.HasPoint(origin) || rewards==null || rewards.Length>16
            || eventId==null || !eventId.StartsWith(ScopeId+"/",StringComparison.Ordinal) || _events.ContainsKey(eventId) || _events.Count>=_settings.MaxRecords || (sourceId!="" && SourceCompleted?.Invoke(sourceId)==true)) return false;
        var staged=new List<WorldDropRecord>();
        _exclude.Clear(); if (GodotObject.IsInstanceValid(source)) _exclude.Add(source.GetRid());
        _shape.Exclude=_exclude; _ray.Exclude=_exclude;
        for(int index=0;index<rewards.Length;index++)
        {
            var reward=rewards[index];
            if (!Catalog.Contains(reward.Item) || reward.Quantity<0 || reward.Quantity>9999) return false;
            if (reward.Quantity==0) continue;
            // Small felling bursts show individual logs; large rewards coalesce to real item stack limits.
            int unit=reward.Quantity<=5 ? 1 : reward.Item.MaxStack;
            int pieces=(reward.Quantity+unit-1)/unit;
            if (pieces>_settings.MaxBurst-staged.Count) return false;
            int remaining=reward.Quantity;
            for(int piece=0;piece<pieces;piece++)
            {
                string id=eventId+"/reward/"+index+"/pickup/"+piece; ulong seed=DropRandom.Hash(id);
                if (!TryLanding(origin,ref seed,out Vector2 landing)) return false;
                float angle=reward.Item.AllowWorldRotation ? (DropRandom.Unit(ref seed)*2-1)*_settings.Rotation : 0;
                float start=reward.Item.AllowWorldRotation ? angle+(DropRandom.Unit(ref seed)*2-1)*_settings.Spin : 0;
                int quantity=Math.Min(unit,remaining); remaining-=quantity;
                staged.Add(new(id,eventId,reward.Item.Id,quantity,origin,landing,angle,start,SourceId:sourceId));
            }
        }
        if ((_chunkSize>0 ? _activeReservations : _reservedLive)+staged.Count>_settings.MaxNodes || _records.Count+staged.Count>_settings.MaxRecords) return false;
        var ids=new string[staged.Count];
        for(int i=0;i<staged.Count;i++) { var record=staged[i]; _records.Add(record.Id,record); IndexOwner(record); ids[i]=record.Id; }
        _events.Add(eventId,ids); _reservedLive+=staged.Count; if(_chunkSize>0) _activeReservations+=staged.Count; return true;
    }
    // Bounded deterministic landing trials reject water/bounds/solid obstruction rather than dropping into inaccessible scenery.
    // =========================================================
    private bool TryLanding(Vector2 origin,ref ulong seed,out Vector2 landing)
    {
        var space=GetWorld2D().DirectSpaceState;
        for(int attempt=0;attempt<12;attempt++)
        {
            float angle=DropRandom.Unit(ref seed)*Mathf.Tau;
            float radius=16+DropRandom.Unit(ref seed)*(_settings.Radius-16);
            landing=origin+Vector2.FromAngle(angle)*radius;
            if (!_bounds.Grow(-6).HasPoint(landing) || LandingAllowed?.Invoke(landing)==false || SegmentAllowed?.Invoke(origin,landing)==false) continue;
            _shape.Transform=new Transform2D(0,landing);
            if (space.IntersectShape(_shape,1).Count!=0) continue;
            _ray.From=origin; _ray.To=landing;
            if (space.IntersectRay(_ray).Count==0) return true;
        }
        landing=default; return false;
    }
    // Reserved quantities survive an interrupted fall within this world scope; release is idempotent.
    // =========================================================
    public void Release(string eventId)
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || !_events.TryGetValue(eventId,out var ids)) return;
        foreach(string id in ids)
        {
            if (!_records.TryGetValue(id,out var record) || record.Released || record.Collected) continue;
            record=record with { Released=true }; _records[id]=record;
            Materialize(record,false);
        }
    }
    // Explicit E request queries only nearby spatial buckets and one line-of-sight ray; settled items never poll.
    // =========================================================
    public bool TryCollectNearest(PlayerMovement player,InventoryStorage storage)
    {
        if (_collecting || !GodotObject.IsInstanceValid(player) || !player.IsInsideTree() || !player.HealthState.IsAlive || storage==null || storage.Catalog!=Catalog) return false;
        Vector2 position=player.GlobalPosition; float best=CollectionRadius*CollectionRadius; string nearest=null;
        Vector2I low=Cell(position-Vector2.One*CollectionRadius), high=Cell(position+Vector2.One*CollectionRadius);
        for(int y=low.Y;y<=high.Y;y++) for(int x=low.X;x<=high.X;x++)
        {
            if (!_buckets.TryGetValue(new(x,y),out var bucket)) continue;
            foreach(string id in bucket)
            {
                var node=_nodes[id]; var record=_records[id];
                if (!GodotObject.IsInstanceValid(node) || !node.Settled || record.Collected) continue;
                float distance=position.DistanceSquaredTo(record.Landing);
                if (distance>best || (distance==best && nearest!=null && string.CompareOrdinal(id,nearest)>=0)) continue;
                _exclude.Clear(); _exclude.Add(player.GetRid()); _ray.Exclude=_exclude; _ray.From=position; _ray.To=record.Landing;
                if (GetWorld2D().DirectSpaceState.IntersectRay(_ray).Count!=0) continue;
                best=distance; nearest=id;
            }
        }
        if (nearest==null) return false;
        var selected=_records[nearest]; _collecting=true;
        try
        {
            // Changed observers may capture a checkpoint inside TryAdd. Publish the matching drop outcome first;
            // failed adds emit no Changed event and roll this provisional mark back without consuming anything.
            _records[nearest]=selected with { Collected=true };
            if (!storage.TryAdd(Catalog.Get(selected.ItemId),selected.Quantity)) { _records[nearest]=selected; return false; }
            _reservedLive--;
            var cell=Cell(selected.Landing); _buckets[cell].Remove(nearest); if (_buckets[cell].Count==0) _buckets.Remove(cell);
            _nodes[nearest].QueueFree(); _nodes.Remove(nearest);
            if (_chunkSize>0)
            {
                _activeReservations--; var owner=ChunkCoord.At(selected.Landing,_chunkSize); _owners[owner].Remove(nearest); if (_owners[owner].Count==0) _owners.Remove(owner);
                _records.Remove(nearest); // Source felled tombstone, not collected-node history, prevents replay in production.
                if (_events.TryGetValue(selected.EventId,out var ids) && !ids.Any(id=>_records.ContainsKey(id))) _events.Remove(selected.EventId);
            }
            return true;
        }
        finally { _collecting=false; }
    }
    // Existing P3 owns when chunks load/unload. This owner only indexes/presents the records for those notifications.
    // =========================================================
    public void EnableStreaming(int chunkSize)
    { if (_records.Count!=0 || chunkSize<256 || chunkSize>2048) throw new InvalidOperationException("Drop streaming requires an empty initialized scope."); _chunkSize=chunkSize; }
    // Restore settled logical outcomes before any chunk is visible; unknown items fail without deleting quantities.
    // =========================================================
    public void Restore(SavedDrop[] drops)
    {
        if (_chunkSize==0 || _records.Count!=0 || drops==null || drops.Length>_settings.MaxRecords) throw new InvalidDataException("Invalid saved drop journal.");
        var staged=new List<WorldDropRecord>(); var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var drop in drops)
        {
            var item=drop==null ? null : Catalog.Get(drop.ItemId);
            string eventId=drop==null ? "" : ScopeId+"/"+drop.SourceId+"/fell/0";
            if (drop==null || item==null || drop.Quantity<1 || drop.Quantity>item.MaxStack || !seen.Add(drop.Id)
                || !float.IsFinite(drop.X) || !float.IsFinite(drop.Y) || !_bounds.HasPoint(new Vector2(drop.X,drop.Y))
                || !float.IsFinite(drop.Rotation) || !drop.Id.StartsWith(eventId+"/reward/",StringComparison.Ordinal))
                throw new InvalidDataException("Unknown item, identity or quantity in saved drop.");
            Vector2 point=new(drop.X,drop.Y);
            staged.Add(new(drop.Id,eventId,drop.ItemId,drop.Quantity,point,point,drop.Rotation,drop.Rotation,true,false,drop.SourceId));
        }
        foreach(var record in staged) { _records.Add(record.Id,record); IndexOwner(record); }
        foreach(var group in staged.GroupBy(d=>d.EventId)) _events.Add(group.Key,group.Select(d=>d.Id).ToArray());
        _reservedLive=staged.Count;
    }
    // Save the final landing even mid-fall/flight. The source's felled outcome and these records share one checkpoint.
    // =========================================================
    public SavedDrop[] CaptureSaved() => _records.Values.Where(d=>!d.Collected).OrderBy(d=>d.Id,StringComparer.Ordinal)
        .Select(d=>new SavedDrop(d.Id,d.SourceId,d.ItemId,d.Quantity,d.Landing.X,d.Landing.Y,d.LandingRotation)).ToArray();
    // Track only owners with actual records; no empty-world per-object bookkeeping.
    // =========================================================
    private void IndexOwner(WorldDropRecord record)
    {
        if (_chunkSize==0) return;
        var coord=ChunkCoord.At(record.Landing,_chunkSize);
        if (!_owners.TryGetValue(coord,out var ids)) { ids=new(StringComparer.Ordinal); _owners.Add(coord,ids); }
        ids.Add(record.Id);
    }
    // Revisit creates ordinary settled presentations. Existing records are never hidden/deleted to enforce a spawn budget.
    // =========================================================
    public void ActivateChunk(ChunkCoord coord)
    {
        if (_chunkSize==0 || _activeRoots.ContainsKey(coord)) return;
        var root=new Node2D { Name=$"Drops_{coord.X}_{coord.Y}",YSortEnabled=true,PhysicsInterpolationMode=PhysicsInterpolationModeEnum.Off };
        root.SetMeta("world_actor_container",true); AddChild(root); _activeRoots.Add(coord,root);
        if (_owners.TryGetValue(coord,out var ids)) foreach(string id in ids)
        { var record=_records[id]; if(!record.Collected) { _activeReservations++; if(record.Released) Materialize(record,true); } }
    }
    // Offscreen nodes are discarded; the final landing/quantity stays authoritative in the indexed logical record.
    // =========================================================
    public void DeactivateChunk(ChunkCoord coord)
    {
        if (!_activeRoots.Remove(coord,out var root)) return;
        if (_owners.TryGetValue(coord,out var ids)) foreach(string id in ids)
        {
            var record=_records[id]; if(!record.Collected) _activeReservations--;
            _records[id]=record with { Released=true };
            if (_nodes.Remove(id,out var node))
            { var cell=Cell(record.Landing); _buckets[cell].Remove(id); if(_buckets[cell].Count==0) _buckets.Remove(cell); node.Hide(); node.SetProcess(false); node.QueueFree(); }
        }
        root.QueueFree();
    }
    // Presentation is created only in a loaded owner chunk (or explicitly standalone fixture).
    // =========================================================
    private void Materialize(WorldDropRecord record,bool settled)
    {
        if (_nodes.ContainsKey(record.Id)) return;
        Node2D parent=this;
        if (_chunkSize>0 && !_activeRoots.TryGetValue(ChunkCoord.At(record.Landing,_chunkSize),out parent)) return;
        var node=_scene.Instantiate<DroppedItem>(); parent.AddChild(node); node.Configure(record,Catalog.Get(record.ItemId),_settings); _nodes.Add(record.Id,node);
        var cell=Cell(record.Landing); if(!_buckets.TryGetValue(cell,out var bucket)) { bucket=new(StringComparer.Ordinal); _buckets.Add(cell,bucket); }
        bucket.Add(record.Id); if(settled) node.RestoreSettled(); else node.Release();
    }
    // Capture detached logical outcomes for tests; campaign persistence uses CaptureSaved without rerolls.
    // =========================================================
    public WorldDropRecord[] Capture()
    { var records=new WorldDropRecord[_records.Count]; _records.Values.CopyTo(records,0); return records; }
    // Query a known presentation instance without scanning the scene or exposing mutable logical records.
    // =========================================================
    public DroppedItem FindNode(string id) => _nodes.TryGetValue(id,out var node) ? node : null;
    // Fixed floor-derived cells handle negative coordinates consistently.
    // =========================================================
    private static Vector2I Cell(Vector2 point) => new(Mathf.FloorToInt(point.X/64),Mathf.FloorToInt(point.Y/64));
    // Native query state belongs only to this scene scope; no global caches survive world teardown.
    // =========================================================
    public override void _ExitTree() { _shape.Dispose(); _ray.Dispose(); _circle.Dispose(); }
}
