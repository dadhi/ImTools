using System;
using System.Linq;
using System.Collections.Generic;
using CsCheck;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

[TestFixture]
public class MapSlimTests
{
    [Test]
    public void Can_add_and_get_3_items_with_arbitrary_hash()
    {
        var map = new MapSlim<string, string>();

        map.AddOrUpdate("a", "a");
        map.AddOrUpdate("b", "b");
        map.AddOrUpdate("c", "c");

        map.TryGetValue("b", out var b);
        Assert.AreEqual("b", b);

        map.TryGetValue("c", out var c);
        Assert.AreEqual("c", c);

        map.TryGetValue("a", out var a);
        Assert.AreEqual("a", a);
    }

    [Test]
    public void Can_add_and_get_3_items_with_the_same_conflicting_hash()
    {
        var map = new MapSlim<string, string>();

        map.AddOrUpdate("a", 1, "a");
        map.AddOrUpdate("b", 1, "b");
        map.AddOrUpdate("c", 1, "c");

        map.TryGetValue("b", 1, out var b);
        Assert.AreEqual("b", b);

        map.TryGetValue("c", 1, out var c);
        Assert.AreEqual("c", c);

        map.TryGetValue("a", 1, out var a);
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
            (items, map, _) =>
            {
                foreach (var (k, _) in items) map.IndexOf(k);
            },
            (items, _, dict) =>
            {
                foreach (var (k, _) in items) dict.ContainsKey(k);
            },
            repeat: 100, writeLine: Console.WriteLine);
    }

    // [Test] // todo: @fixme
    // public void MapSlim_Performance_Increment()
    // {
    //     Gen.Int[0, 255].Array
    //     .Select(a => (a, new MapSlim<int, int>(), new Dictionary<int, int>()))
    //     .Faster(
    //         (items, map, _) =>
    //         {
    //             foreach (var b in items)
    //                 mapslim.GetValueOrNullRefWithHash(b)++;
    //         },
    //         (items, _, dict) =>
    //         {
    //             foreach (var b in items)
    //             {
    //                 dict.TryGetValue(b, out var c);
    //                 dict[b] = c + 1;
    //             }
    //         },
    //         repeat: 1000, sigma: 10, writeLine: Console.WriteLine);
    // }
}
