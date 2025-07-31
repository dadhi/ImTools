// #define VERIFY_MAP

namespace FastExpressionCompiler.ImTools.UnitTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using static SmallMap;

[TestFixture]
public class FecSmallMapTests
{
    private static readonly Type[] _allKeys = typeof(List<>).Assembly.GetTypes().Take(2000).ToArray();

    [Test]
    public void Zero_0_hash_test()
    {
        var map = new SmallMap16<int, int, IntEq>();
        // Make non-empty map
        map.Map.AddOrUpdate(1, 1);
        Assert.AreEqual(1, map.Map.Count);

        Assert.False(map.Map.ContainsKey(0));
        map.Map.AddOrUpdate(0, 0);
        Assert.AreEqual(2, map.Map.Count);
        Assert.True(map.Map.ContainsKey(0));
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_10_items()
    {
        const int Count = 10;
        Debug.Assert(Count <= 1000, "Count should be less than or equal to 1000 for this test to work correctly.");

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
            var entry = map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += entry.Value.Length;
            _ = map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }

        Assert.AreEqual(0, count);
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_100_items()
    {
        const int Count = 100;
        Debug.Assert(Count <= 1000, "Count should be less than or equal to 1000 for this test to work correctly.");

        var m = new SmallMap16<Type, string, RefEq<Type>>();
        ref var map = ref m.Map;

        var presentTypes = _allKeys.Take(Count).ToArray();
        var missingKeys = _allKeys.Skip(1000).Take(Count).ToArray();

        var seed = new Random(42);
        var randomPresentKeys = presentTypes.OrderBy(_ => seed.Next()).ToArray();

        foreach (var type in presentTypes)
            map.AddOrUpdate(type, "a");

#if VERIFY_MAP
        map.Verify(static (cond, msg) => Assert.IsTrue(cond, msg), presentTypes, Pass<Type>.It);
#endif

        var count = 0;
        var iters = Count / 2;
        for (var i = 0; i < iters; ++i)
        {
            var result = map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += result.Value.Length;
            _ = map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }
        Assert.AreEqual(0, count);

#if DEBUG
        Assert.Greater(m.Map.LookupGreaterProbeCheckCount + m.Map.LookupEqualProbeCheckCount, 200);
#endif
    }

    [Test]
    public void Benchmark_test_with_Add_and_Lookup_1000_items()
    {
        const int Count = 1000;
        Debug.Assert(Count <= 1000, "Count should be less than or equal to 1000 for this test to work correctly.");

        var m = new SmallMap16<Type, string, RefEq<Type>>();
        ref var map = ref m.Map;

        var presentTypes = _allKeys.Take(Count).ToArray();
        var missingKeys = _allKeys.Skip(1000).Take(Count).ToArray();

        var seed = new Random(42);
        var randomPresentKeys = presentTypes.OrderBy(_ => seed.Next()).ToArray();

        foreach (var type in presentTypes)
            map.AddOrUpdate(type, "a");

        map.Verify(static (cond, msg) => Assert.IsTrue(cond, msg), presentTypes, Pass<Type>.It);

        var count = 0;
        var iters = Count / 2;
        for (var i = 0; i < iters; ++i)
        {
            var result = map.TryGetEntryRef(randomPresentKeys[i], out var found);
            if (found)
                count += result.Value.Length;
            _ = map.TryGetEntryRef(missingKeys[i], out found);
            if (!found)
                --count;
        }
        Assert.AreEqual(0, count);

#if DEBUG
        Assert.Greater(m.Map.LookupGreaterProbeCheckCount + m.Map.LookupEqualProbeCheckCount, 2000);
#endif
    }

    // [Test]
    // public void Real_world_test_AddOrUpdate_NO_Resize()
    // {
    //     var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

    //     var map = new SmallMap16<Type, string, RefEq<Type>>(8);

    //     foreach (var key in types)
    //         map.AddOrUpdate(key, "a");

    //     map.AddOrUpdate(typeof(FHashMap11Tests), "!");

    //     Assert.AreEqual(101, map.Count);

    //     Verify(map, types);
    // }

    // [Test]
    // public void Real_world_test_with_TryRemove_from_1000_items()
    // {
    //     var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

    //     var map = new SmallMap16<Type, string, RefEq<Type>>();

    //     foreach (var key in types)
    //         map.AddOrUpdate(key, "a");

    //     map.AddOrUpdate(typeof(FHashMap11Tests), "!");
    //     Assert.AreEqual(1001, map.Count);

    //     Assert.IsTrue(map.TryRemove(typeof(FHashMap11Tests)));
    //     Assert.AreEqual(1000, map.Count);

    //     Verify(map, types);
    // }

    // [Test]
    // public void Real_world_test_with_Enumerator_and_TryRemove_the_entries()
    // {
    //     var count = 1000;
    //     var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(count).ToList();
    //     Assert.AreEqual(count, types.Count);

    //     var map = new SmallMap16<Type, string, RefEq<Type>>();

    //     foreach (var key in types)
    //         map.AddOrUpdate(key, "a");

    //     var keys = map.Select(kv => kv.Key).ToList();
    //     CollectionAssert.AreEquivalent(types, keys);

    //     Assert.IsTrue(map.TryRemove(types[0]));
    //     Assert.IsTrue(map.TryRemove(types[999]));
    //     Assert.IsTrue(map.TryRemove(types[377]));
    //     Assert.IsTrue(map.TryRemove(types[733]));
    //     Assert.AreEqual(count - 4, map.Count);

    //     // remove in the reverse order to keep the correct index in regard to map
    //     types.RemoveAt(999);
    //     types.RemoveAt(733);
    //     types.RemoveAt(377);
    //     types.RemoveAt(0);
    //     Assert.AreEqual(count - 4, types.Count);

    //     // Check the second enumeration is working
    //     var keys2 = map.Select(kv => kv.Key).ToList();
    //     CollectionAssert.AreEquivalent(types, keys2);

    //     Verify(map, types);
    // }

    // [Test]
    // public void Simplified_test_with_equal_hashes_RefEq()
    // {
    //     var map = new SmallMap16<Type, string, RefEq<Type>>();

    //     var keys = new[] { typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>) };
    //     var i = 1;
    //     foreach (var k in keys)
    //         map.AddOrUpdate(k, "" + i++);

    //     Assert.AreEqual(3, map.Count);

    //     Assert.IsTrue(map.TryRemove(typeof(Tuple<,,>)));
    //     Assert.AreEqual(2, map.Count);

    //     Verify(map, new[] { typeof(Tuple<>), typeof(Tuple<,>) });
    // }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var m = new SmallMap16<int, string, IntEq>();
        ref var map = ref m.Map;

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(42 + 32 + 32, "3");

        // interrupt the keys with ne key
        map.AddOrUpdate(43, "a");
        map.AddOrUpdate(43 + 32, "b");
        map.AddOrUpdate(43 + 32 + 32, "c");

        map.AddOrUpdate(42 + 32 + 32 + 32, "4");

        // insert 3rd variety of the keys
        map.AddOrUpdate(44, "*");

        map.AddOrUpdate(42 + 32 + 32 + 32 + 32, "5");
        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32, "6");
        map.AddOrUpdate(43 + 32 + 32 + 32, "d");

        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32, "7");
        map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32, "8");

        // check for the missing key
        Assert.AreEqual(null, map.GetValueOrDefault(43 + 32 + 32 + 32 + 32, default(string)));

        // check for the strange key
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

    /*
    ## Debug output example

    ### IntEq

    [AddOrUpdate] Probes abs max=2, max=2, all=[1: 1, 2: 1]; first 4 probes are 2 out of 2
    [AddOrUpdate] Probes abs max=3, max=3, all=[1: 1, 2: 1, 3: 1]; first 4 probes are 3 out of 3
    [AllocateEntries] Resize entries: 2 -> 4
    [ResizeHashes] 4 -> 8
    [ResizeHashes] Probes abs max=3, max=3, all=[1: 1, 2: 1, 3: 1]; first 4 probes are 3 out of 3
    [AddOrUpdate] Probes abs max=4, max=4, all=[1: 1, 2: 1, 3: 2, 4: 1]; first 4 probes are 5 out of 5
    [AllocateEntries] Resize entries: 4 -> 8
    [AddOrUpdate] Probes abs max=5, max=5, all=[1: 1, 2: 1, 3: 2, 4: 1, 5: 1]; first 4 probes are 5 out of 6
    [AddOrUpdate-RH] Probes abs max=6, max=6, all=[1: 1, 2: 1, 3: 2, 4: 2, 5: 1, 6: 1]; first 4 probes are 6 out of 8
    [ResizeHashes] 8 -> 16
    [ResizeHashes] Probes abs max=6, max=6, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 1, 6: 1]; first 4 probes are 5 out of 7
    [AllocateEntries] Resize entries: 8 -> 16
    [AddOrUpdate-RH] Probes abs max=7, max=7, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 1, 7: 1]; first 4 probes are 5 out of 9
    [AddOrUpdate-RH] Probes abs max=8, max=8, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 2, 7: 1, 8: 1]; first 4 probes are 5 out of 11
    [AddOrUpdate] Probes abs max=9, max=9, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 2, 7: 1, 8: 2, 9: 1]; first 4 probes are 5 out of 13
    [AddOrUpdate-RH] Probes abs max=10, max=10, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 2, 7: 2, 8: 2, 9: 1, 10: 1]; first 4 probes are 5 out of 15
    [AddOrUpdate-RH] Probes abs max=11, max=11, all=[1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 2, 7: 2, 8: 3, 9: 1, 10: 1, 11: 1]; first 4 probes are 5 out of 17

    ### GoldenIntEq

    [AddOrUpdate] Probes abs max=2, max=2, all=[1: 1, 2: 1]; first 4 probes are 2 out of 2
    [AllocateEntries] Resize entries: 2 -> 4
    [ResizeHashes] 4 -> 8
    [ResizeHashes] Probes abs max=2, max=1, all=[1: 3]; first 4 probes are 3 out of 3
    [AllocateEntries] Resize entries: 4 -> 8
    [AddOrUpdate] Probes abs max=2, max=2, all=[1: 5, 2: 1]; first 4 probes are 6 out of 6
    [ResizeHashes] 8 -> 16
    [ResizeHashes] Probes abs max=2, max=2, all=[1: 6, 2: 1]; first 4 probes are 7 out of 7
    [AllocateEntries] Resize entries: 8 -> 16

    */

    // [Test]
    // public void Can_store_and_retrieve_value_from_map_Golden()
    // {
    //     var map = new SmallMap16<int, string, IntEq>(2);
    //     // var map = new SmallMap16<int, string, GoldenIntEq>(2);

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42 + 32, "2");
    //     map.AddOrUpdate(42 + 32 + 32, "3");

    //     // interrupt the keys with new key
    //     map.AddOrUpdate(43, "a");
    //     map.AddOrUpdate(43 + 32, "b");
    //     map.AddOrUpdate(43 + 32 + 32, "c");

    //     map.AddOrUpdate(42 + 32 + 32 + 32, "4");

    //     // insert 3rd variety of the keys
    //     map.AddOrUpdate(44, "*");

    //     map.AddOrUpdate(42 + 32 + 32 + 32 + 32, "5");
    //     map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32, "6");
    //     map.AddOrUpdate(43 + 32 + 32 + 32, "d");

    //     map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32, "7");
    //     map.AddOrUpdate(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32, "8");

    //     // check for the missing key
    //     Assert.AreEqual(null, map.GetValueOrDefault(43 + 32 + 32 + 32 + 32));

    //     // check for the strange key
    //     Assert.AreEqual("*", map.GetValueOrDefault(44));

    //     Assert.AreEqual("1", map.GetValueOrDefault(42));
    //     Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
    //     Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
    //     Assert.AreEqual("4", map.GetValueOrDefault(42 + 32 + 32 + 32));
    //     Assert.AreEqual("5", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32));
    //     Assert.AreEqual("6", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32));
    //     Assert.AreEqual("7", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32));
    //     Assert.AreEqual("8", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32));

    //     Assert.AreEqual("a", map.GetValueOrDefault(43));
    //     Assert.AreEqual("b", map.GetValueOrDefault(43 + 32));
    //     Assert.AreEqual("c", map.GetValueOrDefault(43 + 32 + 32));
    //     Assert.AreEqual("d", map.GetValueOrDefault(43 + 32 + 32 + 32));

    //     Assert.AreEqual(13, map.Count);

    //     Verify(map, null);
    // }

    // [Test]
    // public void Can_lookup_the_default_map_without_error()
    // {
    //     SmallMap16<int, string, IntEq> map = default;

    //     Assert.IsFalse(map.TryGetValue(42, out _));
    // }

    // [Test]
    // public void Can_store_and_retrieve_value_from_map_with_Expand_in_the_middle()
    // {
    //     var map = new SmallMap16<int, string, IntEq>(1);

    //     Assert.IsFalse(map.TryGetValue(42, out _));

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42 + 32, "2");

    //     // interrupt the keys with new key
    //     map.AddOrUpdate(43, "a");
    //     map.AddOrUpdate(43 + 32, "b");

    //     map.AddOrUpdate(42 + 32 + 32, "3");

    //     Assert.AreEqual("1", map.GetValueOrDefault(42));
    //     Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
    //     Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
    //     Assert.AreEqual(null, map.GetValueOrDefault(42 + 32 + 32 + 32));
    //     Assert.AreEqual("a", map.GetValueOrDefault(43));

    //     map.AddOrUpdate(43, "a!");
    //     Assert.AreEqual("a!", map.GetValueOrDefault(43));

    //     map.AddOrUpdate(47, "x");
    //     map.AddOrUpdate(53, "y");
    //     Assert.AreEqual("x", map.GetValueOrDefault(47));
    //     Assert.AreEqual("y", map.GetValueOrDefault(53));

    //     map.AddOrUpdate(47 + 16, "x!");
    //     map.AddOrUpdate(53 + 16, "y!");
    //     Assert.AreEqual("x!", map.GetValueOrDefault(47 + 16));
    //     Assert.AreEqual("y!", map.GetValueOrDefault(53 + 16));

    //     Verify(map, null);
    // }

    // [Test]
    // public void Can_resize_without_moving()
    // {
    //     var map = new SmallMap16<int, string, IntEq>(2);

    //     map.AddOrUpdate(0, "0");
    //     map.AddOrUpdate(1, "1");
    //     map.AddOrUpdate(9, "9");

    //     // resize goes here
    //     map.AddOrUpdate(3, "3");

    //     map.AddOrUpdate(5, "5");

    //     Verify(map, new[] { 0, 1, 3, 5, 9 });
    // }

    // [Test]
    // public void Can_store_and_get_stored_item_count()
    // {
    //     var map = new SmallMap16<int, string, IntEq>();

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42 + 32 + 32, "3");

    //     Assert.AreEqual(2, map.Count);
    //     Verify(map, new[] { 42, 42 + 32 + 32 });
    // }

    // [Test]
    // public void Can_update_a_stored_item_with_new_value()
    // {
    //     var map = new SmallMap16<int, string, IntEq>();

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42, "3");

    //     Assert.AreEqual("3", map.GetValueOrDefault(42));
    //     Assert.AreEqual(1, map.Count);
    //     Verify(map, new[] { 42 });
    // }

    // [Test]
    // public void Can_add_key_with_0_hash_code()
    // {
    //     var map = new SmallMap16<int, string, IntEq>();

    //     map.AddOrUpdate(0, "aaa");
    //     map.AddOrUpdate(0 + 32, "2");
    //     map.AddOrUpdate(0 + 32 + 32, "3");
    //     Verify(map, new[] { 0, 0 + 32, 0 + 32 + 32 });

    //     string value;
    //     Assert.IsTrue(map.TryGetValue(0, out value));

    //     Assert.AreEqual("aaa", value);
    // }

    // [Test]
    // public void Can_quickly_find_the_scattered_items_with_the_same_cache()
    // {
    //     var map = new SmallMap16<int, string, IntEq>();

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(43, "a");
    //     map.AddOrUpdate(42 + 32, "2");
    //     map.AddOrUpdate(45, "b");
    //     map.AddOrUpdate(46, "c");
    //     map.AddOrUpdate(42 + 32 + 32, "3");
    //     Verify(map, new[] { 42, 43, 42 + 32, 45, 46, 42 + 32 + 32 });

    //     string value;
    //     Assert.IsTrue(map.TryGetValue(42 + 32, out value));
    //     Assert.AreEqual("2", value);

    //     Assert.IsTrue(map.TryGetValue(42 + 32 + 32, out value));
    //     Assert.AreEqual("3", value);
    // }

    // [Test]
    // public void Can_remove_the_stored_item()
    // {
    //     var map = new SmallMap16<int, string, IntEq>(2);

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42 + 32, "2");
    //     map.AddOrUpdate(42 + 32 + 32, "3");

    //     Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
    //     var r = map.TryRemove(42 + 32);
    //     Assert.IsTrue(r);

    //     Assert.AreEqual(2, map.Count);
    //     Assert.AreEqual("1", map.GetValueOrDefault(42));
    //     Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
    //     Verify(map, null);
    // }
}
