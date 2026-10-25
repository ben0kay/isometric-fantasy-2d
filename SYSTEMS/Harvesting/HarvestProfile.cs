// Optional extraction settings: reusable work requirement/tool capability plus independent reward table.
using Godot;
using System;
[GlobalClass]
public partial class HarvestProfile : Resource
{
    [Export] public string AcceptedTool { get; set; } = "axe";
    [Export] public float RequiredWork { get; set; } = 4;
    [Export] public DropTable Rewards { get; set; }
    // Validate once per harvest-source setup, not once per idle tree.
    // =========================================================
    public void Validate()
    { if (string.IsNullOrWhiteSpace(AcceptedTool) || !float.IsFinite(RequiredWork) || RequiredWork<=0 || RequiredWork>10000 || Rewards==null) throw new InvalidOperationException("Invalid harvest profile."); }
}

public readonly record struct ToolWork(string Kind,float Amount)
{
    // Empty means no harvest capability; otherwise reject malformed item tuning before an attack starts.
    // =========================================================
    public void Validate()
    { if (string.IsNullOrEmpty(Kind) ? Amount!=0 : string.IsNullOrWhiteSpace(Kind) || !float.IsFinite(Amount) || Amount<=0 || Amount>1000) throw new ArgumentException("Invalid tool work capability."); }
}
public readonly record struct HarvestHit(Node2D Source,ToolWork Tool,Vector2 Direction,Vector2 Position);
public interface IHarvestHitTarget { void ReceiveHarvestHit(HarvestHit hit); }
public enum HarvestWorkResult { Rejected,Progress,Completed }
