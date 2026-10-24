// Removable Ground Surfaces drawer; only the production preview API changes runtime uniforms.
using Godot;
using System.Collections.Generic;
public partial class GroundSurfaceDebugPanel : VBoxContainer
{
    private readonly DebugWidgets _widgets=new();
    private readonly Dictionary<string,WorldBiomeDefinition> _biomes=new();
    private WorldScene _world;
    private GroundSurfacePreview _preview;
    private Label _status, _patchStatus;
    private CheckButton _patchToggle;
    private readonly Label[] _slotLabels=new Label[2];
    private readonly CheckButton[] _slotToggles=new CheckButton[2];
    private readonly HSlider[,] _sliders=new HSlider[2,2];
    private double _elapsed;
    private string _lastBiome;
    public int RefreshCount { get; private set; }
    // Optional test-scene injection; normal World resolves these from its production owner.
    public GroundSurfacePreview PreviewSource { get; set; }
    public System.Func<WorldBiomeDefinition> BiomeSource { get; set; }

    // The existing host owns F9/category selection; this body adds no hotkey or scene root.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false);
        for(Node parent=GetParent();parent!=null;parent=parent.GetParent())
            if(parent is WorldScene world) { _world=world; break; }
        _preview=PreviewSource ?? _world?.GroundPreview;
        if(_preview==null) { AddChild(new Label { Text="Ground preview is unavailable in this scene." }); return; }
        if(_world?.Stream!=null) foreach(var biome in _world.Config.Generation.Biomes)
            if(biome!=null) _biomes[biome.Id]=biome;
        AddChild(new Label { Text="Session preview only — changes are not saved.",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        _status=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_status);
        AddChild(new HSeparator()); AddChild(new Label { Text="Base appearance" });
        _widgets.Toggle(this,"Base fine detail",()=>_preview.ShowBaseDetail,v=>_preview.ShowBaseDetail=v);
        _widgets.Slider(this,"Base fine detail strength",0,2,0.01f,()=>_preview.BaseDetailStrength,v=>_preview.BaseDetailStrength=v);
        AddChild(new HSeparator()); AddChild(new Label { Text="Accompanying patch layer" });
        _patchStatus=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_patchStatus);
        _widgets.Toggle(this,"Show accompanying patches",()=>_preview.ShowAccompanyingPatches,v=>_preview.ShowAccompanyingPatches=v);
        _patchToggle=(CheckButton)GetChild(GetChildCount()-1);
        _widgets.Slider(this,"Accompanying patch strength",0,2,0.01f,()=>_preview.AccompanyingStrength,v=>_preview.AccompanyingStrength=v);
        if(_preview.SupportsOverlays)
        {
            AddChild(new HSeparator()); AddChild(new Label { Text="Extra patch layers — global comparisons" });
            _widgets.Toggle(this,"Show extra patch layers",()=>_preview.ShowOptionalLayers,v=>_preview.ShowOptionalLayers=v);
            for(int i=0;i<2;i++) BuildSlot(i);
            AddChild(new Label { Text="Sliders preview configured layers across all biomes. Missing layers stay off. The base always remains visible.",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        }
        else AddChild(new Label { Text="Compatibility preview: base comparisons only. Use streamed World for overlays.",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        var reset=new Button { Text="Reset Ground Preview",FocusMode=Control.FocusModeEnum.None }; AddChild(reset);
        reset.Pressed+=()=> { _preview.Reset(); Refresh(); };
        VisibilityChanged+=VisibilityUpdated; VisibilityUpdated(); Refresh();
    }
    // Each slot captures its own index; widgets never reference or mutate a shared Resource.
    // =========================================================
    private void BuildSlot(int slot)
    {
        var group=new VBoxContainer(); AddChild(group);
        _slotLabels[slot]=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; group.AddChild(_slotLabels[slot]);
        _widgets.Toggle(group,$"Show overlay {(slot==0 ? "A" : "B")}",()=>_preview.ShowOverlay(slot),v=>_preview.SetShowOverlay(slot,v));
        _slotToggles[slot]=(CheckButton)group.GetChild(group.GetChildCount()-1);
        _widgets.Slider(group,"Coverage multiplier",0,2,0.01f,()=>_preview.Coverage(slot),v=>_preview.SetCoverage(slot,v));
        _sliders[slot,0]=(HSlider)group.GetChild(group.GetChildCount()-1);
        _widgets.Slider(group,"Opacity multiplier",0,2,0.01f,()=>_preview.Opacity(slot),v=>_preview.SetOpacity(slot,v));
        _sliders[slot,1]=(HSlider)group.GetChild(group.GetChildCount()-1);
    }
    // Hidden/collapsed drawers have no processing callback; overrides remain active when hidden.
    // =========================================================
    private void VisibilityUpdated()
    { bool shown=IsVisibleInTree(); SetProcess(shown); _elapsed=0; if(shown) Refresh(); }
    // Only visible current-biome status polls, at no more than four times per second.
    // =========================================================
    public override void _Process(double delta)
    { _elapsed+=delta; if(_elapsed<0.25) return; _elapsed=0; Refresh(); }
    // Labels follow the local biome; controls are global session multipliers, not per-biome edits.
    // =========================================================
    private void Refresh()
    {
        if(_preview==null) return; RefreshCount++; _widgets.Refresh();
        WorldBiomeDefinition biome=null;
        if(BiomeSource!=null) biome=BiomeSource();
        else if(_world?.Stream!=null)
        {
            string id=_world.MacroMap.BiomeAt(_world.Player.GlobalPosition)?.Id;
            if(id!=null) _biomes.TryGetValue(id,out biome);
        }
        else if(_world!=null)
        {
            var layout=_world.Config.Biomes;
            biome=layout?.Secondary!=null && layout.IsSecondary(_world.Player.GlobalPosition) ? layout.Secondary : _world.Config.Biome;
        }
        string key=biome?.Id ?? "outside";
        if(key==_lastBiome) return; _lastBiome=key;
        var stack=biome?.GroundStack;
        var profile=stack?.DefiningBaseGround?.Surface ?? biome?.Surface;
        string profileName=profile==null ? "none" : string.IsNullOrEmpty(profile.ResourcePath) ? $"{profile.DetailStyle} surface" : System.IO.Path.GetFileNameWithoutExtension(profile.ResourcePath);
        string label=stack?.DefiningBaseGround?.Label ?? "Legacy base";
        _status.Text=$"Biome: {biome?.DisplayName ?? "Outside world"}\nID: {key}\nBase Ground: {label} ({profileName})";
        var accompanying=stack?.AccompanyingPatches;
        bool hasPatches=stack!=null ? accompanying?.Enabled==true && accompanying.Strength>0 : profile?.Variation>0;
        _patchStatus.Text=stack==null ? "Legacy accompanying patches" : accompanying==null ? "Not configured — base only"
            : $"{accompanying.Label} ({(hasPatches ? "authored on" : "authored off / zero strength")})";
        _patchToggle.Disabled=!hasPatches;
        if(!_preview.SupportsOverlays) return;
        for(int i=0;i<2;i++)
        {
            var layer=stack?.OverlayLayers!=null && i<stack.OverlayLayers.Count ? stack.OverlayLayers[i] : null;
            bool available=layer!=null && GroundOverlayPalette.CanCompile(layer);
            string state=layer==null ? "not configured here" : $"{layer.Label} ({(layer.Enabled ? "authored on" : available ? "off by default; preview available" : "authored off / zero strength")})";
            _slotLabels[i].Text=$"Overlay {(i==0 ? "A" : "B")}: {state}";
            _slotToggles[i].Disabled=!available;
            _sliders[i,0].Editable=available; _sliders[i,1].Editable=available;
        }
    }
}
