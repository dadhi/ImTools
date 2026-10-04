#define VERIFY_MAP
#if NET6_0_OR_GREATER
#define CS_CHECK
#endif

namespace FastExpressionCompiler.ImTools.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static SmallMap;
#if CS_CHECK
using CsCheck;
#endif

/// <summary>
/// Baseline tests for the hybrid SmallMap (stack entries + Grr/SingleArray heap + Grr metadata).
/// Removal/TryRemove is intentionally not covered yet - not implemented in this baseline.
/// </summary>
[TestFixture]
public class FecSmallMapTests
{
    private static readonly Type[] _allKeys = typeof(Dictionary<,>).Assembly.GetTypes().Take(2000).ToArray();

    private static void AssertVerifyKeys<TMap, K>(ref TMap map, IEnumerable<K> expectedKeys = null)
        where TMap : struct, IMap<K>, IMapImpl<K>
    {
#if VERIFY_MAP
        map.Verify(static (cond, msg) => Assert.IsTrue(cond, msg), expectedKeys, Pass<K>.It);
#endif
    }

    [Test]
    public void Zero_0_hash_test()
    {
        var m = new SmallMap16<int, int, IntEq>();
        ref var map = ref m.Map;
        map.AddOrUpdate(1, 1);
        Assert.AreEqual(1, map.Count);

        Assert.False(map.ContainsKey(0));
        map.AddOrUpdate(0, 0);
        Assert.AreEqual(2, map.Count);
        Assert.True(map.ContainsKey(0));
        Assert.AreEqual(0, map.GetValueOrDefault(0, 0));
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var m = new SmallMap4<int, string, IntEq>();
        ref var map = ref m.Map;

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(42 + 32 + 32, "3");

        map.AddOrUpdate(43, "a");
        map.AddOrUpdate(43 + 32, "b");
        map.AddOrUpdate(43 + 32 + 32, "c");

        map.AddOrUpdate(42 + 32 + 32 + 32, "4");
        map.AddOrUpdate(44, "*");

        map.AddOrUpdate(42 + 32 + 32 + 32 + 32, "5");
        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32, "6");
        map.AddOrUpdate(43 + 32 + 32 + 32, "d");

        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32, "7");
        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32, "8");

        AssertVerifyKeys(ref map, (IEnumerable<int>)null);

        Assert.AreEqual(null, map.GetValueOrDefault(43 + 32 + 32 + 32 + 32, default(string)));
        Assert.AreEqual("*", map.GetValueOrDefault(44, default(string)));

        Assert.AreEqual("1", map.GetValueOrDefault(42, default(string)));
        Assert.AreEqual("2", map.GetValueOrDefault(42 + 32, default(string)));
        Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32, default(string)));
        Assert.AreEqual("4", map.GetValueOrDefault(42 + 32 + 32 + 32, default(string)));
        Assert.AreEqual("5", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32, default(string)));
        Assert.AreEqual("6", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32, default(string)));
        Assert.AreEqual("7", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32, default(string)));
        Assert.AreEqual("8", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32, default(string)));

        Assert.AreEqual("a", map.GetValueOrDefault(43, default(string)));
        Assert.AreEqual("b", map.GetValueOrDefault(43 + 32, default(string)));
        Assert.AreEqual("c", map.GetValueOrDefault(43 + 32 + 32, default(string)));
        Assert.AreEqual("d", map.GetValueOrDefault(43 + 32 + 32 + 32, default(string)));

        Assert.AreEqual(13, map.Count);
    }

    [Test]
    public void Update_existing_key_keeps_count()
    {
        var m = new SmallMap8<int, string, IntEq>();
        ref var map = ref m.Map;

        Assert.IsFalse(map.AddOrUpdate(7, "a"));
        Assert.IsTrue(map.AddOrUpdate(7, "b"));
        Assert.AreEqual(1, map.Count);
        Assert.AreEqual("b", map.GetValueOrDefault(7, default(string)));
        AssertVerifyKeys(ref map, new[] { 7 });
    }

    [Test]
    public void Default_map_lookups_are_safe()
    {
        // Truly default (uninitialized) map: Count/Capacity 0, no heap metadata yet.
        var m = default(SmallMap16<int, string, IntEq>);
        ref var map = ref m.Map;
        Assert.AreEqual(0, map.Count);
        Assert.IsFalse(map.ContainsKey(42));
        Assert.AreEqual(null, map.GetValueOrDefault(42, default(string)));
        map.TryGetEntryRef(42, out var found);
        Assert.IsFalse(found);
        // Do not dereference the returned ref when found is false (null ref).
    }

    [Test]
    public void Stack_to_heap_migration_and_entry_order()
    {
        var m = new SmallMap4<int, int, IntEq>();
        ref var map = ref m.Map;

        const int n = 40;
        for (var i = 0; i < n; i++)
            map.AddOrUpdate(i, i * 10);

        Assert.AreEqual(n, map.Count);
        Assert.Greater(map.Capacity, 0);
        Assert.Greater(map.HeapEntries.Capacity, 0);

        for (var i = 0; i < n; i++)
        {
            Assert.AreEqual(i, map.GetSurePresentKey(i));
            Assert.AreEqual(i * 10, map.GetSurePresentEntryRef(i).Value);
            Assert.AreEqual(i * 10, map.GetValueOrDefault(i, 0));
        }

        AssertVerifyKeys(ref map, Enumerable.Range(0, n));
    }

    [Test]
    public void Heap_entry_refs_remain_stable_with_Grr_backing()
    {
        var m = new SmallMap4<int, int, IntEq>();
        ref var map = ref m.Map;

        for (var i = 0; i < 8; i++)
            map.AddOrUpdate(i, i);

        ref var heapEntry = ref map.GetSurePresentEntryRef(4);
        Assert.AreEqual(4, heapEntry.Key);
        Assert.AreEqual(4, heapEntry.Value);

        for (var i = 8; i < 200; i++)
            map.AddOrUpdate(i, i);

        Assert.AreEqual(4, heapEntry.Key);
        Assert.AreEqual(4, heapEntry.Value);
        heapEntry.Value = -1;
        Assert.AreEqual(-1, map.GetSurePresentEntryRef(4).Value);
        Assert.AreEqual(-1, map.GetValueOrDefault(4, 0));

        AssertVerifyKeys(ref map, (IEnumerable<int>)null);
    }

    [Test]
    public void SingleBackingArray_map_behaves_the_same_for_lookup()
    {
        var m = new SmallMap16_SingleArrEntries<int, string, IntEq>();
        ref var map = ref m.Map;

        for (var i = 0; i < 100; i++)
            map.AddOrUpdate(i, "v" + i);

        for (var i = 0; i < 100; i++)
            Assert.AreEqual("v" + i, map.GetValueOrDefault(i, default(string)));

        Assert.IsFalse(map.ContainsKey(1000));
        AssertVerifyKeys(ref map, Enumerable.Range(0, 100));
    }

    [Test]
    public void AddSureAbsent_and_AddOrGet_paths()
    {
        var m = new SmallMap8<int, int, IntEq>();
        ref var map = ref m.Map;

        map.AddSureAbsentDefaultEntryAndGetRef(1).Value = 10;
        map.AddSureAbsentDefaultEntryAndGetRef(2).Value = 20;
        Assert.AreEqual(2, map.Count);

        ref var existing = ref map.AddOrGetEntryRef(1, out var found);
        Assert.IsTrue(found);
        Assert.AreEqual(10, existing.Value);
        existing.Value = 11;

        ref var added = ref map.AddOrGetEntryRef(3, out found);
        Assert.IsFalse(found);
        added.Value = 30;

        Assert.AreEqual(3, map.Count);
        Assert.AreEqual(11, map.GetValueOrDefault(1, 0));
        Assert.AreEqual(30, map.GetValueOrDefault(3, 0));
        AssertVerifyKeys(ref map, new[] { 1, 2, 3 });
    }

    [Test]
    public void Constructor_capacity_is_honored_as_power_of_two_floor()
    {
        var m = new SmallMap16<int, int, IntEq>(100);
        ref var map = ref m.Map;
        Assert.GreaterOrEqual(map.Capacity, 16);
        Assert.AreEqual(0, map.Capacity & (map.Capacity - 1));

        for (var i = 0; i < 50; i++)
            map.AddOrUpdate(i, i);

        Assert.AreEqual(50, map.Count);
        AssertVerifyKeys(ref map, Enumerable.Range(0, 50));
    }

    [Test]
    public void Set_stores_keys_only()
    {
        var s = new SmallSet8<int, IntEq>();
        ref var set = ref s.Set;

        set.AddOrGetEntryRef(5, out var found);
        Assert.IsFalse(found);
        set.AddOrGetEntryRef(5, out found);
        Assert.IsTrue(found);
        Assert.IsTrue(set.ContainsKey(5));
        Assert.IsFalse(set.ContainsKey(6));
        Assert.AreEqual(1, set.Count);
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_10_items()
    {
        const int Count = 10;
        var m = new SmallMap16<Type, string, RefEq<Type>>();
        ref var map = ref m.Map;

        var presentKeys = _allKeys.Take(Count).ToArray();
        var missingKeys = _allKeys.Skip(1000).Take(Count).ToArray();
        var seed = new Random(42);
        var randomPresentKeys = presentKeys.OrderBy(_ => seed.Next()).ToArray();

        foreach (var key in presentKeys)
            map.AddOrUpdate(key, "a");

        var count = 0;
        var iters = Count / 2;
        for (var i = 0; i < iters; ++i)
        {
            ref var entry = ref map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += entry.Value.Length;
            _ = map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }

        Assert.AreEqual(0, count);
        AssertVerifyKeys(ref map, presentKeys);
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_100_items()
    {
        const int Count = 100;
        var m = new SmallMap16<Type, string, RefEq<Type>>();
        ref var map = ref m.Map;

        var presentTypes = _allKeys.Take(Count).ToArray();
        var missingKeys = _allKeys.Skip(1000).Take(Count).ToArray();
        var seed = new Random(42);
        var randomPresentKeys = presentTypes.OrderBy(_ => seed.Next()).ToArray();

        foreach (var type in presentTypes)
            map.AddOrUpdate(type, "a");

        AssertVerifyKeys(ref map, presentTypes);

        var count = 0;
        var iters = Count / 2;
        for (var i = 0; i < iters; ++i)
        {
            ref var result = ref map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += result.Value.Length;
            _ = ref map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_1000_items()
    {
        const int Count = 1000;
        var m = new SmallMap16<Type, string, RefEq<Type>>();
        ref var map = ref m.Map;

        var presentTypes = _allKeys.Take(Count).ToArray();
        var missingKeys = _allKeys.Skip(1000).Take(Count).ToArray();
        var seed = new Random(42);
        var randomPresentKeys = presentTypes.OrderBy(_ => seed.Next()).ToArray();

        foreach (var type in presentTypes)
            map.AddOrUpdate(type, "a");

        AssertVerifyKeys(ref map, presentTypes);

        var count = 0;
        var iters = Count / 2;
        for (var i = 0; i < iters; ++i)
        {
            ref var result = ref map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += result.Value.Length;
            _ = ref map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Colliding_hashes_still_roundtrip()
    {
        var m = new SmallMap8<int, string, IntEq>();
        ref var map = ref m.Map;

        var keys = new[] { 1, 1 + 16, 1 + 32, 1 + 48, 1 + 64, 1 + 80, 1 + 96, 1 + 112, 1 + 128, 1 + 144 };
        for (var i = 0; i < keys.Length; i++)
            map.AddOrUpdate(keys[i], "v" + i);

        for (var i = 0; i < keys.Length; i++)
            Assert.AreEqual("v" + i, map.GetValueOrDefault(keys[i], default(string)));

        Assert.AreEqual(keys.Length, map.Count);
        AssertVerifyKeys(ref map, keys);
    }

    [Test]
    public void Multiple_resizes_keep_all_keys_findable()
    {
        // Force several metadata resizes (threshold ~7/8 full) past stack migration.
        var m = new SmallMap4<int, int, IntEq>();
        ref var map = ref m.Map;
        const int n = 5000;
        for (var i = 0; i < n; i++)
            map.AddOrUpdate(i, i);

        Assert.AreEqual(n, map.Count);
        Assert.Greater(map.Capacity, 16);
        for (var i = 0; i < n; i++)
            Assert.AreEqual(i, map.GetValueOrDefault(i, -1));
        Assert.AreEqual(-1, map.GetValueOrDefault(n, -1));
        AssertVerifyKeys(ref map, Enumerable.Range(0, n));
    }

#if CS_CHECK
    [Test]
    public void Check_AddOrUpdate_random_items_and_verify_all_added()
    {
        const int upperBound = 100_000;
        Gen.Int[0, upperBound].Array.Sample(items =>
        {
            var m = new SmallMap16<int, int, IntEq>();
            ref var map = ref m.Map;
            foreach (var n in items)
            {
                map.AddOrUpdate(n, n);
                Assert.AreEqual(n, map.GetValueOrDefault(n, -1));
            }

            foreach (var n in items)
                Assert.AreEqual(n, map.GetValueOrDefault(n, -1));

            Assert.AreEqual(-1, map.GetValueOrDefault(upperBound + 1, -1));
            Assert.AreEqual(-1, map.GetValueOrDefault(-1, -1));
            AssertVerifyKeys(ref map, items.Distinct());
        },
        iter: 2000);
    }

    [Test]
    public void Check_AddOrUpdate_unique_keys_count_and_lookup()
    {
        Gen.Int[0, 50_000].ArrayUnique.Sample(keys =>
        {
            var m = new SmallMap8<int, int, IntEq>();
            ref var map = ref m.Map;
            for (var i = 0; i < keys.Length; i++)
                map.AddOrUpdate(keys[i], i);

            Assert.AreEqual(keys.Length, map.Count);
            for (var i = 0; i < keys.Length; i++)
                Assert.AreEqual(i, map.GetValueOrDefault(keys[i], -1));

            AssertVerifyKeys(ref map, keys);
        },
        iter: 1500);
    }

    [Test]
    public void Check_colliding_int_keys_across_resizes()
    {
        // Same low bits (ideal index) force long RH chains and wrap cases before/after resize.
        Gen.Int[1, 64].SelectMany(baseKey =>
            Gen.Int[8, 400].Select(n => (baseKey, n)))
            .Sample(t =>
            {
                var (baseKey, n) = t;
                var m = new SmallMap4<int, int, IntEq>();
                ref var map = ref m.Map;
                var keys = new int[n];
                for (var i = 0; i < n; i++)
                {
                    // Step by growing powers of two so ideals collide under successive masks.
                    keys[i] = baseKey + i * 16;
                    map.AddOrUpdate(keys[i], i);
                }

                Assert.AreEqual(n, map.Count);
                for (var i = 0; i < n; i++)
                    Assert.AreEqual(i, map.GetValueOrDefault(keys[i], -1));
                AssertVerifyKeys(ref map, keys);
            },
            iter: 1000);
    }
#endif
}
