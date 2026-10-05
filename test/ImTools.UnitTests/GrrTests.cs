using System;
using System.Collections.Generic;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
#else
using FastExpressionCompiler.ImTools;
#endif

namespace ImTools.Experiments.UnitTests;

[TestFixture]
public class GrrTests
{
    private static Grr<int, Stack8<int[]>> NewGrr(uint capacity = 0)
    {
        var g = default(Grr<int, Stack8<int[]>>);
        g.Init(capacity);
        return g;
    }

    [Test]
    public void Init_uses_min_capacity_16()
    {
        var g = NewGrr(0);
        Assert.AreEqual(16, g.Capacity);
        Assert.AreEqual(1, g.SegmentCount);
        g.GetSurePresentRef(0) = 42;
        Assert.AreEqual(42, g.GetSurePresentRef(0));
    }

    [Test]
    public void Init_rounds_up_to_power_of_two()
    {
        var g = NewGrr(20);
        Assert.AreEqual(32, g.Capacity);
    }

    [Test]
    public void DoubleCapacity_keeps_existing_values_and_refs_stable()
    {
        var g = NewGrr(16);
        for (var i = 0; i < 16; i++)
            g.GetSurePresentRef(i) = i + 1;

        ref var first = ref g.GetSurePresentRef(0);
        var before = first;

        g.DoubleCapacity();
        Assert.AreEqual(32, g.Capacity);
        Assert.AreEqual(2, g.SegmentCount);

        for (var i = 0; i < 16; i++)
            Assert.AreEqual(i + 1, g.GetSurePresentRef(i));

        Assert.AreEqual(before, first);
        first = 99;
        Assert.AreEqual(99, g.GetSurePresentRef(0));
    }

    [Test]
    public void GrowCapacity_across_multiple_segments_and_random_access()
    {
        var g = NewGrr(16);
        g.GrowCapacity(1000);
        Assert.GreaterOrEqual(g.Capacity, 1000);
        Assert.Greater(g.SegmentCount, 1);

        for (var i = 0; i < 1000; i++)
            g.GetSurePresentRef(i) = i * 3;

        for (var i = 999; i >= 0; i -= 7)
            Assert.AreEqual(i * 3, g.GetSurePresentRef(i));
    }

    [Test]
    public void Indexing_matches_segment_layout_for_various_first_sizes()
    {
        foreach (var firstReq in new uint[] { 1, 16, 17, 32, 64, 100 })
        {
            var g = NewGrr(firstReq);
            var first = g.Capacity;
            Assert.GreaterOrEqual(first, 16);
            Assert.AreEqual(0, first & (first - 1)); // power of two

            while (g.Capacity < first * 16)
                g.DoubleCapacity();

            var n = g.Capacity;
            for (var i = 0; i < n; i++)
                g.GetSurePresentRef(i) = i;

            for (var i = 0; i < n; i++)
                Assert.AreEqual(i, g.GetSurePresentRef(i), $"firstReq={firstReq}, i={i}, cap={n}");
        }
    }

    [Test]
    public void Re_Init_resets_segments()
    {
        var g = NewGrr(16);
        g.DoubleCapacity();
        g.DoubleCapacity();
        Assert.Greater(g.SegmentCount, 1);

        g.Init(16);
        Assert.AreEqual(16, g.Capacity);
        Assert.AreEqual(1, g.SegmentCount);
    }

    [Test]
    public void Enumerator_visits_all_capacity_slots()
    {
        var g = NewGrr(16);
        g.DoubleCapacity(); // capacity 32
        for (var i = 0; i < 32; i++)
            g.GetSurePresentRef(i) = i;

        var list = new List<int>();
        foreach (var x in g)
            list.Add(x);

        Assert.AreEqual(32, list.Count);
        for (var i = 0; i < 32; i++)
            Assert.AreEqual(i, list[i]);
    }

    [Test]
    public void Enumerator_Reset_restarts()
    {
        var g = NewGrr(16);
        for (var i = 0; i < 16; i++)
            g.GetSurePresentRef(i) = i + 10;

        var e = g.GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(10, e.Current);
        e.Reset();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(10, e.Current);
    }
}

[TestFixture]
public class SmallGrrTests
{
    private static SmallGrr<int, Stack8<int>, Stack8<int[]>> New() => default;

    [Test]
    public void Add_stays_on_stack_until_capacity()
    {
        var s = New();
        for (var i = 0; i < 8; i++)
            Assert.AreEqual(i, s.Add(i * 10));

        Assert.AreEqual(8, s.Count);
        Assert.AreEqual(0, s.Rest.Capacity); // never grew rest

        for (var i = 0; i < 8; i++)
            Assert.AreEqual(i * 10, s.GetSurePresentRef(i));
    }

    [Test]
    public void Add_overflow_goes_to_Grr_and_keeps_stack_stable()
    {
        var s = New();
        for (var i = 0; i < 8; i++)
            s.Add(i);

        ref var stack0 = ref s.GetSurePresentRef(0);
        for (var i = 8; i < 100; i++)
            s.Add(i);

        Assert.AreEqual(100, s.Count);
        Assert.GreaterOrEqual(s.Rest.Capacity, 100 - 8);

        for (var i = 0; i < 100; i++)
            Assert.AreEqual(i, s[i]);

        // stack item ref still valid after heap growth
        Assert.AreEqual(0, stack0);
        stack0 = 777;
        Assert.AreEqual(777, s.GetSurePresentRef(0));
    }

    [Test]
    public void Heap_item_refs_remain_stable_across_Rest_growth()
    {
        var s = New();
        for (var i = 0; i < 20; i++)
            s.Add(i * 2);

        ref var heapItem = ref s.GetSurePresentRef(10); // past stack
        Assert.AreEqual(20, heapItem);

        // force more rest growth
        for (var i = 20; i < 500; i++)
            s.Add(i);

        Assert.AreEqual(20, heapItem);
        heapItem = -1;
        Assert.AreEqual(-1, s.GetSurePresentRef(10));
    }

    [Test]
    public void Random_access_matches_SmallList_for_heap_heavy_counts()
    {
        var grr = New();
        var list = default(SmallList<int, Stack8<int>>);

        const int n = 1024;
        for (var i = 0; i < n; i++)
        {
            grr.Add(i ^ 0x5a5a);
            list.Add(i ^ 0x5a5a);
        }

        var rng = new Random(123);
        for (var t = 0; t < 5000; t++)
        {
            var i = rng.Next(n);
            Assert.AreEqual(list.GetSurePresentRef(i), grr.GetSurePresentRef(i));
        }
    }

    [Test]
    public void Clear_and_reuse()
    {
        var s = New();
        for (var i = 0; i < 40; i++)
            s.Add(i);
        var restCap = s.Rest.Capacity;
        s.Clear();
        Assert.AreEqual(0, s.Count);
        Assert.AreEqual(restCap, s.Rest.Capacity); // segments kept

        s.Add(5);
        Assert.AreEqual(1, s.Count);
        Assert.AreEqual(5, s[0]);
    }

    [Test]
    public void Enumerator_and_TryGetIndex()
    {
        var s = New();
        for (var i = 0; i < 30; i++)
            s.Add(i * 3);

        var sum = 0;
        foreach (var x in s)
            sum += x;
        Assert.AreEqual(3 * 29 * 30 / 2, sum);

        Assert.AreEqual(10, s.TryGetIndex(30, default(FastExpressionCompiler.ImTools.IntEq)));
        Assert.AreEqual(-1, s.TryGetIndex(1, default(FastExpressionCompiler.ImTools.IntEq)));
        Assert.AreEqual(30, s.GetIndexOrAdd(999, default(FastExpressionCompiler.ImTools.IntEq)));
    }

    [Test]
    public void RemoveLast()
    {
        var s = New();
        s.Add(1);
        s.Add(2);
        s.RemoveLastSurePresentItem();
        Assert.AreEqual(1, s.Count);
        Assert.AreEqual(1, s.GetLastSurePresentItem());
    }
}
