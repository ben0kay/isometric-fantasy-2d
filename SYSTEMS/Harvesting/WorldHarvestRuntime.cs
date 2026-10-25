// Real World composition for harvesting, chunk-owned drops and persistent player inventory. No idle frame loop.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GameTree=FantasyGame.World.Trees.Tree;
public partial class WorldHarvestRuntime : Node
{
    public PlayerInventory Inventory { get; private set; }
    public WorldDropSpawner Drops { get; private set; }
    private WorldScene _world;
    private readonly HashSet<string> _previewFelled=new(StringComparer.Ordinal);
    // Create authoritative owners before chunk materialization so every configured tree uses the same contracts.
    // =========================================================
    public void Initialize(WorldScene world,CampaignSnapshot restored)
    {
        _world=world; Inventory=new PlayerInventory { Name="Inventory" }; world.Player.AddChild(Inventory); Inventory.Initialize(world.Player,restored?.Player.Inventory);
        Drops=new WorldDropSpawner { Name="WorldDrops" }; Drops.SetMeta("world_actor_container",true); world.ActorsRoot.AddChild(Drops);
        Drops.Initialize(Inventory.Storage.Catalog,world.Blueprint?.WorldId ?? "preview",world.Config.Bounds,GD.Load<DropScatterProfile>("res://SYSTEMS/Drops/DefaultDropScatter.tres"));
        bool streaming=world.Config.Streaming?.Enabled==true;
        if(streaming)
        {
            Drops.EnableStreaming(world.Config.Streaming.ChunkSize);
            Drops.Restore(restored?.Chunks.SelectMany(c=>c.Drops ?? Array.Empty<SavedDrop>()).ToArray() ?? Array.Empty<SavedDrop>());
        }
        Drops.LandingAllowed=point=>(world.Stream==null || world.Stream.IsPositionLoaded(point))
            && !(world.SceneryPlacement.ExcludedLiquid?.Contains(point,6) ?? false) && !(world.GeneratedWater?.Contains(point,6) ?? false);
        Drops.SegmentAllowed=(from,to)=>world.Stream==null || world.Stream.IsSegmentLoaded(from,to);
        Drops.SourceCompleted=id=>world.Stream?.IsFelled(id) ?? _previewFelled.Contains(id);
        var pickup=new PlayerPickupInteraction { Name="PickupInteraction" }; AddChild(pickup); pickup.Initialize(world.Player,Inventory.Storage,Drops);
        var hud=world.GetNodeOrNull<GameplayHUD>("GameplayUIRoot"); hud?.BindInventory(Inventory.Storage,Inventory.Hotbar);
        hud?.SetInteractionHint("1–9 select · E pickup · I backpack · G shield");
        SetProcess(false); SetPhysicsProcess(false);
    }
    // Called once for each generated tree; only changed trees retain progress in P3's existing delta owner.
    // =========================================================
    public void AttachTree(GameTree tree,ChunkCoord coord,string id,float progress=0)
    {
        if(tree.Definition.Harvesting==null) return;
        var state=tree.EnableHarvesting(Drops,id,()=>
        {
            if(_world.Stream!=null) _world.Stream.RecordFelled(id,coord); else _previewFelled.Add(id);
        });
        state.RestoreProgress(progress);
        state.Changed+=()=> { if(!state.Completed && _world.Stream!=null) _world.Stream.RecordHarvest(id,coord,state.Progress); };
    }
    // Non-streaming F6 preview has no campaign saves, but still plays the same real inventory/harvesting loop.
    // =========================================================
    public void BindPreview(Node root)
    {
        int index=0;
        foreach(var tree in root.GetChildren().OfType<GameTree>())
        { string id="preview/"+index++; tree.SetMeta("world_instance_id",id); AttachTree(tree,default,id); }
    }
    // Drops join the same checkpoint as source edits and inventory, even if their visual fall/flight is unfinished.
    // =========================================================
    public SavedChunk[] MergeDrops(SavedChunk[] source)
    {
        var chunks=source.ToDictionary(c=>new ChunkCoord(c.X,c.Y));
        foreach(var group in Drops.CaptureSaved().GroupBy(d=>ChunkCoord.At(new Vector2(d.X,d.Y),_world.Config.Streaming.ChunkSize)))
        {
            if(chunks.TryGetValue(group.Key,out var chunk)) chunks[group.Key]=chunk with { Drops=group.ToArray() };
            else chunks.Add(group.Key,new SavedChunk(_world.Blueprint.WorldId,"surface",group.Key.X,group.Key.Y,Array.Empty<ChunkEdit>(),group.ToArray()));
        }
        return chunks.OrderBy(p=>p.Key.Y).ThenBy(p=>p.Key.X).Select(p=>p.Value).ToArray();
    }
}
