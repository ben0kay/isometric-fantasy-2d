// One switch for authored World practice actors; no polling and no production dependency on this script.
using Godot;

public partial class DebugActorRoot : Node2D
{
    private bool _enabled=true;
    [Export] public bool Enabled
    {
        get => _enabled;
        set { _enabled=value; ApplyEnabled(); }
    }

    // A disabled ancestor removes inherited actor bodies from physics through DisableMode.Remove.
    // =========================================================
    public override void _Ready() => ApplyEnabled();

    // Visibility alone is insufficient: suspend AI and physics together with the practice root.
    // =========================================================
    private void ApplyEnabled()
    { Visible=_enabled; ProcessMode=_enabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled; }
}
