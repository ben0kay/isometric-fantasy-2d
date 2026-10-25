// Reusable slot presentation; observes stack values without owning inventory state.
using Godot;

public partial class InventorySlot : PanelContainer
{
    private StyleBoxFlat _frame;
    private Color _border, _accent;
    private TextureRect _icon;
    private Label _count;
    // Build once with shared palette data; passive slots never capture gameplay input.
    // =========================================================
    public void Initialize(UIMasterConfig config, string caption, float size)
    {
        MouseFilter=MouseFilterEnum.Ignore; CustomMinimumSize=new Vector2(size,size);
        _border=config.BorderColor; _accent=config.AccentColor;
        _frame=Frame(config.SlotColor,_border,8,8); AddThemeStyleboxOverride("panel",_frame);
        var content=new Control { MouseFilter=MouseFilterEnum.Ignore }; AddChild(content);
        _icon=new TextureRect { MouseFilter=MouseFilterEnum.Ignore, ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize, StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered };
        content.AddChild(_icon); _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); _icon.OffsetLeft=9; _icon.OffsetTop=9; _icon.OffsetRight=-9; _icon.OffsetBottom=-9;
        var label=new Label { Text=caption, MouseFilter=MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color",config.TextColor); label.AddThemeFontSizeOverride("font_size",14); content.AddChild(label);
        _count=new Label { MouseFilter=MouseFilterEnum.Ignore, HorizontalAlignment=HorizontalAlignment.Right, VerticalAlignment=VerticalAlignment.Bottom };
        content.AddChild(_count); _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); _count.AddThemeFontSizeOverride("font_size",14);

    }
    // Refresh on inventory events; missing icons remain honestly blank with descriptive tooltips.
    // =========================================================
    public void Display(InventoryStack stack)
    {
        _icon.Texture=stack.Item?.Icon; _count.Text=stack.Count>1 ? stack.Count.ToString() : "";
        TooltipText=stack.IsEmpty ? "Empty" : $"{stack.Item.DisplayName} ×{stack.Count}\n{stack.Item.Description}\n{stack.Item.WeightKg:0.##} kg each";
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
