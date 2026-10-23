using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Fec = FastExpressionCompiler.ImTools;
using Im = ImTools;

namespace ImTools.Benchmarks;

/// <summary>
/// FEC SmallMap (stack entries + Grr or single-array heap overflow) vs ImTools HSmallMap and Dictionary.
/// Run: dotnet run -c Release -f net10.0 -- --filter *SmallMapVsHSmallMap* [--runtimes net10.0 net11.0 net8.0]
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 3, iterationCount: 10)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SmallMapVsHSmallMapBenchmarks
{
    // 4/8/16 are the stack-only zone of SmallMap16, 32+ spill to the heap and the metadata table.
    [Params(4, 8, 16, 32, 128, 1024)]
    public int Count;

    private int[] _keys = null!;
    private int[] _missingKeys = null!;

    private Im.HSmallMap<int, int, Im.HSmallMap.GoldenIntEq, Im.HSmallMap.SingleArrayEntries<int, int, Im.HSmallMap.GoldenIntEq>> _hMap;
    private Fec.SmallMap16<int, int, Fec.IntEq> _grr16;
    private Fec.SmallMap16_SingleArrEntries<int, int, Fec.IntEq> _single16;
    private Fec.SmallMap8<int, int, Fec.IntEq> _grr8;
    private Dictionary<int, int> _dict = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        var seen = new HashSet<int>();
        _keys = new int[Count];
        for (var i = 0; i < Count;)
            if (seen.Add(_keys[i] = rng.Next(1, int.MaxValue)))
                ++i;

        _missingKeys = new int[Count];
        for (var i = 0; i < Count;)
        {
            var k = rng.Next(1, int.MaxValue);
            if (seen.Add(k))
                _missingKeys[i++] = k;
        }

        _hMap = Im.HSmallMap.New<int, int, Im.HSmallMap.GoldenIntEq>();
        _grr16 = new Fec.SmallMap16<int, int, Fec.IntEq>();
        _single16 = new Fec.SmallMap16_SingleArrEntries<int, int, Fec.IntEq>();
        _grr8 = new Fec.SmallMap8<int, int, Fec.IntEq>();
        _dict = new Dictionary<int, int>();
        foreach (var k in _keys)
        {
            _hMap.AddOrUpdate(k, k);
            Fec.SmallMap.AddOrUpdate(ref _grr16.Map, k, k);
            Fec.SmallMap.AddOrUpdate(ref _single16.Map, k, k);
            Fec.SmallMap.AddOrUpdate(ref _grr8.Map, k, k);
            _dict[k] = k;
        }
    }

    #region Populate

    [BenchmarkCategory("Populate"), Benchmark(Baseline = true)]
    public int Populate_HSmallMap()
    {
        var m = Im.HSmallMap.New<int, int, Im.HSmallMap.GoldenIntEq>();
        foreach (var k in _keys)
            m.AddOrUpdate(k, k);
        return m.Count;
    }

    [BenchmarkCategory("Populate"), Benchmark]
    public int Populate_SmallMap16_Grr()
    {
        var m = new Fec.SmallMap16<int, int, Fec.IntEq>();
        foreach (var k in _keys)
            Fec.SmallMap.AddOrUpdate(ref m.Map, k, k);
        return m.Map.Count;
    }

    [BenchmarkCategory("Populate"), Benchmark]
    public int Populate_SmallMap16_SingleArray()
    {
        var m = new Fec.SmallMap16_SingleArrEntries<int, int, Fec.IntEq>();
        foreach (var k in _keys)
            Fec.SmallMap.AddOrUpdate(ref m.Map, k, k);
        return m.Map.Count;
    }

    [BenchmarkCategory("Populate"), Benchmark]
    public int Populate_SmallMap8_Grr()
    {
        var m = new Fec.SmallMap8<int, int, Fec.IntEq>();
        foreach (var k in _keys)
            Fec.SmallMap.AddOrUpdate(ref m.Map, k, k);
        return m.Map.Count;
    }

    [BenchmarkCategory("Populate"), Benchmark]
    public int Populate_Dictionary()
    {
        var m = new Dictionary<int, int>();
        foreach (var k in _keys)
            m[k] = k;
        return m.Count;
    }

    #endregion

    #region Lookup hit

    [BenchmarkCategory("LookupHit"), Benchmark(Baseline = true)]
    public int LookupHit_HSmallMap()
    {
        var sum = 0;
        foreach (var k in _keys)
            if (_hMap.TryGetValue(k, out var v)) sum += v;
        return sum;
    }

    [BenchmarkCategory("LookupHit"), Benchmark]
    public int LookupHit_SmallMap16_Grr()
    {
        var sum = 0;
        foreach (var k in _keys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _grr16.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupHit"), Benchmark]
    public int LookupHit_SmallMap16_SingleArray()
    {
        var sum = 0;
        foreach (var k in _keys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _single16.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupHit"), Benchmark]
    public int LookupHit_SmallMap8_Grr()
    {
        var sum = 0;
        foreach (var k in _keys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _grr8.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupHit"), Benchmark]
    public int LookupHit_Dictionary()
    {
        var sum = 0;
        foreach (var k in _keys)
            if (_dict.TryGetValue(k, out var v)) sum += v;
        return sum;
    }

    #endregion

    #region Lookup miss

    [BenchmarkCategory("LookupMiss"), Benchmark(Baseline = true)]
    public int LookupMiss_HSmallMap()
    {
        var sum = 0;
        foreach (var k in _missingKeys)
            if (_hMap.TryGetValue(k, out var v)) sum += v;
        return sum;
    }

    [BenchmarkCategory("LookupMiss"), Benchmark]
    public int LookupMiss_SmallMap16_Grr()
    {
        var sum = 0;
        foreach (var k in _missingKeys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _grr16.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupMiss"), Benchmark]
    public int LookupMiss_SmallMap16_SingleArray()
    {
        var sum = 0;
        foreach (var k in _missingKeys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _single16.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupMiss"), Benchmark]
    public int LookupMiss_SmallMap8_Grr()
    {
        var sum = 0;
        foreach (var k in _missingKeys)
            sum += Fec.SmallMap.GetValueOrDefault(ref _grr8.Map, k, -1);
        return sum;
    }

    [BenchmarkCategory("LookupMiss"), Benchmark]
    public int LookupMiss_Dictionary()
    {
        var sum = 0;
        foreach (var k in _missingKeys)
            if (_dict.TryGetValue(k, out var v)) sum += v;
        return sum;
    }

    #endregion

    #region Enumerate by insertion order

    [BenchmarkCategory("Enumerate"), Benchmark(Baseline = true)]
    public int Enumerate_HSmallMap()
    {
        var sum = 0;
        var e = _hMap.Entries;
        for (var i = 0; i < e.GetCount(); ++i)
            sum += e.GetSurePresentEntryRef(i).Value;
        return sum;
    }

    [BenchmarkCategory("Enumerate"), Benchmark]
    public int Enumerate_SmallMap16_Grr()
    {
        var sum = 0;
        ref var m = ref _grr16.Map;
        for (var i = 0; i < m.Count; ++i)
            sum += m.GetSurePresentEntryRef(i).Value;
        return sum;
    }

    [BenchmarkCategory("Enumerate"), Benchmark]
    public int Enumerate_SmallMap16_SingleArray()
    {
        var sum = 0;
        ref var m = ref _single16.Map;
        for (var i = 0; i < m.Count; ++i)
            sum += m.GetSurePresentEntryRef(i).Value;
        return sum;
    }

    #endregion
}
