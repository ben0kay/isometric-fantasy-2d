// Non-pausing pack/equipment placeholder view; all actual buttons use the shared factory.
using Godot;
using System;

public partial class PlayerInventoryView : Control
{
    public event Action CloseRequested;
    private Button _close;
    private PanelContainer _panel;
    private UIMasterConfig _config;
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
        var pack=new PanelContainer(); pack.AddThemeStyleboxOverride("panel",InventorySlot.Frame(config.SlotColor,config.BorderColor,12,20)); column.AddChild(pack);
        var empty=Text("Your backpack is not connected yet.\nItems, stacks and carry weight arrive in UI3.",18); empty.CustomMinimumSize=new Vector2(0,120); empty.AutowrapMode=TextServer.AutowrapMode.WordSmart; pack.AddChild(empty);
        column.AddChild(Text("Equipment spaces are previews. Starter weapon keys stay 1 / 2 / 3.",15));
        Hide();
    }
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
    public void Open() { Show(); _close.GrabFocus(); }
    // Release keyboard focus before returning to gameplay.
    // =========================================================
    public void Close() { GetViewport().GuiGetFocusOwner()?.ReleaseFocus(); Hide(); }
}
