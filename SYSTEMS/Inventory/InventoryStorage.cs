// Shared event-driven stack storage for any owner. Transactions are all-or-nothing; no Godot node or ticking callback.
using System;

public readonly record struct InventoryStack(ItemDefinition Item, int Count, long Key)
{
    public bool IsEmpty => Item==null || Count<=0;
}

public sealed class InventoryStorage
{
    public event Action Changed;
    public ItemCatalog Catalog { get; }
    public int SlotCount => _slots.Length;
    private readonly InventoryStack[] _slots;
    private long _nextKey=1;
    // Allocate bounded storage against a validated explicit catalog.
    // =========================================================
    public InventoryStorage(ItemCatalog catalog, int slots)
    {
        Catalog=catalog ?? throw new ArgumentNullException(nameof(catalog));
        if (slots<1 || slots>256) throw new ArgumentOutOfRangeException(nameof(slots));
        _slots=new InventoryStack[slots];
    }
    // Invalid reads are empty; callers cannot mutate the backing array.
    // =========================================================
    public InventoryStack Get(int index) => index>=0 && index<SlotCount ? _slots[index] : default;
    // Shortcuts resolve stable session stack identity after slot moves, without owning copies.
    // =========================================================
    public InventoryStack Find(long key)
    { if (key>0) foreach (var stack in _slots) if (stack.Key==key) return stack; return default; }
    // Validate total capacity before adding anything; fill existing stacks before empty slots.
    // =========================================================
    public bool TryAdd(ItemDefinition item, int count)
    {
        if (!Catalog.Contains(item) || count<=0) return false;
        long capacity=0;
        foreach (var stack in _slots) capacity+=stack.IsEmpty ? item.MaxStack : stack.Item==item ? Math.Max(0,item.MaxStack-stack.Count) : 0;
        if (capacity<count) return false;
        int remaining=count;
        for (int pass=0;pass<2 && remaining>0;pass++)
            for (int i=0;i<SlotCount && remaining>0;i++)
            {
                var stack=_slots[i];
                if (pass==0 ? stack.IsEmpty || stack.Item!=item : !stack.IsEmpty) continue;
                int added=Math.Min(remaining,item.MaxStack-stack.Count);
                _slots[i]=new(item,stack.Count+added,stack.IsEmpty ? _nextKey++ : stack.Key); remaining-=added;
            }
        Changed?.Invoke(); return true;
    }
    // Remove a bounded quantity once, retaining a live stack's shortcut identity.
    // =========================================================
    public bool TryRemove(int index, int count)
    {
        var stack=Get(index); if (stack.IsEmpty || count<=0 || count>stack.Count) return false;
        _slots[index]=count==stack.Count ? default : stack with { Count=stack.Count-count }; Changed?.Invoke(); return true;
    }
    // Move/swap or merge stacks atomically. Fully merged source shortcuts become empty.
    // =========================================================
    public bool TryMove(int from, int to)
    {
        if (from==to || from<0 || to<0 || from>=SlotCount || to>=SlotCount || _slots[from].IsEmpty) return false;
        var source=_slots[from]; var target=_slots[to];
        if (!target.IsEmpty && source.Item==target.Item)
        {
            int moved=Math.Min(source.Count,source.Item.MaxStack-target.Count); if (moved<=0) return false;
            _slots[to]=target with { Count=target.Count+moved };
            _slots[from]=moved==source.Count ? default : source with { Count=source.Count-moved };
        }
        else { _slots[to]=source; _slots[from]=target; }
        Changed?.Invoke(); return true;
    }
    // Snapshot individual stack identity/slot/count for the save coordinator, never sharing the mutable backing array.
    // =========================================================
    public SavedItemStack[] Capture()
    {
        var result=new System.Collections.Generic.List<SavedItemStack>();
        for(int i=0;i<SlotCount;i++) if (!_slots[i].IsEmpty) result.Add(new(i,_slots[i].Item.Id,_slots[i].Count,_slots[i].Key));
        return result.ToArray();
    }
    // Validate all loaded references before committing; unknown IDs or stack overflows never disappear silently.
    // =========================================================
    public void Restore(SavedItemStack[] items)
    {
        if (items==null) throw new System.IO.InvalidDataException("Missing saved inventory stacks.");
        var next=new InventoryStack[SlotCount]; var keys=new System.Collections.Generic.HashSet<long>(); long maximum=0;
        foreach(var item in items)
        {
            if (item==null || item.Slot<0 || item.Slot>=SlotCount || !next[item.Slot].IsEmpty || item.Key<=0 || item.Key==long.MaxValue || !keys.Add(item.Key))
                throw new System.IO.InvalidDataException("Invalid saved inventory identity.");
            var definition=Catalog.Get(item.ItemId);
            if (definition==null || item.Count<1 || item.Count>definition.MaxStack) throw new System.IO.InvalidDataException("Unknown item or invalid saved stack: "+item.ItemId);
            next[item.Slot]=new(definition,item.Count,item.Key); maximum=Math.Max(maximum,item.Key);
        }
        Array.Copy(next,_slots,SlotCount); _nextKey=maximum+1; Changed?.Invoke();
    }
    // Recalculate only on request following inventory events; no idle weight polling.
    // =========================================================
    public double TotalWeightKg()
    { double weight=0; foreach (var stack in _slots) if (!stack.IsEmpty) weight+=(double)stack.Item.WeightKg*stack.Count; return weight; }
}
