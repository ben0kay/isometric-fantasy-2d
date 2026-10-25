// Bounded quick-access references to session stack keys, never extra storage. No player or UI dependency.
using System;

public sealed class InventoryHotbar : IDisposable
{
    public event Action Changed;
    public event Action SelectionInvalidated;
    public int SlotCount => _keys.Length;
    public int SelectedSlot { get; private set; } = -1;
    public Func<ItemDefinition,bool> CanSelect { get; set; }
    private readonly InventoryStorage _storage;
    private readonly long[] _keys;
    // Subscribe once; owners dispose when their session ends.
    // =========================================================
    public InventoryHotbar(InventoryStorage storage, int slots)
    {
        _storage=storage ?? throw new ArgumentNullException(nameof(storage));
        if (slots<1 || slots>9) throw new ArgumentOutOfRangeException(nameof(slots));
        _keys=new long[slots]; _storage.Changed+=OnStorageChanged;
    }
    // Read current quantity through authoritative storage.
    // =========================================================
    public InventoryStack Get(int index) => index>=0 && index<SlotCount ? _storage.Find(_keys[index]) : default;
    // Assign each physical stack to at most one shortcut without moving it.
    // =========================================================
    public bool Bind(int index, int storageSlot)
    {
        var stack=_storage.Get(storageSlot);
        if (index<0 || index>=SlotCount || stack.IsEmpty || index==SelectedSlot) return false;
        for(int i=0;i<SlotCount;i++) if (_keys[i]==stack.Key) { if (i==SelectedSlot) return false; _keys[i]=0; }
        _keys[index]=stack.Key; Changed?.Invoke(); return true;
    }
    // The equipment adapter may reject switching during attack/recovery. Never queue a rejected selection.
    // =========================================================
    public bool Select(int index)
    {
        if (index<0 || index>=SlotCount) return false;
        if (CanSelect!=null && !CanSelect(Get(index).Item)) return false;
        SelectedSlot=index; Changed?.Invoke(); return true;
    }
    // Persist references to durable stack keys, not copies of item quantities.
    // =========================================================
    public long[] CaptureKeys() => (long[])_keys.Clone();
    // Restore only valid unique shortcuts and explicitly reapply equipment selection before enabling player input.
    // =========================================================
    public void Restore(long[] keys,int selected)
    {
        if (keys==null || keys.Length!=SlotCount || selected< -1 || selected>=SlotCount) throw new System.IO.InvalidDataException("Invalid saved hotbar.");
        var seen=new System.Collections.Generic.HashSet<long>();
        foreach(long key in keys) if (key<0 || (key>0 && (!seen.Add(key) || _storage.Find(key).IsEmpty))) throw new System.IO.InvalidDataException("Stale or duplicate saved shortcut.");
        ItemDefinition item=selected>=0 ? _storage.Find(keys[selected]).Item : null;
        if (CanSelect!=null && !CanSelect(item)) throw new System.IO.InvalidDataException("Cannot restore equipped item.");
        Array.Copy(keys,_keys,SlotCount); SelectedSlot=selected; Changed?.Invoke();
    }
    // Clear disappeared stack references. Inventory removal policy must be enforced by its gameplay owner.
    // =========================================================
    private void OnStorageChanged()
    {
        bool invalidated=SelectedSlot>=0 && _keys[SelectedSlot]!=0 && _storage.Find(_keys[SelectedSlot]).IsEmpty;
        for(int i=0;i<SlotCount;i++) if (_storage.Find(_keys[i]).IsEmpty) _keys[i]=0;
        if (invalidated) SelectionInvalidated?.Invoke(); Changed?.Invoke();
    }
    // Release managed subscriptions; no scene/global cache retains the inventory owner.
    // =========================================================
    public void Dispose() { _storage.Changed-=OnStorageChanged; CanSelect=null; Changed=null; SelectionInvalidated=null; }
}
