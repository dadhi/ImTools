namespace ImTools.Benchmarks;

using CommunityToolkit.HighPerformance.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

/*
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2861/23H2/2023Update/SunValley3)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host] : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=MediumRun  Toolchain=InProcessNoEmitToolchain  IterationCount=15  
LaunchCount=1  WarmupCount=10

| Method               | Count | Mean       | Error     | StdDev    | Rank | Gen0   | Gen1   | Allocated |
|--------------------- |------ |-----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| MemoryOwner_populate | 100   |   623.2 ns |  28.69 ns |  25.43 ns |    1 | 0.5550 | 0.0134 |   3.41 KB |
| ImTools_populate     | 100   | 5,082.8 ns | 620.87 ns | 580.76 ns |    2 | 2.8915 | 0.1221 |  17.73 KB |
*/

public class AntiVirusFriendlyConfig : ManualConfig
{
    public AntiVirusFriendlyConfig()
    {
        AddJob(Job.MediumRun.WithLaunchCount(1)
                  .WithToolchain(InProcessNoEmitToolchain.Instance));
    }
}
[Config(typeof(AntiVirusFriendlyConfig)),
 MemoryDiagnoser,
 Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.Declared), RankColumn,]
public class MemoryOwnerVsHashMap
{
    [Params(100)]
    // [Params(1, 10, 100)]
    public int Count;

    MemoryOwner<SampleClass> MemoryOwner;

    ImHashMap<int, SampleClass> ImHashMap;

    [GlobalSetup]
    public void Setup()
    {
        Count = 100;
        MemoryOwner = MemoryOwner<SampleClass>.Allocate(Count, AllocationMode.Clear);
        ImHashMap = ImHashMap<int, SampleClass>.Empty;
        for (int i = 0; i < Count; i++)
        {
            ImHashMap = ImHashMap.AddSureNotPresent(i, new SampleClass(i));
            MemoryOwner.Span[i] = new SampleClass(i);
        }
    }

    [Benchmark]
    public object MemoryOwner_populate()
    {
        var mo = MemoryOwner<SampleClass>.Allocate(Count, AllocationMode.Clear);
        for (int i = 0; i < Count; i++)
        {
            mo.Span[i] = new SampleClass(i);
        }
        return mo;
    }

    [Benchmark]
    public object ImTools_populate()
    {
        var imt = ImHashMap<int, SampleClass>.Empty;
        for (int i = 0; i < Count; i++)
        {
            imt = imt.AddSureNotPresent(i, new SampleClass(i));
        }
        return imt;
    }

    // [Benchmark]
    public long MemoryOwner_findBy()
    {
        long sum = 0;
        for (int i = 0; i < Count; i++)
        {
            if (MemoryOwner.Span[i] is { } fnd)
            {

                var res = fnd.TargetValue;
            }
        }
        return sum;
    }

    // [Benchmark]
    public long ImTools_tryfind()
    {
        long sum = 0;
        for (int i = 0; i < Count; i++)
        {
            if (ImHashMap.TryFind(i, out SampleClass fnd))
            {
                sum += fnd.TargetValue;
            }
        }
        return sum;
    }

    // [Benchmark]
    public long ImTools_GetEntryOrNull()
    {
        long sum = 0;
        for (int i = 0; i < Count; i++)
        {
            if (ImHashMap.GetEntryOrDefault(i) is { } fnd)
            {
                sum += fnd.GetSurePresent(i).Value.TargetValue;
            }
        }
        return sum;
    }
}

class SampleClass
{
    public SampleClass(int targetValue) { TargetValue = targetValue; }
    public int TargetValue { get; }

}