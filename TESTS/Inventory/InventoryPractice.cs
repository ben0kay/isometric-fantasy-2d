// Disposable item/inventory slice. Test supplies are explicit and never installed into real campaigns or saves.
using Godot;

public partial class InventoryPractice : Node2D
{
    public InventoryStorage Storage { get; private set; }
    public InventoryHotbar Hotbar { get; private set; }
    public PlayerMovement Player { get; private set; }
    private PlayerInput _input;
    // Compose real Player/HUD and a data-authored catalog without production world/save changes.
    // =========================================================
    public override void _Ready()
    {
        var catalog=GD.Load<ItemMasterConfig>("res://ITEMS/DefaultItemConfig.tres").Catalog; catalog.Initialize();
        var config=GD.Load<InventoryMasterConfig>("res://SYSTEMS/Inventory/DefaultInventoryConfig.tres");
        Storage=new InventoryStorage(catalog,config.StorageSlots);
        Storage.TryAdd(catalog.Get("starteraxe"),1); Storage.TryAdd(catalog.Get("startersword"),1); Storage.TryAdd(catalog.Get("wooden_log"),12);
        Player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); Player.Name="Player"; AddChild(Player);
        Hotbar=new InventoryHotbar(Storage,9) { CanSelect=Player.GetNode<EquippedTool>("EquippedAxe").TryEquipInventoryItem };
        Hotbar.SelectionInvalidated+=Player.GetNode<EquippedTool>("EquippedAxe").ClearInventoryEquipment;
        Hotbar.Bind(0,0); Hotbar.Bind(1,1); Hotbar.Bind(2,2); Hotbar.Select(0);
        _input=Player.GetNode<PlayerInput>("Input"); _input.HotbarSelectionEnabled=true; _input.HotbarSelectionRequested+=Select;
        var hud=GD.Load<PackedScene>("res://UI/HUD/Gameplay/GameplayHUD.tscn").Instantiate<GameplayHUD>(); hud.PlayerPath=new NodePath("../Player"); AddChild(hud); hud.BindInventory(Storage,Hotbar);
        var layer=new CanvasLayer(); AddChild(layer);
        var column=new VBoxContainer { Position=new Vector2(20,20) }; layer.AddChild(column);
        column.AddChild(new Label { Text="ITEM PRACTICE · disposable, no saves\nI backpack · 1 axe · 2 sword · 3 logs (empty hands) · 4–9 empty\nArrows move · LMB/MMB attack with a weapon · logs have placeholder artwork" });
        column.AddChild(UIButtonFactory.Create("Add 5 test logs",()=>Storage.TryAdd(catalog.Get("wooden_log"),5),UIButtonFactory.Variant.Small));
        QueueRedraw();
    }
    // Central PlayerInput emits intent; fixture adapter forwards it to the shared hotbar.
    // =========================================================
    private void Select(int slot) => Hotbar.Select(slot);
    // Unsubscribe player intent and dispose model listeners with this fixture.
    // =========================================================
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_input)) { _input.HotbarSelectionRequested-=Select; _input.HotbarSelectionEnabled=false; } Hotbar?.Dispose(); }
    // Static practice ground redraws once, never on an idle timer.
    // =========================================================
    public override void _Draw() => DrawRect(new Rect2(-800,-544,2560,1728),new Color("#344238"));
}
