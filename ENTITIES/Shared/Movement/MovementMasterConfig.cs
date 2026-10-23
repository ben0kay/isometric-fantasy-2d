// Shared movement switches; disabling separation preserves collision, navigation and reactions.
using Godot;
[GlobalClass]
public partial class MovementMasterConfig : Resource
{
    [ExportGroup("Separation")]
    [Export] public bool Enabled { get; set; } = true;
    [ExportGroup("Engagement")]
    [Export] public bool EngagementEnabled { get; set; } = true;
    [Export] public EngagementProfile EngagementDefaults { get; set; } = new();
}
