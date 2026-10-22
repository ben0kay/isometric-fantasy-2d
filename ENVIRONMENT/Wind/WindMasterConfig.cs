// Signed shared wind retained from DEBUG; no regional wind simulation is invented in this pass.
using Godot;
[GlobalClass]
public partial class WindMasterConfig : Resource
{
    [Export(PropertyHint.Range, "-3,3,0.05")] public float Strength { get; set; } = 1f;
    [Export] public Vector2 DriftPerStrength { get; set; } = new(5f, 1.5f);
}
