using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CsCheck;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

[TestFixture]
public class MapSlimTests
{
    [Test]
    public void Can_add_and_get_key_value()
    {
        var map = new MapSlim<string, string>();

        map.AddItem("a", "a", "a".GetHashCode());
        map.AddItem("b", "b", "b".GetHashCode());
        map.AddItem("c", "c", "c".GetHashCode());

        ref var b = ref map.GetValueOrNullRef("b");
        Assert.AreEqual("b", b);

        ref var c = ref map.GetValueOrNullRef("c");
        Assert.AreEqual("c", c);

        ref var a = ref map.GetValueOrNullRef("a");
        Assert.AreEqual("a", a);
    }

    [Test]
    public void MapSlim_ModelBased()
    {
        Gen.Dictionary(Gen.Int, Gen.Byte)
        .Select(d => (new MapSlim<int, byte>(d), new Dictionary<int, byte>(d)))
        .SampleModelBased(
            Gen.Select(Gen.Int[0, 100], Gen.Byte).Operation<MapSlim<int, byte>, Dictionary<int, byte>>((m, d, t) =>
            {
                m[t.Item1] = t.Item2;
                d[t.Item1] = t.Item2;
            })
        );
    }

    [Test]
    public void MapSlim_Metamorphic()
    {
        Gen.Dictionary(Gen.Int, Gen.Byte).Select(d => new MapSlim<int, byte>(d))
        .SampleMetamorphic(
            Gen.Select(Gen.Int[0, 100], Gen.Byte, Gen.Int[0, 100], Gen.Byte).Metamorphic<MapSlim<int, byte>>(
                (d, t) => { d[t.Item1] = t.Item2; d[t.Item3] = t.Item4; },
                (d, t) => { if (t.Item1 == t.Item3) { d[t.Item3] = t.Item4; } else { d[t.Item3] = t.Item4; d[t.Item1] = t.Item2; } }
            )
        );
    }

    [Test]
    public void MapSlim_Concurrency()
    {
        Gen.Dictionary(Gen.Int, Gen.Byte).Select(d => new MapSlim<int, byte>(d))
        .SampleConcurrent(
            Gen.Int.Select(Gen.Byte).Operation<MapSlim<int, byte>>((m, t) => { lock (m) m[t.Item1] = t.Item2; }),
            Gen.Int.NonNegative.Operation<MapSlim<int, byte>>((m, i) => { if (i < m.Count) { var _ = m.Key(i); } }),
            Gen.Int.Operation<MapSlim<int, byte>>((m, i) => { var _ = m.IndexOf(i); }),
            Gen.Operation<MapSlim<int, byte>>(m => { var _ = m.ToArray(); })
        );
    }

    [Test]
    public void MapSlim_Performance_Add()
    {
        Gen.Int.Select(Gen.Byte).Array
        .Faster(
            items =>
            {
                var m = new MapSlim<int, byte>();
                foreach (var (k, v) in items) m[k] = v;
            },
            items =>
            {
                var m = new Dictionary<int, byte>();
                foreach (var (k, v) in items) m[k] = v;
            },
            repeat: 100, raiseexception: false, writeLine: Console.WriteLine);
    }

    // [Test] // todo: @fixme
    public void MapSlim_Performance_IndexOf()
    {
        Gen.Dictionary(Gen.Int, Gen.Byte)
        .Select(a => (a, new MapSlim<int, byte>(a), new Dictionary<int, byte>(a)))
        .Faster(
            (items, mapslim, _) =>
            {
                foreach (var (k, _) in items) mapslim.IndexOf(k);
            },
            (items, _, dict) =>
            {
                foreach (var (k, _) in items) dict.ContainsKey(k);
            },
            repeat: 100, writeLine: Console.WriteLine);
    }

    // [Test] // todo: @fixme
    public void MapSlim_Performance_Increment()
    {
        Gen.Int[0, 255].Array
        .Select(a => (a, new MapSlim<int, int>(), new Dictionary<int, int>()))
        .Faster(
            (items, mapslim, _) =>
            {
                foreach (var b in items)
                    mapslim.GetValueOrNullRef(b)++;
            },
            (items, _, dict) =>
            {
                foreach (var b in items)
                {
                    dict.TryGetValue(b, out var c);
                    dict[b] = c + 1;
                }
            },
            repeat: 1000, sigma: 10, writeLine: Console.WriteLine);
    }
}
