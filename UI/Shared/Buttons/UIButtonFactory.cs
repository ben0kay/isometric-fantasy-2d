// Universal button creation and styling, shared by menus and gameplay UI.
using Godot;
using System;
public static class UIButtonFactory
{
    private const string DefaultPath = "res://UI/Shared/Buttons/DefaultButtonStyle.tres";
    private static UIButtonStyle _default;
    public enum Variant { Primary, Secondary, Danger, Small }
    // =========================================================
    public static Button Create(string title, Action pressed, Variant variant = Variant.Primary)
    {
        Button button = new() { Text = title, FocusMode = Control.FocusModeEnum.All };
        Apply(button, variant);
        if (pressed != null) button.Pressed += pressed;
        return button;
    }
    // =========================================================
    public static void Apply(Button button, Variant variant = Variant.Primary, UIButtonStyle profile = null)
    {
        profile ??= _default ??= GD.Load<UIButtonStyle>(DefaultPath);
        Color normal = profile.Background, hover = profile.Hover, pressed = profile.Pressed;
        Color border = profile.Border, foreground = profile.Foreground;
        int height = profile.Height, font = profile.FontSize;
        if (variant == Variant.Primary) { normal = new Color("#51402a"); hover = new Color("#745531"); }
        if (variant == Variant.Danger) { normal = new Color("#4b2927"); hover = new Color("#713330"); border = new Color("#b26c5c"); }
        if (variant == Variant.Small) { height = 38; font = 17; }
        button.CustomMinimumSize = new Vector2(0, height);
        button.AddThemeFontSizeOverride("font_size", font);
        button.AddThemeColorOverride("font_color", foreground);
        button.AddThemeColorOverride("font_hover_color", foreground);
        button.AddThemeColorOverride("font_pressed_color", foreground);
        button.AddThemeColorOverride("font_disabled_color", new Color(foreground, 0.45f));
        button.AddThemeStyleboxOverride("normal", Box(normal, border, profile.CornerRadius));
        button.AddThemeStyleboxOverride("hover", Box(hover, border, profile.CornerRadius));
        button.AddThemeStyleboxOverride("pressed", Box(pressed, border, profile.CornerRadius));
        button.AddThemeStyleboxOverride("disabled", Box(new Color("#262522"), new Color("#534c41"), profile.CornerRadius));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
    }
    // =========================================================
    private static StyleBoxFlat Box(Color background, Color border, int radius)
    {
        StyleBoxFlat box = new() { BgColor = background, BorderColor = border };
        box.SetBorderWidthAll(1); box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = 18; box.ContentMarginRight = 18;
        box.ContentMarginTop = 8; box.ContentMarginBottom = 8;
        return box;
    }
}
