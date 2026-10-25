// H1-H3 disposable vertical slice: real player, generic trees, harvesting, physical drops and shared inventory/HUD.
using Godot;
using GameTree = FantasyGame.World.Trees.Tree;

public partial class HarvestingPractice : Node2D
{
    [Export] public HarvestProfile TreeHarvest { get; set; }
    public PlayerMovement Player { get; private set; }
    public InventoryStorage Storage { get; private set; }
    public InventoryHotbar Hotbar { get; private set; }
    public WorldDropSpawner Drops { get; private set; }
    public GameTree Oak { get; private set; }
    public GameTree Blossom { get; private set; }
    private PlayerInput _input;
    private Label _status;
    private int _manual;
    private readonly Rect2 _bounds=new(-700,-450,1400,1000);
    private readonly Rect2 _water=new(-330,-60,90,140);
    // Build scoped test data without modifying production species/items, chunk state or campaign saves.
    // =========================================================
    public override void _Ready()
    {
        YSortEnabled=true;
        var source=GD.Load<ItemMasterConfig>("res://ITEMS/DefaultItemConfig.tres").Catalog;
        var catalog=new ItemCatalog();
        EquippedItemDefinition axe=null;
        foreach(var item in source.Items)
        {
            if (item.Equipment!=null && item.Id=="starteraxe")
            {
                var copy=(ItemDefinition)item.Duplicate(); axe=(EquippedItemDefinition)item.Equipment.Duplicate();
                axe.HarvestTool="axe"; axe.PrimaryHarvestWork=1; axe.SecondaryHarvestWork=0; copy.Equipment=axe; catalog.Items.Add(copy);
            }
            else catalog.Items.Add(item);
        }
        catalog.Initialize(); Storage=new InventoryStorage(catalog,24); Storage.TryAdd(catalog.Get("starteraxe"),1); Storage.TryAdd(catalog.Get("startersword"),1);
        Player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); Player.Name="Player"; Player.Position=new(25,20);
        Player.Config=(PlayerMasterConfig)Player.Config.Duplicate(); Player.Config.StarterAxe=axe; AddChild(Player);
        var tool=Player.GetNode<EquippedTool>("EquippedAxe"); Hotbar=new InventoryHotbar(Storage,9) { CanSelect=tool.TryEquipInventoryItem };
        Hotbar.SelectionInvalidated+=tool.ClearInventoryEquipment; Hotbar.Bind(0,0); Hotbar.Bind(1,1); Hotbar.Select(0);
        _input=Player.GetNode<PlayerInput>("Input"); _input.HotbarSelectionEnabled=true; _input.HotbarSelectionRequested+=Select;
        Drops=new WorldDropSpawner { Name="WorldDrops" }; AddChild(Drops); Drops.Initialize(catalog,"harvest-practice-v1",_bounds,GD.Load<DropScatterProfile>("res://SYSTEMS/Drops/DefaultDropScatter.tres"));
        Drops.LandingAllowed=point=>!_water.Grow(6).HasPoint(point);
        var pickup=new PlayerPickupInteraction { Name="PickupInteraction" }; AddChild(pickup); pickup.Initialize(Player,Storage,Drops);
        var hud=GD.Load<PackedScene>("res://UI/HUD/Gameplay/GameplayHUD.tscn").Instantiate<GameplayHUD>(); hud.PlayerPath=new NodePath("../Player"); AddChild(hud); hud.BindInventory(Storage,Hotbar);
        var ui=new CanvasLayer { Layer=11 }; AddChild(ui); var column=new VBoxContainer { Position=new Vector2(18,18) }; ui.AddChild(column);
        column.AddChild(new Label { Text="H1–H3 DISPOSABLE HARVEST PRACTICE · no saves\nArrows move · LMB axe chop ×4 · MMB prod does not chop · 1 axe / 2 sword\nE collects nearest settled log within 60 units · I backpack · blocked/full pickups remain\nOak to your right, mirrored Blossom lower-left, generic work target lower-right" });
        _status=new Label(); column.AddChild(_status);
        column.AddChild(UIButtonFactory.Create("Spawn 3 test logs",SpawnManualDrops,UIButtonFactory.Variant.Small));
        Oak=MakeTree("Oak/Oak.tres",new Vector2(95,20),false,"oak/0");
        Blossom=MakeTree("PinkBlossom/PinkBlossom.tres",new Vector2(-125,150),true,"blossom/0");
        Oak.HarvestState.Changed+=UpdateStatus; Blossom.HarvestState.Changed+=UpdateStatus; Storage.Changed+=UpdateStatus;
        var target=new PracticeHarvestTarget { Position=new Vector2(160,180) }; AddChild(target); target.Initialize(TreeHarvest,Drops,"generic/0");
        UpdateStatus(); QueueRedraw();
    }
    // Both species reuse the same shared scene and extraction profile; only fixture copies opt into harvesting.
    // =========================================================
    public GameTree MakeTree(string path,Vector2 position,bool mirrored,string id)
    {
        var definition=(TreeDefinition)GD.Load<TreeDefinition>("res://WORLD/Content/Vegetation/Trees/"+path).Duplicate(); definition.Harvesting=TreeHarvest;
        var tree=GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn").Instantiate<GameTree>(); tree.Configure(definition,0,mirrored);
        tree.Position=position; AddChild(tree); tree.EnableHarvesting(Drops,id); return tree;
    }
    // Independent H1 button bypasses harvesting while using exactly the same reserve/release/collection path.
    // =========================================================
    private void SpawnManualDrops()
    {
        string id=Drops.ScopeId+"/manual/"+_manual;
        if (Drops.TryReserve(id,new Vector2(0,120),new[] { new DropReward(Drops.Catalog.Get("wooden_log"),3) })) { _manual++; Drops.Release(id); }
    }
    // PlayerInput remains the only gameplay input owner.
    // =========================================================
    private void Select(int slot) => Hotbar.Select(slot);
    // Progress/inventory labels refresh only after accepted state changes.
    // =========================================================
    private void UpdateStatus()
    {
        string oak=GodotObject.IsInstanceValid(Oak) && !Oak.IsQueuedForDeletion() ? $"{Oak.HarvestState.Progress}/4" : "felled";
        string blossom=GodotObject.IsInstanceValid(Blossom) && !Blossom.IsQueuedForDeletion() ? $"{Blossom.HarvestState.Progress}/4" : "felled";
        _status.Text=$"Oak {oak} · Blossom {blossom} · carried mass {Storage.TotalWeightKg():0.##} kg";
    }
    // Scope teardown releases managed subscriptions; no globals retain inventories or drop records.
    // =========================================================
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_input)) { _input.HotbarSelectionRequested-=Select; _input.HotbarSelectionEnabled=false; } if (Storage!=null) Storage.Changed-=UpdateStatus; Hotbar?.Dispose(); }
    // Static floor/water test boundaries render once and do not simulate a second environment.
    // =========================================================
    public override void _Draw()
    { DrawRect(_bounds,new Color("#344238")); DrawRect(_water,new Color("#344e64")); }
}
