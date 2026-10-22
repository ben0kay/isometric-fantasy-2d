// Data-driven palette for all game buttons; applied on creation, not every frame.
using Godot;
[GlobalClass]
public partial class UIButtonStyle : Resource
{
    [Export] public Color Background { get; set; } = new("#302c25");
    [Export] public Color Hover { get; set; } = new("#50432f");
    [Export] public Color Pressed { get; set; } = new("#665033");
    [Export] public Color Border { get; set; } = new("#927950");
    [Export] public Color Foreground { get; set; } = new("#f4e7cf");
    [Export] public int FontSize { get; set; } = 22;
    [Export] public int Height { get; set; } = 58;
    [Export] public int CornerRadius { get; set; } = 6;
}
