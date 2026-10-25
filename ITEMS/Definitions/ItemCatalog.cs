// Explicit, validated item registration for content authoring and future ID-based saves; no runtime folder scans.
using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class ItemCatalog : Resource
{
    [Export] public Godot.Collections.Array<ItemDefinition> Items { get; set; } = new();
    private Dictionary<string,ItemDefinition> _lookup;
    // Build atomically; a failed revalidation cannot leave a partially usable catalog.
    // =========================================================
    public void Initialize()
    {
        _lookup=null; var next=new Dictionary<string,ItemDefinition>(StringComparer.Ordinal);
        if (Items==null) throw new InvalidOperationException("Item catalog has no Items array.");
        foreach (var item in Items)
        {
            if (item==null) throw new InvalidOperationException("Item catalog contains a missing definition.");
            item.Validate();
            if (!next.TryAdd(item.Id,item)) throw new InvalidOperationException($"Duplicate item ID: {item.Id}");
        }
        _lookup=next;
    }
    // Resolve only successfully registered canonical definitions.
    // =========================================================
    public ItemDefinition Get(string id) => id!=null && _lookup!=null && _lookup.TryGetValue(id,out var item) ? item : null;
    // Storage accepts the registered resource itself, not a conflicting copy with the same ID.
    // =========================================================
    public bool Contains(ItemDefinition item) => item!=null && Get(item.Id)==item;
}
