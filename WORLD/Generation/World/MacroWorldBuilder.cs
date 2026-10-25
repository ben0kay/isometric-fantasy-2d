// Bounded seeded sites and connected region growth partition all land using biome resource data.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public static class MacroWorldBuilder
{
    private sealed record Site(int Cell, int Biome, float Diameter);
    #region Generation
    // Validate before allocating; sort stable IDs so inspector list order does not change geography.
    // =========================================================
    public static MacroWorldMap Build(WorldGenerationMasterConfig config, int seed,WorldGeographyPlan geography=null)
    {
        if (config == null || !config.Bounds.Position.IsFinite() || !config.Bounds.Size.IsFinite() || !config.Bounds.End.IsFinite()
            || config.Bounds.Size.X <= 0 || config.Bounds.Size.Y <= 0 || !float.IsFinite(config.CellSize) || config.CellSize <= 0
            || !float.IsFinite(config.BoundaryIrregularity) || config.BoundaryIrregularity < 0 || config.BoundaryIrregularity > 1
            || config.MaximumCells < 1 || config.MaximumCells > 4194304 || config.MaximumRegions < 1 || config.MaximumRegions > 65536)
            throw new ArgumentException("Generation requires finite positive bounds/cell size, irregularity 0–1 and bounded limits.");
        double width = Math.Ceiling(config.Bounds.Size.X / config.CellSize), height = Math.Ceiling(config.Bounds.Size.Y / config.CellSize);
        if (width * height > config.MaximumCells) throw new ArgumentException("Macro map exceeds MaximumCells; increase CellSize or reduce bounds.");
        int columns = (int)width, rows = (int)height, count = columns * rows;
        if (config.Biomes == null || config.Biomes.Any(b => b == null)) throw new ArgumentException("Biome collection must contain valid Resource references.");
        var definitions = config.Biomes.Where(b => b != null && b.EnabledForGeneration).OrderBy(b => b.Id, StringComparer.Ordinal).ToArray();
        if (definitions.Length == 0 || definitions.Any(b => string.IsNullOrWhiteSpace(b.Id))
            || definitions.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
            throw new ArgumentException("Enabled biome IDs must be nonempty and unique; at least one is required.");
        foreach (var b in definitions) b.ValidateColour();
        foreach (var b in definitions)
            if (!float.IsFinite(b.GenerationWeight) || b.GenerationWeight < 0 || !float.IsFinite(b.MinimumRegionDiameter)
                || !float.IsFinite(b.MaximumRegionDiameter) || b.MinimumRegionDiameter <= 0 || b.MaximumRegionDiameter < b.MinimumRegionDiameter)
                throw new ArgumentException($"Invalid weight/target diameters for {b.Id}.");
        if (geography!=null && (geography.Seed!=seed || geography.Bounds!=config.Bounds))
            throw new ArgumentException("Biome map and geography must have matching ownership.");
        var rules=geography==null ? null : new GeographyBiomeRules(geography,config.CellSize,columns,rows,definitions);
        double total = definitions.Sum(b => (double)b.GenerationWeight);
        if (total <= 0) throw new ArgumentException("At least one enabled biome needs positive generation weight.");
        double meanArea = definitions.Sum(b => b.GenerationWeight / total * Math.PI * Math.Pow(((double)b.MinimumRegionDiameter + b.MaximumRegionDiameter) / 4, 2));
        double desired = Math.Ceiling((double)config.Bounds.Size.X * config.Bounds.Size.Y * (rules==null ? 1d : rules.LandCells/(double)count) / meanArea);
        if (desired > config.MaximumRegions) throw new ArgumentException("Target diameters require more than MaximumRegions; increase sizes or limit.");
        int siteCount = Math.Clamp((int)desired, 1, Math.Min(rules?.LandCells ?? count, config.MaximumRegions));
        var random = new SeedStream(seed); var sites = new List<Site>(siteCount); var used = new HashSet<int>();
        // Best of eight samples spreads seeds without potentially unbounded spacing retries.
        for (int i = 0; i < siteCount; i++)
        {
            double choice = random.Unit() * total; int biome = definitions.Length - 1;
            for (int b = 0; b < definitions.Length; b++) { choice -= definitions[b].GenerationWeight; if (choice < 0) { biome = b; break; } }
            var definition = definitions[biome];
            float diameter = definition.MinimumRegionDiameter + (float)random.Unit() * (definition.MaximumRegionDiameter - definition.MinimumRegionDiameter);
            int best = -1; double bestScore = -1;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int candidate = (int)(random.Unit() * count); if (used.Contains(candidate) || (rules!=null && !rules.IsLand(candidate))) continue;
                double nearest = double.PositiveInfinity;
                foreach (var other in sites)
                {
                    double dx = candidate % columns - other.Cell % columns, dy = candidate / columns - other.Cell / columns;
                    nearest = Math.Min(nearest, (dx * dx + dy * dy) / Math.Pow(((double)diameter + other.Diameter) / config.CellSize, 2));
                }
                if (nearest > bestScore) { bestScore = nearest; best = candidate; }
            }
            if (best < 0) { best = (int)(random.Unit() * count); while (used.Contains(best) || (rules!=null && !rules.IsLand(best))) best = (best + 1) % count; }
            if (rules?.NeedsSelection(best)==true)
            {
                biome=rules.Select(best,definitions,random.Unit()); definition=definitions[biome];
                diameter=definition.MinimumRegionDiameter+(float)random.Unit()*(definition.MaximumRegionDiameter-definition.MinimumRegionDiameter);
            }
            used.Add(best); sites.Add(new Site(best, biome, diameter));
        }
        return Grow(config, seed, columns, rows, definitions, sites,rules);
    }
    // Positive-cost growth guarantees complete, four-connected, uniquely owned regions.
    // =========================================================
    private static MacroWorldMap Grow(WorldGenerationMasterConfig config, int seed, int columns, int rows, WorldBiomeDefinition[] definitions, List<Site> sites,GeographyBiomeRules rules)
    {
        int[] cells = new int[columns * rows]; Array.Fill(cells, -1); var sizes = new List<int>(new int[sites.Count]);
        if (rules!=null) for (int i=0;i<cells.Length;i++) if (!rules.IsLand(i)) cells[i]=-2;
        var queue = new PriorityQueue<(int Cell, int Region), (double Cost, long Order)>(); long order = 0;
        for (int i = 0; i < sites.Count; i++) { cells[sites[i].Cell] = i; sizes[i] = 1; }
        for (int i = 0; i < sites.Count; i++) Expand(sites[i].Cell, i, 0);
        int unassigned=0;
        while (true)
        {
            while (queue.TryDequeue(out var item, out var priority))
            {
                if (cells[item.Cell] >= 0) continue;
                cells[item.Cell] = item.Region; sizes[item.Region]++; Expand(item.Cell, item.Region, priority.Cost);
            }
            while (unassigned<cells.Length && cells[unassigned]!=-1) unassigned++;
            if (unassigned==cells.Length) break;
            if (rules==null || sites.Count>=config.MaximumRegions)
                throw new ArgumentException("Compatible geography components exceed MaximumRegions; adjust biome eligibility or region limits.");
            // A hard constraint can isolate a component from existing seeds. Seed it explicitly, preserving connected regions.
            int biome=rules.Select(unassigned,definitions,Mix((uint)seed^(uint)unassigned^0x503542u)/4294967296.0);
            var definition=definitions[biome]; float diameter=(definition.MinimumRegionDiameter+definition.MaximumRegionDiameter)*0.5f;
            int region=sites.Count; sites.Add(new Site(unassigned,biome,diameter)); sizes.Add(1); cells[unassigned]=region;
            Expand(unassigned,region,0);
        }
        for (int i=0;i<cells.Length;i++) if (cells[i]==-2) cells[i]=-1;
        var regions = sites.Select((s, i) =>
        {
            float left = s.Cell % columns * config.CellSize, top = s.Cell / columns * config.CellSize;
            var centre = config.Bounds.Position + new Vector2(left + Mathf.Min(config.CellSize, config.Bounds.Size.X - left) * 0.5f,
                top + Mathf.Min(config.CellSize, config.Bounds.Size.Y - top) * 0.5f);
            return new MacroRegion(i, s.Biome, centre, s.Diameter, sizes[i]);
        }).ToArray();
        return new MacroWorldMap(seed, config.Bounds, config.CellSize, columns, rows, cells,
            definitions.Select(b => new MacroBiomeInfo(b.Id, b.DisplayName, b.Colour)).ToArray(), regions,
            rules?.GenerationVersion ?? MacroWorldMap.GeneratorVersion);
        void Expand(int cell, int region, double cost)
        {
            int x = cell % columns, y = cell / columns;
            if (x > 0) Add(cell - 1); if (x + 1 < columns) Add(cell + 1); if (y > 0) Add(cell - columns); if (y + 1 < rows) Add(cell + columns);
            void Add(int next)
            {
                if (cells[next] != -1 || (rules!=null && rules.Preference(next,sites[region].Biome)<=0)) return;
                uint key = Mix((uint)seed ^ (uint)Math.Min(cell, next) * 0x9e3779b9u ^ (uint)Math.Max(cell, next));
                double roughness = 1 + config.BoundaryIrregularity * (key / (double)uint.MaxValue * 2 - 1) * 0.9;
                queue.Enqueue((next, region), (cost + roughness * config.CellSize / sites[region].Diameter / (rules?.Preference(next,sites[region].Biome) ?? 1f), order++));
            }
        }
    }
    #endregion
    #region Randomness
    // Explicit integer math avoids runtime-dependent hashes or random implementations.
    // =========================================================
    private static uint Mix(uint v) { unchecked { v ^= v >> 16; v *= 0x7feb352du; v ^= v >> 15; v *= 0x846ca68bu; return v ^ (v >> 16); } }
    private sealed class SeedStream
    {
        private uint _state;
        public SeedStream(int seed) { _state = (uint)seed; }
        public double Unit() { unchecked { _state += 0x9e3779b9u; } return Mix(_state) / 4294967296.0; }
    }
    #endregion
}
