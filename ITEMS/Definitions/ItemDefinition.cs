// Shared item authoring data. Runtime quantities and individual stack identities never live in Resources.
using Godot;
using System;

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "Item";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [ExportGroup("Inventory")]
    [Export] public Texture2D Icon { get; set; }
    [Export(PropertyHint.Range,"1,9999,1")] public int MaxStack { get; set; } = 1;
    [Export] public float WeightKg { get; set; }
    [ExportGroup("World pickup appearance")]
    [Export] public Texture2D WorldSprite { get; set; }
    [Export] public float WorldSize { get; set; } = 24;
    [Export] public bool AllowWorldRotation { get; set; } = true;
    [ExportGroup("Optional behavior")]
    [Export] public EquippedItemDefinition Equipment { get; set; }
    // Reject invalid authoring data at catalog registration, outside gameplay hot paths.
    // =========================================================
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Id!=Id.Trim() || string.IsNullOrWhiteSpace(DisplayName)
            || !float.IsFinite(WorldSize) || WorldSize<4 || WorldSize>96
            || MaxStack<1 || MaxStack>9999 || !float.IsFinite(WeightKg) || WeightKg<0)
            throw new InvalidOperationException($"Invalid item definition: {ResourcePath} ({Id}).");
        if (Equipment!=null && (!float.IsFinite(Equipment.PrimaryHarvestWork) || Equipment.PrimaryHarvestWork<0 || Equipment.PrimaryHarvestWork>1000
            || !float.IsFinite(Equipment.SecondaryHarvestWork) || Equipment.SecondaryHarvestWork<0 || Equipment.SecondaryHarvestWork>1000
            || ((Equipment.PrimaryHarvestWork>0 || Equipment.SecondaryHarvestWork>0) && string.IsNullOrWhiteSpace(Equipment.HarvestTool))))
            throw new InvalidOperationException($"Invalid tool work on item {Id}.");
        if (Equipment!=null && (MaxStack!=1 || Equipment.PrimaryAttack==null || Equipment.SecondaryAttack==null || Equipment.Texture==null))
            throw new InvalidOperationException($"Equipment item {Id} requires one item per stack and complete combat/art references.");
    }
}
