// Isolated fixed negative-coordinate map; it never changes world configuration or campaigns.
using Godot;
public static class GroundLayerFixture
{
    // Four macro cells in each direction, two distinct materials and a biome edge.
    // =========================================================
    public static MacroWorldMap Map() => new(213876,new Rect2(-1024,-1024,2048,2048),512,4,4,
        new[] {0,0,1,1, 0,0,1,1, 0,0,1,1, 0,0,1,1},
        new[] {new MacroBiomeInfo("fixture.sand","Sand",Colors.Yellow),new MacroBiomeInfo("fixture.soil","Soil",Colors.Brown)},
        new[] {new MacroRegion(0,0,new Vector2(-512,0),1024,8),new MacroRegion(1,1,new Vector2(512,0),1024,8)});
    // Duplicate fixture data before any toggles; cached Resources remain untouched.
    // =========================================================
    public static WorldBiomeDefinition[] Definitions() => new[] {
        (WorldBiomeDefinition)GD.Load<WorldBiomeDefinition>("res://TESTS/Terrain/Fixtures/SandBiome.tres").Duplicate(true),
        (WorldBiomeDefinition)GD.Load<WorldBiomeDefinition>("res://TESTS/Terrain/Fixtures/SoilBiome.tres").Duplicate(true) };
}
