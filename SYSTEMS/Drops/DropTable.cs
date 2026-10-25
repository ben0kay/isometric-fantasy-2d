// Reusable deterministic reward data shared by harvesting and later death/container sources.
using Godot;
using System;

public readonly record struct DropReward(ItemDefinition Item, int Quantity);
[GlobalClass]
public partial class DropTable : Resource
{
    [Export] public Godot.Collections.Array<DropEntry> Entries { get; set; } = new();
    // Validate and snapshot outside the hit path; authored Resources never receive rolled runtime quantities.
    // =========================================================
    public DropPlan Compile(ItemCatalog catalog)
    {
        if (catalog==null || Entries==null || Entries.Count>16) throw new InvalidOperationException("Invalid reward table.");
        var rules=new DropPlan.Rule[Entries.Count];
        for(int i=0;i<rules.Length;i++)
        {
            var entry=Entries[i];
            if (entry==null || !catalog.Contains(entry.Item) || entry.Minimum<1 || entry.Maximum<entry.Minimum || entry.Maximum>9999
                || !float.IsFinite(entry.Chance) || entry.Chance<0 || entry.Chance>1) throw new InvalidOperationException("Invalid canonical reward entry.");
            rules[i]=new(entry.Item,entry.Minimum,entry.Maximum,entry.Chance);
        }
        return new DropPlan(rules);
    }
}

public sealed class DropPlan
{
    public readonly record struct Rule(ItemDefinition Item,int Minimum,int Maximum,float Chance);
    private readonly Rule[] _rules;
    // Keep the compiled snapshot isolated from caller mutation.
    // =========================================================
    public DropPlan(Rule[] rules) { _rules=(Rule[])rules.Clone(); }
    // Source identity selects an independent stream for each reward index, independent of call order.
    // =========================================================
    public DropReward[] Roll(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("Reward event requires a stable identity.");
        var result=new DropReward[_rules.Length];
        for(int i=0;i<_rules.Length;i++)
        {
            var rule=_rules[i]; ulong seed=DropRandom.Hash(eventId+"/reward/"+i);
            bool awarded=DropRandom.Unit(ref seed)<rule.Chance;
            int count=rule.Minimum+(int)(DropRandom.Next(ref seed)%(uint)(rule.Maximum-rule.Minimum+1));
            result[i]=new(rule.Item,awarded ? count : 0);
        }
        return result;
    }
}

public static class DropRandom
{
    // Stable FNV identity hash; never use Node IDs, global RNG state or process-randomized string hashes.
    // =========================================================
    public static ulong Hash(string value)
    { ulong hash=14695981039346656037UL; foreach(char c in value) { hash^=c; hash=unchecked(hash*1099511628211UL); } return hash; }
    // Small fixed integer generator keeps cosmetic/reward trials reproducible across sessions.
    // =========================================================
    public static ulong Next(ref ulong seed)
    { seed=unchecked(seed+0x9e3779b97f4a7c15UL); ulong z=seed; z=unchecked((z^(z>>30))*0xbf58476d1ce4e5b9UL); z=unchecked((z^(z>>27))*0x94d049bb133111ebUL); return z^(z>>31); }
    // Fixed 24-bit fraction in [0,1).
    // =========================================================
    public static float Unit(ref ulong seed) => (Next(ref seed)>>40)*(1f/16777216f);
}
