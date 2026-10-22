// Opt-in main-thread callback tracing. Fixed storage and value-type scopes avoid per-callback allocations.
// This measures instrumented CPU wall time; it is not a sampling profiler or a GPU/whole-engine breakdown.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
public static class RuntimeFrameTrace
{
    private const int Capacity = 96;
    private static readonly Dictionary<string,int> Names = new();
    private static readonly long[] SelfTicks = new long[Capacity], ChildTicks = new long[64];
    private static readonly int[] Calls = new int[Capacity], LastCalls = new int[Capacity];
    private static readonly long[] LastTicks = new long[Capacity];
    private static readonly long[] WorstTicks = new long[Capacity];
    private static readonly int[] WorstCalls = new int[Capacity];
    private static ulong _frame, _worstFrame, _owner;
    private static long _started, _allocated;
    private static int _depth, _session, _gc0, _gc1, _gc2;
    private static double _lastInterval, _lastMeasured, _worstInterval, _worstMeasured;
    private static long _lastBytes, _worstBytes;
    private static (int Gen0,int Gen1,int Gen2) _lastGc, _worstGc;
    public static bool Enabled { get; private set; }
    public static double SlowFrameMilliseconds { get; set; } = 25;
    public static int CapturedFrames { get; private set; }
    public static int SlowFrames { get; private set; }

    // One removable debug owner controls a capture; stopping it never changes game simulation.
    // =========================================================
    public static void Start(ulong owner)
    {
        _owner = owner; _session++; Enabled = true; Reset();
    }
    public static void Stop(ulong owner) { if (_owner == owner) { Enabled = false; _session++; } }
    public static void Release(ulong owner)
    {
        if (_owner != owner) return;
        Stop(owner); Reset(); _owner = 0;
    }
    public static void Reset()
    {
        _session++; _frame = ulong.MaxValue; _depth = 0; CapturedFrames = SlowFrames = 0;
        _worstInterval = _worstMeasured = _lastInterval = _lastMeasured = 0;
        Names.Clear(); Array.Clear(SelfTicks); Array.Clear(Calls); Array.Clear(WorstTicks); Array.Clear(WorstCalls);
    }

    // Nested scopes subtract child durations, so summing callbacks does not count shadows twice.
    // =========================================================
    public static Scope Measure(string name)
    {
        if (!Enabled) return default;
        long now = Stopwatch.GetTimestamp(); ulong frame = Engine.GetProcessFrames();
        if (_frame != frame)
        {
            if (_frame != ulong.MaxValue) Capture(now);
            _frame = frame; _started = now; _allocated = GC.GetAllocatedBytesForCurrentThread();
            _gc0 = GC.CollectionCount(0); _gc1 = GC.CollectionCount(1); _gc2 = GC.CollectionCount(2);
            Array.Clear(SelfTicks); Array.Clear(Calls); _depth = 0;
        }
        if (!Names.TryGetValue(name,out int id))
        { if (Names.Count == Capacity) return default; id = Names.Count; Names.Add(name,id); }
        if (_depth >= ChildTicks.Length) return default;
        int depth = _depth++; ChildTicks[depth] = 0;
        return new Scope(id,depth,_session,Stopwatch.GetTimestamp());
    }
    public readonly struct Scope : IDisposable
    {
        private readonly int _id, _depth, _session;
        private readonly long _start;
        internal Scope(int id,int depth,int session,long start) { _id=id; _depth=depth; _session=session; _start=start; }
        public void Dispose()
        {
            if (_start == 0 || _session != RuntimeFrameTrace._session) return;
            long elapsed = Stopwatch.GetTimestamp()-_start;
            SelfTicks[_id] += Math.Max(0,elapsed-ChildTicks[_depth]); Calls[_id]++;
            RuntimeFrameTrace._depth = _depth;
            if (_depth > 0) ChildTicks[_depth-1] += elapsed;
        }
    }

    // Preserve the worst slow frame's entire measured breakdown, rather than unrelated last values.
    // Allocation/GC indicators span the same callback-to-callback frame interval, including uninstrumented work.
    // =========================================================
    private static void Capture(long now)
    {
        _lastInterval = (now-_started)*1000.0/Stopwatch.Frequency;
        _lastMeasured = SelfTicks.Sum()*1000.0/Stopwatch.Frequency;
        _lastBytes = Math.Max(0,GC.GetAllocatedBytesForCurrentThread()-_allocated);
        _lastGc = (GC.CollectionCount(0)-_gc0,GC.CollectionCount(1)-_gc1,GC.CollectionCount(2)-_gc2);
        Array.Copy(SelfTicks,LastTicks,Capacity); Array.Copy(Calls,LastCalls,Capacity);
        CapturedFrames++;
        if (_lastInterval < SlowFrameMilliseconds) return;
        SlowFrames++;
        if (_lastInterval <= _worstInterval) return;
        _worstFrame = _frame; _worstInterval = _lastInterval; _worstMeasured = _lastMeasured;
        _worstBytes = _lastBytes; _worstGc = _lastGc;
        Array.Copy(SelfTicks,WorstTicks,Capacity); Array.Copy(Calls,WorstCalls,Capacity);
    }

    // Format only on demand at the debug UI's low refresh rate, or when copying a report.
    // =========================================================
    public static string Report()
    {
        if (CapturedFrames == 0) return "Trace: enable and walk around to capture frames.";
        var text = new StringBuilder($"C# trace: {CapturedFrames} frames, {SlowFrames} above {SlowFrameMilliseconds:F0}ms\n"
            + $"Latest interval {_lastInterval:F2}ms; measured callbacks {_lastMeasured:F2}ms\n"
            + $"Main-thread allocation {_lastBytes/1024.0:F1} KiB; GC 0/1/2: {_lastGc.Gen0}/{_lastGc.Gen1}/{_lastGc.Gen2}\n");
        bool worst = _worstInterval > 0;
        text.AppendLine(worst ? $"Worst slow frame #{_worstFrame}: {_worstInterval:F2}ms interval / {_worstMeasured:F2}ms measured\nAllocation {_worstBytes/1024.0:F1} KiB; GC 0/1/2: {_worstGc.Gen0}/{_worstGc.Gen1}/{_worstGc.Gen2}" : "No slow frame captured; latest callback breakdown:");
        var ticks = worst ? WorstTicks : LastTicks; var calls = worst ? WorstCalls : LastCalls;
        foreach (var pair in Names.OrderByDescending(p => ticks[p.Value]).Take(10))
            if (calls[pair.Value] > 0) text.AppendLine($"{pair.Key}: {ticks[pair.Value]*1000.0/Stopwatch.Frequency:F2}ms ({calls[pair.Value]} calls)");
        text.Append("Intervals include waits/engine work. Callback times are exclusive and include native calls inside those callbacks. Unmeasured time is not proof of C# or GC cost.");
        return text.ToString();
    }
}
