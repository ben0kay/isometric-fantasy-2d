// Disposable GS3 comparison scene; input rebuilds a shared material only when a key is pressed.
using Godot;
public partial class GroundLayersPractice : Node2D
{
    private WorldBiomeDefinition[] _biomes;
    private MeshInstance2D[] _quads=new MeshInstance2D[16];
    private ShaderMaterial _material;
    private Camera2D _camera;
    private Label _label;
    // Fixed camera/map and sixteen 512-unit chunks expose seams and negative-coordinate sampling.
    // =========================================================
    public override void _Ready()
    {
        _biomes=GroundLayerFixture.Definitions();
        _camera=new Camera2D { Zoom=new Vector2(0.45f,0.45f) }; AddChild(_camera);
        for(int y=0;y<4;y++) for(int x=0;x<4;x++)
        { var quad=StreamedGroundPalette.Create(new Rect2(-1024+x*512,-1024+y*512,512,512),null); _quads[y*4+x]=quad; AddChild(quad); }
        var ui=new CanvasLayer(); AddChild(ui); _label=new Label { Position=new Vector2(16,16) }; ui.AddChild(_label);
        Rebuild(); SetProcess(false);
    }
    // Toggle A/B separately, reset, or zoom; production F9 is untouched.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if(input is not InputEventKey key || !key.Pressed || key.Echo) return;
        var layers=_biomes[0].GroundStack.OverlayLayers;
        if(key.Keycode==Key.Key1) layers[0].Enabled=!layers[0].Enabled;
        else if(key.Keycode==Key.Key2) layers[1].Enabled=!layers[1].Enabled;
        else if(key.Keycode==Key.R) { layers[0].Enabled=true; layers[1].Enabled=true; }
        else if(key.Keycode==Key.Z) { _camera.Zoom= _camera.Zoom.X<0.5f ? Vector2.One : new Vector2(0.45f,0.45f); return; }
        else return;
        Rebuild();
    }
    // Release replaced world-owned GPU resources after rebinding every quad.
    // =========================================================
    private void Rebuild()
    {
        var previous=_material; _material=StreamedGroundPalette.Build(GroundLayerFixture.Map(),_biomes);
        foreach(var quad in _quads) quad.Material=_material;
        previous?.Dispose();
        var layers=_biomes[0].GroundStack.OverlayLayers;
        _label.Text=$"GS3 fixture: Sand left / Soil right | 1 Soil: {layers[0].Enabled} | 2 Mineral: {layers[1].Enabled} | R Reset | Z Zoom";
    }
}
