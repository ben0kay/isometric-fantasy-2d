// Shared explicit item catalog reference; no runtime quantities live in configuration.
// =========================================================
using Godot;

[GlobalClass]
public partial class ItemMasterConfig : Resource
{
    [Export] public ItemCatalog Catalog { get; set; }
}
