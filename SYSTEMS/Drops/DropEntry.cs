// One independently rolled reward entry; only canonical item definitions are accepted.
using Godot;
[GlobalClass]
public partial class DropEntry : Resource
{
    [Export] public ItemDefinition Item { get; set; }
    [Export] public int Minimum { get; set; } = 1;
    [Export] public int Maximum { get; set; } = 1;
    [Export(PropertyHint.Range,"0,1,0.01")] public float Chance { get; set; } = 1;
}
