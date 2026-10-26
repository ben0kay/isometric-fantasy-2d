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
        if ((schema != 1 && schema != 2 && schema != 3 && schema != CampaignSnapshot.CurrentSchema) || b == null || (b.SchemaVersion != 4 && b.SchemaVersion != 5 && b.SchemaVersion != 6 && b.SchemaVersion != 7 && b.SchemaVersion != 8 && b.SchemaVersion != 9 && b.SchemaVersion != 10 && b.SchemaVersion != 11)
            || b.GeneratorVersion != (b.SchemaVersion==11 ? MacroWorldMap.ClimateGeneratorVersion : b.SchemaVersion>=9 ? MacroWorldMap.OceanGeneratorVersion : b.SchemaVersion==8 ? MacroWorldMap.GeographyGeneratorVersion : MacroWorldMap.GeneratorVersion) || b.LocalGeneratorVersion != ChunkSceneryData.GeneratorVersion
            || (b.SchemaVersion >= 5 && b.SchemaVersion<8 && (b.Water?.Bodies == null || b.Water.Bodies.Length == 0))
            || (b.SchemaVersion>=9 ? (b.GeographyVersion!=WorldGeographyPlan.Version || b.Water==null || (b.Water.Version!=1 && b.Water.Version!=3 && b.Water.Version!=4)) : b.SchemaVersion==8 ? (b.GeographyVersion!=1 || b.Water==null || (b.Water.Version!=1 && b.Water.Version!=3))
                : (b.GeographyVersion!=0 || (b.Water!=null && b.Water.Version!=(b.SchemaVersion==7 ? 3 : b.SchemaVersion==6 ? 2 : 1))))
            || (b.SchemaVersion>=10 ? (b.Climate==null || b.AuthorPolicy==null) : (b.Climate!=null || b.AuthorPolicy!=null))
            || b.FeaturePlanVersion != WorldFeaturePlan.Version)
            throw new InvalidDataException("Unsupported save/generator version; migration is required. Files were preserved.");
    }
}
