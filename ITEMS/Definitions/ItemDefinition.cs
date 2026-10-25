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
    [ExportGroup("Optional behavior")]
    [Export] public EquippedItemDefinition Equipment { get; set; }
    // Reject invalid authoring data at catalog registration, outside gameplay hot paths.
    // =========================================================
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Id!=Id.Trim() || string.IsNullOrWhiteSpace(DisplayName)
            || MaxStack<1 || MaxStack>9999 || !float.IsFinite(WeightKg) || WeightKg<0)
            throw new InvalidOperationException($"Invalid item definition: {ResourcePath} ({Id}).");
        if (Equipment!=null && (MaxStack!=1 || Equipment.PrimaryAttack==null || Equipment.SecondaryAttack==null || Equipment.Texture==null))
            throw new InvalidOperationException($"Equipment item {Id} requires one item per stack and complete combat/art references.");
    }
}
