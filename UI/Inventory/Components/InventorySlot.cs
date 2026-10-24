// Passive reusable empty-slot presentation; real item bindings are deferred to UI3.
using Godot;

public partial class InventorySlot : PanelContainer
{
    private StyleBoxFlat _frame;
    private Color _border, _accent;
    // Build once with shared palette data; passive slots never capture gameplay input.
    // =========================================================
    public void Initialize(UIMasterConfig config, string caption, float size)
    {
        MouseFilter=MouseFilterEnum.Ignore; CustomMinimumSize=new Vector2(size,size);
        _border=config.BorderColor; _accent=config.AccentColor;
        _frame=Frame(config.SlotColor,_border,8,8); AddThemeStyleboxOverride("panel",_frame);
        var label=new Label { Text=caption, MouseFilter=MouseFilterEnum.Ignore, VerticalAlignment=VerticalAlignment.Top };
        label.AddThemeColorOverride("font_color",config.TextColor); label.AddThemeFontSizeOverride("font_size",14); AddChild(label);
    }
    // Selection is presentation-only and updates exclusively on explicit state changes.
    // =========================================================
    public void SetSelected(bool selected) { _frame.BorderColor=selected ? _accent : _border; _frame.SetBorderWidthAll(selected ? 2 : 1); }
    // Shared lightweight rounded frame for pack and hotbar surfaces.
    // =========================================================
    public static StyleBoxFlat Frame(Color fill, Color border, int radius, int padding)
    {
        var box=new StyleBoxFlat { BgColor=fill, BorderColor=border, ContentMarginLeft=padding, ContentMarginRight=padding, ContentMarginTop=padding, ContentMarginBottom=padding };
        box.SetBorderWidthAll(1); box.SetCornerRadiusAll(radius); return box;
    }
}
