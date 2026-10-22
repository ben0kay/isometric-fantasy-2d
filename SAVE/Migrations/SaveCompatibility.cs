// First durable schema: future migrations must retain original files and produce validated snapshots.
using System.IO;
public static class SaveCompatibility
{
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
