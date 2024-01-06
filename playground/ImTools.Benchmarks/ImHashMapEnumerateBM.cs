namespace ImTools.Benchmarks;

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

/*
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2861/23H2/2023Update/SunValley3)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                           | ItemCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Enumerate_with_condition_stopper | 100       | 1.209 us | 0.0239 us | 0.0406 us | 1.203 us |  1.00 |    0.00 |      - |         - |          NA |
| ForEach_with_condition_stopper   | 100       | 1.334 us | 0.0265 us | 0.0669 us | 1.312 us |  1.12 |    0.08 | 0.0267 |     168 B |          NA |
*/

[MemoryDiagnoser]
public class ImHashMapEnumerateBM
{
    [Params(100)]
    public int ItemCount;

    [GlobalSetup]
    public void Populate()
    {
        _map = ImHashMap_AddOrUpdate();
    }

    [Benchmark(Baseline = true)]
    public object Enumerate_with_condition_stopper()
    {
        var needle = "b";
        foreach (var entry in _map.Enumerate())
        {
            if (entry.Value == needle)
                return entry.Key.Name;
        }
        return "X";
    }

    [Benchmark]
    public object ForEach_with_condition_stopper()
    {
        var needle = "b";
        var entry = _map.FindFirstOrDefault(ref needle,
            static (ref string n, ImHashMapEntry<Type, string> e, int i) => n == e.Value);
        return entry != null ? entry.Key.Name : "X";
    }

    private static readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

    private ImHashMap<Type, string> _map;

    public ImHashMap<Type, string> ImHashMap_AddOrUpdate()
    {
        var map = ImHashMap<Type, string>.Empty;

        var i = 0;
        foreach (var key in _keys.Take(ItemCount))
            map = map.AddOrUpdate(key.GetHashCode(), key, ++i == ItemCount ? "b" : "a");

        return map;
    }

}