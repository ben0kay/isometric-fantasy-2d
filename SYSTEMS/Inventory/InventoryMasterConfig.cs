// Shared storage tuning; slot capacity is independent from the presentation grid and hotbar.
using Godot;
[GlobalClass]
public partial class InventoryMasterConfig : Resource
{
    [Export(PropertyHint.Range,"1,256,1")] public int StorageSlots { get; set; } = 24;
}
