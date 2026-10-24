// Species-owned seasonal targets; gradient RGB is leaf colour and alpha is recolour influence.
using Godot;

[GlobalClass]
public partial class SeasonalFoliageProfile : Resource
{
    [Export] public Gradient Palette { get; set; } = new();
}
