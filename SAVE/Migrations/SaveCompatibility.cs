// First durable schema: future migrations must retain original files and produce validated snapshots.
using System.IO;
public static class SaveCompatibility
{
    // Exact source scene accepted by the SO1 → SO2 layout-only compatibility bridge.
    public const string SO2SceneSourceHash = "F5B98A79AEA0DDEEB52685E827EE8B159835C841CBF3944C3B4A86F35CC3247E";
    // Schema and generator versions are independent; never silently reshape an established world.
    // =========================================================
    public static void RequireSupported(int schema, WorldBlueprintManifest b)
    {
        if (schema != CampaignSnapshot.CurrentSchema || b == null || b.SchemaVersion != 4
            || b.GeneratorVersion != MacroWorldMap.GeneratorVersion || b.LocalGeneratorVersion != ChunkSceneryData.GeneratorVersion
            || b.FeaturePlanVersion != WorldFeaturePlan.Version)
            throw new InvalidDataException("Unsupported save/generator version; migration is required. Files were preserved.");
    }
}
