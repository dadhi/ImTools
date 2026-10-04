using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FastExpressionCompiler.ImTools;

namespace ImTools.Benchmarks;

/// <summary>
/// Random-access benchmark: SmallList (stack + single growing array)
/// vs SmallGrr (stack + stable segmented Grr) once enough items spill to heap.
/// Run under net10.0 host (project TFM). Heap spill starts after Stack8 capacity (8).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 3, iterationCount: 8)]
public class SmallGrrVsSmallListBenchmarks
{
    // Sizes where Rest/Grr is on the hot path (well past stack capacity).
    [Params(256, 1024, 4096)]
    public int Count;

    private int[] _indices = null!;
    private SmallList<int, Stack8<int>> _smallList;
    private SmallGrr<int, Stack8<int>, Stack8<int[]>> _smallGrr;

    [GlobalSetup]
    public void Setup()
    {
        _smallList = default;
        _smallGrr = default;

        for (var i = 0; i < Count; i++)
        {
            _smallList.Add(i);
            _smallGrr.Add(i);
        }

        // Deterministic pseudo-random access pattern (mostly heap indices)
        _indices = new int[Count * 4];
        var rng = new Random(42);
        for (var i = 0; i < _indices.Length; i++)
            _indices[i] = rng.Next(Count);
    }

    [Benchmark(Baseline = true)]
    public int SmallList_RandomAccess()
    {
        var sum = 0;
        var idxs = _indices;
        ref var list = ref _smallList;
        for (var i = 0; i < idxs.Length; i++)
            sum += list.GetSurePresentRef(idxs[i]);
        return sum;
    }

    [Benchmark]
    public int SmallGrr_RandomAccess()
    {
        var sum = 0;
        var idxs = _indices;
        ref var list = ref _smallGrr;
        for (var i = 0; i < idxs.Length; i++)
            sum += list.GetSurePresentRef(idxs[i]);
        return sum;
    }
}
