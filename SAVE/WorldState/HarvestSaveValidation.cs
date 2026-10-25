// Detached schema-3 validation: source outcomes, independent drop ownership and inventory form one checkpoint.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Godot;
public static class HarvestSaveValidation
{
    // Validate references without loading Resources or touching live Nodes on the save worker.
    // =========================================================
    public static void Validate(CampaignSnapshot snapshot,Rect2 bounds)
    {
        var inventory=snapshot.Player.Inventory;
        if(snapshot.SchemaVersion<3)
        {
            if(inventory!=null || snapshot.Chunks.Any(c=>c.Edits.Any(e=>e.HarvestWork!=0 || e.Felled) || (c.Drops?.Length ?? 0)!=0))
                throw new InvalidDataException("Legacy schema cannot hide inventory or harvesting records.");
            return;
        }
        if(inventory==null || inventory.Slots<1 || inventory.Slots>256 || inventory.Items==null || inventory.Items.Length>inventory.Slots
            || inventory.HotbarKeys==null || inventory.HotbarKeys.Length!=9 || inventory.SelectedSlot< -1 || inventory.SelectedSlot>=9)
            throw new InvalidDataException("Missing/invalid saved inventory or hotbar.");
        var slots=new HashSet<int>(); var keys=new HashSet<long>();
        foreach(var item in inventory.Items)
            if(item==null || item.Slot<0 || item.Slot>=inventory.Slots || !slots.Add(item.Slot) || !ValidItemId(item.ItemId)
                || item.Count<1 || item.Count>9999 || item.Key<=0 || item.Key==long.MaxValue || !keys.Add(item.Key)) throw new InvalidDataException("Invalid saved item stack.");
        var shortcuts=new HashSet<long>();
        foreach(long key in inventory.HotbarKeys) if(key<0 || (key>0 && (!keys.Contains(key) || !shortcuts.Add(key)))) throw new InvalidDataException("Stale/duplicated saved shortcut.");
        var felled=new HashSet<string>(StringComparer.Ordinal);
        foreach(var chunk in snapshot.Chunks) foreach(var edit in chunk.Edits)
        {
            if(!float.IsFinite(edit.HarvestWork) || edit.HarvestWork<0 || edit.HarvestWork>=10000 || (edit.Removed && edit.HarvestWork!=0)
                || (edit.Felled && (!edit.Removed || edit.BurnProgress!=0))) throw new InvalidDataException("Invalid harvest outcome.");
            if(edit.Felled) felled.Add(edit.Id);
        }
        var ids=new HashSet<string>(StringComparer.Ordinal); int count=0;
        foreach(var chunk in snapshot.Chunks)
        {
            if((chunk.Drops?.Length ?? 0)>65536) throw new InvalidDataException("Too many saved chunk drops.");
            foreach(var drop in chunk.Drops ?? Array.Empty<SavedDrop>())
            {
                if(++count>65536 || drop==null || string.IsNullOrWhiteSpace(drop.Id) || drop.Id.Length>1024 || !ids.Add(drop.Id)
                    || drop.SourceId==null || !felled.Contains(drop.SourceId) || !ValidItemId(drop.ItemId) || drop.Quantity<1 || drop.Quantity>9999
                    || !float.IsFinite(drop.X) || !float.IsFinite(drop.Y) || !bounds.HasPoint(new Vector2(drop.X,drop.Y))
                    || !float.IsFinite(drop.Rotation) || Math.Abs(drop.Rotation)>Math.PI*2
                    || ChunkCoord.At(new Vector2(drop.X,drop.Y),snapshot.Blueprint.ChunkSize)!=new ChunkCoord(chunk.X,chunk.Y))
                    throw new InvalidDataException("Invalid/duplicated drop, missing felled source or wrong owner chunk.");
                string prefix=snapshot.WorldId+"/"+drop.SourceId+"/fell/0/reward/";
                if(!drop.Id.StartsWith(prefix,StringComparison.Ordinal)) throw new InvalidDataException("Drop belongs to another world/source.");
                string[] suffix=drop.Id[prefix.Length..].Split('/');
                if(suffix.Length!=3 || suffix[1]!="pickup" || !int.TryParse(suffix[0],out int reward) || reward<0 || reward>=16
                    || !int.TryParse(suffix[2],out int piece) || piece<0 || piece>=32) throw new InvalidDataException("Invalid drop identity.");
            }
        }
    }
    // Stable item IDs are labels, never OS paths or live resource objects.
    // =========================================================
    private static bool ValidItemId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length<=256 && id==id.Trim();
}
