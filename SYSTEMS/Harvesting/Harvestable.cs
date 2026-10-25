// Generic dormant extraction state; only confirmed eligible tool work advances progress. No tree-specific logic.
using Godot;
using System;

public partial class Harvestable : Node
{
    public event Action Changed;
    public float Progress { get; private set; }
    public float RequiredWork { get; private set; }
    public bool Completed { get; private set; }
    public string EventId { get; private set; }
    public Func<bool> Eligible { get; set; }
    public Func<DropReward[],HarvestHit,bool> TryComplete { get; set; }
    private string _tool;
    private DropPlan _plan;
    private DropReward[] _prepared;
    private bool _completing;
    // Snapshot shared configuration and require an externally assigned world/source event identity.
    // =========================================================
    public void Initialize(HarvestProfile profile,ItemCatalog catalog,string eventId)
    {
        if (_plan!=null || string.IsNullOrWhiteSpace(eventId)) throw new InvalidOperationException("Harvest source needs a unique stable identity.");
        profile.Validate(); _plan=profile.Rewards.Compile(catalog); RequiredWork=profile.RequiredWork; _tool=profile.AcceptedTool; EventId=eventId;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // Reserve rewards through the owner before committing completion; retries reuse the same preselected batch.
    // =========================================================
    public HarvestWorkResult Apply(HarvestHit hit)
    {
        if (_plan==null || Completed || _completing || IsQueuedForDeletion() || !CanProcess() || Eligible?.Invoke()==false
            || !GodotObject.IsInstanceValid(hit.Source) || !hit.Source.IsInsideTree() || hit.Source.IsQueuedForDeletion()
            || !hit.Source.CanProcess() || !hit.Position.IsFinite() || !hit.Direction.IsFinite() || hit.Tool.Kind!=_tool || !float.IsFinite(hit.Tool.Amount) || hit.Tool.Amount<=0 || hit.Tool.Amount>1000) return HarvestWorkResult.Rejected;
        float next=Mathf.Min(RequiredWork,Progress+hit.Tool.Amount);
        if (next<RequiredWork) { Progress=next; Changed?.Invoke(); return HarvestWorkResult.Progress; }
        _prepared ??= _plan.Roll(EventId); _completing=true;
        bool accepted;
        try { accepted=TryComplete?.Invoke((DropReward[])_prepared.Clone(),hit)==true; }
        finally { _completing=false; }
        if (!accepted) return HarvestWorkResult.Rejected;
        Progress=RequiredWork; Completed=true; Changed?.Invoke(); return HarvestWorkResult.Completed;
    }
    // Release owner callbacks on source unload; logical H4 persistence is deliberately not implemented here.
    // =========================================================
    public override void _ExitTree() { Eligible=null; TryComplete=null; Changed=null; }
}
