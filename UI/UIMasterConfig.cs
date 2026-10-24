// Shared Inspector-authored gameplay UI layout and palette; presentation owns no item state.
using Godot;

[GlobalClass]
public partial class UIMasterConfig : Resource
{
    [ExportGroup("Hotbar")]
    [Export(PropertyHint.Range,"1,9,1")] public int HotbarSlots { get; set; } = 9;
    [Export(PropertyHint.Range,"36,96,1")] public float SlotSize { get; set; } = 62;
    [Export(PropertyHint.Range,"0,24,1")] public int SlotSpacing { get; set; } = 7;
    [Export] public float BottomMargin { get; set; } = 26;
    [ExportGroup("Inventory")]
    [Export] public Vector2 InventorySize { get; set; } = new(760,560);
    [ExportGroup("Palette")]
    [Export] public Color PanelColor { get; set; } = new("#202823");
    [Export] public Color SlotColor { get; set; } = new("#151c19");
    [Export] public Color BorderColor { get; set; } = new("#71644b");
    [Export] public Color AccentColor { get; set; } = new("#c5a469");
    [Export] public Color TextColor { get; set; } = new("#ded8c7");
}
