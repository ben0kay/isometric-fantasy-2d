// Configurable slot presentation with optional shared-hotbar binding; no input polling or item authority.
using Godot;

public partial class GameplayHotbar : PanelContainer
{
    private InventorySlot[] _slots;
    private InventoryHotbar _data;
    private Label _hint;
    private string _interactionHint;
    // Construct passive slots once; the entire bar lets mouse events reach gameplay.
    // =========================================================
    public void Initialize(UIMasterConfig config)
    {
        MouseFilter=MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel",InventorySlot.Frame(config.PanelColor,config.BorderColor,12,10));
        var column=new VBoxContainer { MouseFilter=MouseFilterEnum.Ignore }; AddChild(column);
        var row=new HBoxContainer { MouseFilter=MouseFilterEnum.Ignore }; row.AddThemeConstantOverride("separation",Mathf.Clamp(config.SlotSpacing,0,24)); column.AddChild(row);
        _slots=new InventorySlot[Mathf.Clamp(config.HotbarSlots,1,9)];
        for (int i=0;i<_slots.Length;i++)
        { var slot=new InventorySlot(); slot.Initialize(config,(i+1).ToString(),Mathf.Clamp(config.SlotSize,36,96)); _slots[i]=slot; row.AddChild(slot); }
        _hint=new Label { Text="QUICK BELT  ·  Empty slots  ·  I  Backpack", HorizontalAlignment=HorizontalAlignment.Center, MouseFilter=MouseFilterEnum.Ignore };
        _hint.AddThemeFontSizeOverride("font_size",14); _hint.AddThemeColorOverride("font_color",config.TextColor); column.AddChild(_hint);
    }
    // Bind only when an inventory owner explicitly supplies state; production placeholders remain supported.
    // =========================================================
    public void Bind(InventoryHotbar data)
    { if (_data!=null) _data.Changed-=Refresh; _data=data; if (_data!=null) _data.Changed+=Refresh; Refresh(); }
    // Quantities and selection are resolved from shared storage, never copied into the UI as authority.
    // =========================================================
    private void Refresh()
    {
        for(int i=0;i<_slots.Length;i++) _slots[i].Display(_data?.Get(i) ?? default);
        SetSelected(_data?.SelectedSlot ?? -1); _hint.Text=_interactionHint ?? (_data==null ? "QUICK BELT  ·  Empty slots  ·  I  Backpack" : "QUICK BELT  ·  1–9 select  ·  I  Backpack");
    }
    // Optional world-specific instruction text is set once by composition.
    // =========================================================
    public void SetInteractionHint(string text) { _interactionHint=text; _hint.Text=text; }
    // Release the shared model when the canvas leaves its session.
    // =========================================================
    public override void _ExitTree() { if (_data!=null) _data.Changed-=Refresh; }
    // Future authoritative bindings can highlight a slot; -1 clears without selecting a fake item.
    // =========================================================
    public void SetSelected(int index)
    { for (int i=0;i<_slots.Length;i++) _slots[i].SetSelected(i==index); }
}
