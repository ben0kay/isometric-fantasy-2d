// World-owned gameplay canvas: event-driven hotbar and non-pausing inventory composition.
using Godot;

public partial class GameplayHUD : CanvasLayer
{
    [Export] public UIMasterConfig Config { get; set; }
    [Export] public NodePath PlayerPath { get; set; } = new("../WorldActorsRoot/PersistentEntitiesRoot/Player");
    private PlayerInput _input;
    private PlayerInventoryView _inventory;
    private GameplayHotbar _hotbar;
    private Control _root;
    private Viewport _viewport;
    // Bind once to this world's player; no global lookups, frame callbacks or chunk ownership.
    // =========================================================
    public override void _Ready()
    {
        Config ??= GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres"); Layer=10;
        _root=new Control { MouseFilter=Control.MouseFilterEnum.Ignore }; AddChild(_root); _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hotbar=new GameplayHotbar(); _hotbar.Initialize(Config); _root.AddChild(_hotbar);
        _inventory=new PlayerInventoryView(); _root.AddChild(_inventory); _inventory.Initialize(Config);
        _input=GetNode<PlayerMovement>(PlayerPath).GetNode<PlayerInput>("Input");
        _input.InventoryToggleRequested+=ToggleInventory; _input.InventoryCloseRequested+=CloseInventory; _inventory.CloseRequested+=CloseInventory;
        _viewport=GetViewport(); _viewport.SizeChanged+=Fit; Fit();
    }
    // Resizing scales the complete belt when necessary and preserves a centered safe margin.
    // =========================================================
    private void Fit()
    {
        Vector2 viewport=_viewport.GetVisibleRect().Size;
        Vector2 bar=_hotbar.GetCombinedMinimumSize(); _hotbar.Size=bar;
        float scale=Mathf.Min(1,Mathf.Max(1,viewport.X-32)/Mathf.Max(1,bar.X)); _hotbar.Scale=Vector2.One*scale;
        _hotbar.Position=new Vector2((viewport.X-bar.X*scale)*0.5f,Mathf.Max(0,viewport.Y-bar.Y*scale-Mathf.Clamp(Config.BottomMargin,0,120)));
        _inventory.Fit(viewport);
    }
    // Toggle changes only player command availability; the scene tree keeps simulating.
    // =========================================================
    private void ToggleInventory()
    {
        if (_inventory.Visible) { CloseInventory(); return; }
        _input.SetInventoryOpen(true); _inventory.Open();
    }
    // All close routes restore commands through the same central input boundary.
    // =========================================================
    private void CloseInventory() { _inventory.Close(); _input.SetInventoryOpen(false); }
    // Release event references and input suppression with this world-owned canvas.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_viewport)) _viewport.SizeChanged-=Fit;
        if (GodotObject.IsInstanceValid(_input))
        { _input.InventoryToggleRequested-=ToggleInventory; _input.InventoryCloseRequested-=CloseInventory; _input.SetInventoryOpen(false); }
        if (GodotObject.IsInstanceValid(_inventory)) _inventory.CloseRequested-=CloseInventory;
    }
}
