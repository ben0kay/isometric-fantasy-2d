// Shared static fantasy menu presentation, no particle simulation or per-frame UI updates.
using Godot;
public static class MenuScreenStyle
{
    // =========================================================
    public static void AddBackground(Control scene)
    {
        ColorRect background = new() { Color = new Color("#171a17"), MouseFilter = Control.MouseFilterEnum.Ignore };
        scene.AddChild(background); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        ColorRect accent = new() { Color = new Color("#2e3023"), MouseFilter = Control.MouseFilterEnum.Ignore };
        scene.AddChild(accent); accent.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        accent.CustomMinimumSize = new Vector2(0, 12);
    }
    // =========================================================
    public static VBoxContainer CreatePanel(Control scene, string title, string subtitle)
    {
        CenterContainer center = new(); scene.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(510, 0) };
        StyleBoxFlat style = new() { BgColor = new Color("#292c25"), BorderColor = new Color("#8f774c") };
        style.SetBorderWidthAll(2); style.SetCornerRadiusAll(8);
        style.ContentMarginLeft = 30; style.ContentMarginRight = 30;
        style.ContentMarginTop = 28; style.ContentMarginBottom = 28;
        panel.AddThemeStyleboxOverride("panel", style); center.AddChild(panel);
        VBoxContainer stack = new(); stack.AddThemeConstantOverride("separation", 13); panel.AddChild(stack);
        Label heading = new() { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        heading.AddThemeFontSizeOverride("font_size", 34);
        heading.AddThemeColorOverride("font_color", new Color("#e6ca91"));
        stack.AddChild(heading);
        Label description = new() { Text = subtitle, HorizontalAlignment = HorizontalAlignment.Center };
        description.AddThemeColorOverride("font_color", new Color("#d5cbb7"));
        stack.AddChild(description);
        stack.AddChild(new HSeparator());
        return stack;
    }
}
