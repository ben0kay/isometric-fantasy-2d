// Nine configurable visual slots; no selection keys or invented item/equipment authority.
using Godot;

public partial class GameplayHotbar : PanelContainer
{
    private InventorySlot[] _slots;
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
        var hint=new Label { Text="QUICK BELT  ·  Empty slots  ·  I  Backpack", HorizontalAlignment=HorizontalAlignment.Center, MouseFilter=MouseFilterEnum.Ignore };
        hint.AddThemeFontSizeOverride("font_size",14); hint.AddThemeColorOverride("font_color",config.TextColor); column.AddChild(hint);
    }
    // Future authoritative bindings can highlight a slot; -1 clears without selecting a fake item.
    // =========================================================
    public void SetSelected(int index)
    { for (int i=0;i<_slots.Length;i++) _slots[i].SetSelected(i==index); }
}
