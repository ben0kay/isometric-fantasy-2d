// Non-pausing pack view with optional shared storage binding and honest equipment previews.
using Godot;
using System;

public partial class PlayerInventoryView : Control
{
    public event Action CloseRequested;
    private Button _close;
    private PanelContainer _panel;
    private UIMasterConfig _config;
    private PanelContainer _pack;
    private Label _empty, _summary, _footnote;
    private InventoryStorage _storage;
    private InventorySlot[] _slots;
    private bool _dirty;
    // Build one screen-covering input shield and one responsive, scrollable pack panel.
    // =========================================================
    public void Initialize(UIMasterConfig config)
    {
        _config=config; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter=MouseFilterEnum.Stop;
        var shade=new ColorRect { Color=new Color(0.03f,0.05f,0.04f,0.28f), MouseFilter=MouseFilterEnum.Ignore };
        AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel=new PanelContainer { MouseFilter=MouseFilterEnum.Stop }; AddChild(_panel);
        _panel.AddThemeStyleboxOverride("panel",InventorySlot.Frame(config.PanelColor,config.BorderColor,18,22));
        var scroll=new ScrollContainer { HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled }; _panel.AddChild(scroll);
        var column=new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill }; column.AddThemeConstantOverride("separation",16); scroll.AddChild(column);
        var heading=new HBoxContainer(); column.AddChild(heading);
        var title=Text("ADVENTURER’S PACK",24); title.SizeFlagsHorizontal=SizeFlags.ExpandFill; heading.AddChild(title);
        _close=UIButtonFactory.Create("Close · I / Esc",()=>CloseRequested?.Invoke(),UIButtonFactory.Variant.Small); heading.AddChild(_close);
        column.AddChild(Text("The world continues while you look through your pack.",16));
        column.AddChild(Text("WORN EQUIPMENT",17));
        var equipment=new HFlowContainer(); equipment.AddThemeConstantOverride("h_separation",8); column.AddChild(equipment);
        foreach (string name in new[] { "Head", "Body", "Hands", "Belt", "Back" })
        { var slot=new InventorySlot(); slot.Initialize(config,name,86); equipment.AddChild(slot); }
        column.AddChild(Text("BACKPACK",17));
        _pack=new PanelContainer(); _pack.AddThemeStyleboxOverride("panel",InventorySlot.Frame(config.SlotColor,config.BorderColor,12,20)); column.AddChild(_pack);
        _empty=Text("Your backpack is not connected yet.\nItems, stacks and carry weight arrive in UI3.",18); _empty.CustomMinimumSize=new Vector2(0,120); _empty.AutowrapMode=TextServer.AutowrapMode.WordSmart; _pack.AddChild(_empty);
        _summary=Text("",16); column.AddChild(_summary);
        _footnote=Text("Equipment spaces are previews. Starter weapon keys stay 1 / 2 / 3.",15); column.AddChild(_footnote);
        Hide();
    }
    // Create reusable stack views once for the supplied owner's storage; equipment spaces remain future views.
    // =========================================================
    public void Bind(InventoryStorage storage)
    {
        if (_storage!=null) throw new InvalidOperationException("Inventory view is already bound.");
        _storage=storage ?? throw new ArgumentNullException(nameof(storage));
        _empty.Hide(); var cells=new HFlowContainer(); cells.AddThemeConstantOverride("h_separation",8); cells.AddThemeConstantOverride("v_separation",8); _pack.AddChild(cells);
        _slots=new InventorySlot[storage.SlotCount];
        for(int i=0;i<_slots.Length;i++) { var slot=new InventorySlot(); slot.Initialize(_config,"",64); slot.MouseFilter=MouseFilterEnum.Stop; _slots[i]=slot; cells.AddChild(slot); }
        _footnote.Text="Equipment spaces above are previews. The belt references the backpack items below.";
        _storage.Changed+=MarkDirty; MarkDirty();
    }
    // Hidden panels defer drawing updates until opened; storage remains authoritative.
    // =========================================================
    private void MarkDirty() { _dirty=true; if (Visible) Refresh(); }
    // Reuse existing widgets and calculate weight only following real storage changes.
    // =========================================================
    private void Refresh()
    {
        if (!_dirty || _storage==null) return;
        for(int i=0;i<_slots.Length;i++) _slots[i].Display(_storage.Get(i));
        _summary.Text=$"Carried items: {_storage.TotalWeightKg():0.##} kg  ·  No encumbrance rules yet"; _dirty=false;
    }
    // Never retain an inventory model after scene teardown.
    // =========================================================
    public override void _ExitTree() { if (_storage!=null) _storage.Changed-=MarkDirty; }
    // Labels share the gameplay palette and never act as fake interactive buttons.
    // =========================================================
    private Label Text(string value, int size)
    { var label=new Label { Text=value, MouseFilter=MouseFilterEnum.Ignore, AutowrapMode=TextServer.AutowrapMode.WordSmart }; label.AddThemeFontSizeOverride("font_size",size); label.AddThemeColorOverride("font_color",_config.TextColor); return label; }
    // Only viewport resize events adjust layout, never an idle frame loop.
    // =========================================================
    public void Fit(Vector2 viewport)
    {
        Vector2 size=new(Mathf.Min(Mathf.Clamp(_config.InventorySize.X,360,1200),Mathf.Max(1,viewport.X-40)),Mathf.Min(Mathf.Clamp(_config.InventorySize.Y,260,900),Mathf.Max(1,viewport.Y-40)));
        _panel.Position=(viewport-size)*0.5f; _panel.Size=size;
    }
    // Focus stays in the view while open; keyboard toggle is owned by PlayerInput.
    // =========================================================
    public void Open() { Refresh(); Show(); _close.GrabFocus(); }
    // Release keyboard focus before returning to gameplay.
    // =========================================================
    public void Close() { GetViewport().GuiGetFocusOwner()?.ReleaseFocus(); Hide(); }
}
