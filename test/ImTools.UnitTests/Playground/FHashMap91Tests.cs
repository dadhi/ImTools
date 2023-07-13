
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

[TestFixture]
public class FHashMap91Tests
{
    internal static void Verify<K, V, TEq>(FHashMap91<K, V, TEq> map, IEnumerable<K> expectedKeys)
        where TEq : struct, IEqualityComparer<K>
    {
        map.VerifyHashesAndKeysEq(eq => Assert.True(eq));
        map.VerifyNoDuplicateKeys(key => Assert.Fail($"Duplicate key: {key}"));
        if (expectedKeys != null)
            map.VerifyContainAllKeys(expectedKeys, (contains, key) => Assert.True(contains, $"Key not found:`{key}`"));
    }

#if NET7_0_OR_GREATER
    [Test]
    public void Test_stackalloc_for_entries()
    {
        Span<int> arr = stackalloc int[8];

        Foo(in arr);

        Assert.AreEqual(42, arr[2]);
    }

    static void Foo(in Span<int> arr)
    {
        arr[2] = 42;
    }
#endif

    [Test]
    public void Real_world_test_AddOrUpdate()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        // todo: @perf testing diff equality comparers
        // var map = new ImTools.Experiments.FHashMap91<Type, string, TypeEq>();         // MaxProbes -> 8
        // var map = new ImTools.Experiments.FHashMap91<Type, string, RefEq<Type>>();    // MaxProbes -> 7
        var map = new ImTools.Experiments.FHashMap91<Type, string, GoldenRefEq<Type>>(); // MaxProbes -> 5

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(Console), "!");

        var found = map.TryGetValue(typeof(Console), out var value);
        Assert.IsTrue(found);
        Assert.AreEqual("!", value);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_AddOrUpdate_NO_Resize()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = new ImTools.Experiments.FHashMap91<Type, string, RefEq<Type>>(8);

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap91Tests), "!");

        Assert.AreEqual(101, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_TryRemove_from_1000_items()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        var map = new ImTools.Experiments.FHashMap91<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap91Tests), "!");
        Assert.AreEqual(1001, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(FHashMap91Tests)));
        Assert.AreEqual(1000, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_TryRemove_from_1000_items_GoldenRefEq()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        var map = new ImTools.Experiments.FHashMap91<Type, string, GoldenRefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap91Tests), "!");
        Assert.AreEqual(1001, map.Count);

        Assert.IsTrue(map.TryRemove(types[500]));
        Assert.AreEqual(1000, map.Count);

        types[500] = typeof(FHashMap91Tests);
        Verify(map, types);
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var map = new FHashMap91<int, string, IntEq>(2);

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
        Assert.AreEqual(null, map.GetValueOrDefault(43 + 32 + 32 + 32 + 32));

        // check for the strange key
        Assert.AreEqual("*", map.GetValueOrDefault(44));

        Assert.AreEqual("1", map.GetValueOrDefault(42));
        Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
        Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        Assert.AreEqual("4", map.GetValueOrDefault(42 + 32 + 32 + 32));
        Assert.AreEqual("5", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32));
        Assert.AreEqual("6", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32));
        Assert.AreEqual("7", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32));
        Assert.AreEqual("8", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32));

        Assert.AreEqual("a", map.GetValueOrDefault(43));
        Assert.AreEqual("b", map.GetValueOrDefault(43 + 32));
        Assert.AreEqual("c", map.GetValueOrDefault(43 + 32 + 32));
        Assert.AreEqual("d", map.GetValueOrDefault(43 + 32 + 32 + 32));

        Assert.AreEqual(13, map.Count);

        Verify(map, null);
    }

/*
## The comparison if IntEq vs GoldenIntEq vs GoldenShiftIntEq

// IntEq
[AddOrUpdate] max_probes = 2, all probes = [1: 1, 2: 1]
[AddOrUpdate] first 4 probes total is 2 out of 2
[AddOrUpdate] max_probes = 3, all probes = [1: 1, 2: 1, 3: 1]
[AddOrUpdate] first 4 probes total is 3 out of 3
[ResizeHashes] with overflow buffer 4+2=6 -> 8+3=11
[ResizeHashes] max_probes = 3, all probes = [1: 1, 2: 1, 3: 1]
[ResizeHashes] first 4 probes total is 3 out of 3
[AddOrUpdate] max_probes = 4, all probes = [1: 1, 2: 1, 3: 2, 4: 1]
[AddOrUpdate] first 4 probes total is 5 out of 5
[AllocateEntries] Resize entries: 4 -> 8
[AddOrUpdate] max_probes = 5, all probes = [1: 1, 2: 1, 3: 2, 4: 1, 5: 1]
[AddOrUpdate] first 4 probes total is 5 out of 6
[AddOrUpdate-RH] max_probes = 6, all probes = [1: 1, 2: 1, 3: 2, 4: 2, 5: 1, 6: 1]
[AddOrUpdate-RH] first 4 probes total is 6 out of 8
[ResizeHashes] with overflow buffer 8+3=11 -> 16+4=20
[ResizeHashes] max_probes = 6, all probes = [1: 1, 2: 1, 3: 1, 4: 2, 5: 1, 6: 1]
[ResizeHashes] first 4 probes total is 5 out of 7
[AllocateEntries] Resize entries: 8 -> 16
[AddOrUpdate-RH] max_probes = 7, all probes = [1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 1, 7: 1]
[AddOrUpdate-RH] first 4 probes total is 5 out of 9
[AddOrUpdate-RH] max_probes = 8, all probes = [1: 1, 2: 1, 3: 1, 4: 2, 5: 2, 6: 2, 7: 1, 8: 1]
[AddOrUpdate-RH] first 4 probes total is 5 out of 11
[ResizeHashes] with overflow buffer 16+4=20 -> 32+5=37
[ResizeHashes] max_probes = 8, all probes = [1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 2, 7: 1, 8: 2]
[ResizeHashes] first 4 probes total is 4 out of 10
_hashesOverflowBufferIsFull!
[AddOrUpdate] max_probes = 9, all probes = [1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 2, 7: 1, 8: 2, 9: 1]
[AddOrUpdate] first 4 probes total is 4 out of 11
[AddOrUpdate-RH] max_probes = 10, all probes = [1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 2, 7: 2, 8: 2, 9: 1, 10: 1]
[AddOrUpdate-RH] first 4 probes total is 4 out of 13
[AddOrUpdate-RH] max_probes = 11, all probes = [1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 2, 7: 2, 8: 3, 9: 1, 10: 1, 11: 1]
[AddOrUpdate-RH] first 4 probes total is 4 out of 15

// GoldenIntEq
[AddOrUpdate] max_probes = 2, all probes = [1: 1, 2: 1]
[AddOrUpdate] first 4 probes total is 2 out of 2
[AddOrUpdate] max_probes = 3, all probes = [1: 1, 2: 1, 3: 1]
[AddOrUpdate] first 4 probes total is 3 out of 3
[ResizeHashes] with overflow buffer 4+2=6 -> 8+3=11
[ResizeHashes] max_probes = 3, all probes = [1: 1, 2: 1, 3: 1]
[ResizeHashes] first 4 probes total is 3 out of 3
[AddOrUpdate] max_probes = 4, all probes = [1: 1, 2: 1, 3: 2, 4: 1]
[AddOrUpdate] first 4 probes total is 5 out of 5
[AllocateEntries] Resize entries: 4 -> 8
[AddOrUpdate] max_probes = 5, all probes = [1: 1, 2: 1, 3: 2, 4: 1, 5: 1]
[AddOrUpdate] first 4 probes total is 5 out of 6
[AddOrUpdate-RH] max_probes = 6, all probes = [1: 1, 2: 1, 3: 2, 4: 2, 5: 1, 6: 1]
[AddOrUpdate-RH] first 4 probes total is 6 out of 8
[ResizeHashes] with overflow buffer 8+3=11 -> 16+4=20
[ResizeHashes] max_probes = 4, all probes = [1: 2, 2: 2, 3: 2, 4: 1]
[ResizeHashes] first 4 probes total is 7 out of 7
[AddOrUpdate] max_probes = 5, all probes = [1: 2, 2: 2, 3: 3, 4: 1, 5: 1]
[AddOrUpdate] first 4 probes total is 8 out of 9
[AllocateEntries] Resize entries: 8 -> 16
[AddOrUpdate] max_probes = 6, all probes = [1: 2, 2: 2, 3: 3, 4: 2, 5: 1, 6: 1]
[AddOrUpdate] first 4 probes total is 9 out of 11
[AddOrUpdate] max_probes = 7, all probes = [1: 2, 2: 2, 3: 3, 4: 3, 5: 2, 6: 1, 7: 1]
[AddOrUpdate] first 4 probes total is 10 out of 14
[AddOrUpdate] max_probes = 8, all probes = [1: 2, 2: 2, 3: 3, 4: 3, 5: 2, 6: 2, 7: 1, 8: 1]
[AddOrUpdate] first 4 probes total is 10 out of 16

// GoldenShiftIntEq (shift 5)
[ResizeHashes] with overflow buffer 4+2=6 -> 8+3=11
[ResizeHashes] max_probes = 1, all probes = [1: 3]
[ResizeHashes] first 4 probes total is 3 out of 3
[AllocateEntries] Resize entries: 4 -> 8
[AddOrUpdate] max_probes = 2, all probes = [1: 5, 2: 1]
[AddOrUpdate] first 4 probes total is 6 out of 6
[ResizeHashes] with overflow buffer 8+3=11 -> 16+4=20
[ResizeHashes] max_probes = 2, all probes = [1: 6, 2: 1]
[ResizeHashes] first 4 probes total is 7 out of 7
[AllocateEntries] Resize entries: 8 -> 16
*/
    [Test]
    public void Can_store_and_retrieve_value_from_map_Golden()
    {
        // var map = new FHashMap91<int, string, IntEq>(2);
        // var map = new FHashMap91<int, string, GoldenIntEq>(2);
        var map = new FHashMap91<int, string, GoldenShiftIntEq>(2);

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
        Assert.AreEqual(null, map.GetValueOrDefault(43 + 32 + 32 + 32 + 32));

        // check for the strange key
        Assert.AreEqual("*", map.GetValueOrDefault(44));

        Assert.AreEqual("1", map.GetValueOrDefault(42));
        Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
        Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        Assert.AreEqual("4", map.GetValueOrDefault(42 + 32 + 32 + 32));
        Assert.AreEqual("5", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32));
        Assert.AreEqual("6", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32));
        Assert.AreEqual("7", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32));
        Assert.AreEqual("8", map.GetValueOrDefault(42 + 32 + 32 + 32 + 32 + 32 + 32 + 32));

        Assert.AreEqual("a", map.GetValueOrDefault(43));
        Assert.AreEqual("b", map.GetValueOrDefault(43 + 32));
        Assert.AreEqual("c", map.GetValueOrDefault(43 + 32 + 32));
        Assert.AreEqual("d", map.GetValueOrDefault(43 + 32 + 32 + 32));

        Assert.AreEqual(13, map.Count);

        Verify(map, null);
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map_with_Expand_in_the_middle()
    {
        var map = new FHashMap91<int, string, IntEq>(1);

        Assert.IsFalse(map.TryGetValue(42, out _));

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");

        // interrupt the keys with ne key
        map.AddOrUpdate(43, "a");
        map.AddOrUpdate(43 + 32, "b");

        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual("1", map.GetValueOrDefault(42));
        Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
        Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        Assert.AreEqual(null, map.GetValueOrDefault(42 + 32 + 32 + 32));
        Assert.AreEqual("a", map.GetValueOrDefault(43));

        map.AddOrUpdate(43, "a!");
        Assert.AreEqual("a!", map.GetValueOrDefault(43));

        map.AddOrUpdate(47, "x");
        map.AddOrUpdate(53, "y");
        Assert.AreEqual("x", map.GetValueOrDefault(47));
        Assert.AreEqual("y", map.GetValueOrDefault(53));

        Verify(map, null);
    }

    [Test]
    public void Can_resize_without_moving()
    {
        var map = new FHashMap91<int, string, IntEq>(2);

        map.AddOrUpdate(0, "0");
        map.AddOrUpdate(1, "1");
        map.AddOrUpdate(9, "9");

        // resize goes here
        map.AddOrUpdate(3, "3");

        map.AddOrUpdate(5, "5");

        Verify(map, new[] { 0, 1, 3, 5, 9 });
    }

    [Test]
    public void Can_store_and_get_stored_item_count()
    {
        var map = new FHashMap91<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual(2, map.Count);
        Verify(map, new[] { 42, 42 + 32 + 32 });
    }

    [Test]
    public void Can_update_a_stored_item_with_new_value()
    {
        var map = new FHashMap91<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42, "3");

        Assert.AreEqual("3", map.GetValueOrDefault(42));
        Assert.AreEqual(1, map.Count);
        Verify(map, new[] { 42 });
    }

    [Test]
    public void Can_add_key_with_0_hash_code()
    {
        var map = new FHashMap91<int, string, IntEq>();

        map.AddOrUpdate(0, "aaa");
        map.AddOrUpdate(0 + 32, "2");
        map.AddOrUpdate(0 + 32 + 32, "3");
        Verify(map, new[] { 0, 0 + 32, 0 + 32 + 32 });

        string value;
        Assert.IsTrue(map.TryGetValue(0, out value));

        Assert.AreEqual("aaa", value);
    }

    [Test]
    public void Can_quickly_find_the_scattered_items_with_the_same_cache()
    {
        var map = new FHashMap91<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(43, "a");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(45, "b");
        map.AddOrUpdate(46, "c");
        map.AddOrUpdate(42 + 32 + 32, "3");
        Verify(map, new[] { 42, 43, 42 + 32, 45, 46, 42 + 32 + 32 });

        string value;
        Assert.IsTrue(map.TryGetValue(42 + 32, out value));
        Assert.AreEqual("2", value);

        Assert.IsTrue(map.TryGetValue(42 + 32 + 32, out value));
        Assert.AreEqual("3", value);
    }

    [Test]
    public void Can_remove_the_stored_item()
    {
        var map = new FHashMap91<int, string, IntEq>(2);

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(42 + 32 + 32, "3");

        var r = map.TryRemove(42 + 32);
        Assert.IsTrue(r);

        Assert.AreEqual(2, map.Count);
        Verify(map, new[] { 42, 42 + 32 + 32 });
    }
}
