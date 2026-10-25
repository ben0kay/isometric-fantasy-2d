// Data-driven starter loadout; granted only to new/legacy-without-inventory sessions, never over a restored inventory.
using Godot;
[GlobalClass]
public partial class PlayerInventoryProfile : Resource
{
    [Export] public ItemMasterConfig Items { get; set; }
    [Export] public InventoryMasterConfig Storage { get; set; }
    [Export] public Godot.Collections.Array<ItemDefinition> StartingItems { get; set; } = new();
}
