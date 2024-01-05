namespace ImTools.Playground;

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

[MemoryDiagnoser]
public class ImHashMapEnumerateBM
{
    [Params(10)]
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