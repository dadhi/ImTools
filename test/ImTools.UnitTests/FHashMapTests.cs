
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

using static FHashMap;

[TestFixture]
public class FHashMapTests
{
    internal static void Verify<K, V, TEq, TEntries>(FHashMap<K, V, TEq, TEntries> map, IEnumerable<K> expectedKeys)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        map.VerifyHashesAndKeysEq(eq => Assert.True(eq));
        map.VerifyProbesAreFitRobinHood(err => Assert.Fail(err));
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

    static void Foo(in Span<int> arr) => arr[2] = 42;
#endif

    [Test]
    public void Real_world_test_AddOrUpdate()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = FHashMap.New<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(Console), "!");

        Assert.IsTrue(map.Contains(typeof(Console)));
        Assert.AreEqual("!", map.GetValueOrDefault(typeof(Console)));

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_AddOrUpdate_NO_Resize()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = FHashMap.New<Type, string, RefEq<Type>>(8);

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMapTests), "!");

        Assert.AreEqual(101, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_TryRemove_from_1000_items()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        var map = FHashMap.New<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMapTests), "!");
        Assert.AreEqual(1001, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(FHashMapTests)));
        Assert.AreEqual(1000, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_TryRemove_from_1000_items_TypeEq()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        var map = FHashMap.NewChunked<Type, string, TypeEq>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMapTests), "!");
        Assert.AreEqual(1001, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(FHashMapTests)));
        Assert.AreEqual(1000, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_Enumerator_and_TryRemove_the_entries()
    {
        var count = 1000;
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(count).ToList();
        Assert.AreEqual(count, types.Count);

        var map = FHashMap.New<Type, string, TypeEq>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        var keys = map.Select(kv => kv.Key).ToList();
        CollectionAssert.AreEqual(types, keys);

        Assert.IsTrue(map.TryRemove(types[0]));
        Assert.IsTrue(map.TryRemove(types[999]));
        Assert.IsTrue(map.TryRemove(types[377]));
        Assert.IsTrue(map.TryRemove(types[733]));
        Assert.AreEqual(count - 4, map.Count);

        // remove in the reverse order to keep the correct index in regard to map
        types.RemoveAt(999);
        types.RemoveAt(733);
        types.RemoveAt(377);
        types.RemoveAt(0);
        Assert.AreEqual(count - 4, types.Count);

        // Check the second enumeration is working
        var keys2 = map.Select(kv => kv.Key).ToList();
        CollectionAssert.AreEqual(types, keys2);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_Enumerator_and_TryRemove_the_entries_ChunkedEntriesArray()
    {
        var count = 1000;
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(count).ToList();
        Assert.AreEqual(count, types.Count);

        var map = FHashMap.NewChunked<Type, string, TypeEq>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        var keys = map.Select(kv => kv.Key).ToList();
        CollectionAssert.AreEqual(types, keys);

        Assert.IsTrue(map.TryRemove(types[0]));
        Assert.IsTrue(map.TryRemove(types[999]));
        Assert.IsTrue(map.TryRemove(types[377]));
        Assert.IsTrue(map.TryRemove(types[733]));
        Assert.AreEqual(count - 4, map.Count);

        // remove in the reverse order to keep the correct index in regard to map
        types.RemoveAt(999);
        types.RemoveAt(733);
        types.RemoveAt(377);
        types.RemoveAt(0);
        Assert.AreEqual(count - 4, types.Count);

        // Check the second enumeration is working
        var keys2 = map.Select(kv => kv.Key).ToList();
        CollectionAssert.AreEqual(types, keys2);

        Verify(map, types);
    }

    [Test]
    public void Simplified_test_with_equal_hashes_RefEq()
    {
        var map = FHashMap.New<Type, string, RefEq<Type>>();

        var keys = new[] { typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>) };
        var i = 1;
        foreach (var k in keys)
            map.AddOrUpdate(k, "" + i++);

        Assert.AreEqual(3, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(Tuple<,,>)));
        Assert.AreEqual(2, map.Count);

        Verify(map, new[] { typeof(Tuple<>), typeof(Tuple<,>) });
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var map = FHashMap.New<int, string, IntEq>(2);

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
    [Test]
    public void Can_store_and_retrieve_value_from_map_Golden()
    {
        var map = FHashMap.New<int, string, IntEq>(2);
        // var map = FHashMap.New<int, string, GoldenIntEq>(2);

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(42 + 32 + 32, "3");

        // interrupt the keys with new key
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
    public void Can_lookup_the_default_map_without_error()
    {
        FHashMap<int, string, IntEq, SingleArrayEntries<int, string, IntEq>> map = default;

        Assert.IsFalse(map.TryGetValue(42, out _));
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map_with_Expand_in_the_middle()
    {
        var map = FHashMap.New<int, string, IntEq>(1);

        Assert.IsFalse(map.TryGetValue(42, out _));

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");

        // interrupt the keys with new key
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

        map.AddOrUpdate(47 + 16, "x!");
        map.AddOrUpdate(53 + 16, "y!");
        Assert.AreEqual("x!", map.GetValueOrDefault(47 + 16));
        Assert.AreEqual("y!", map.GetValueOrDefault(53 + 16));

        Verify(map, null);
    }

    [Test]
    public void Can_resize_without_moving()
    {
        var map = FHashMap.New<int, string, IntEq>(2);

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
        var map = FHashMap.New<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual(2, map.Count);
        Verify(map, new[] { 42, 42 + 32 + 32 });
    }

    [Test]
    public void Can_update_a_stored_item_with_new_value()
    {
        var map = FHashMap.New<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42, "3");

        Assert.AreEqual("3", map.GetValueOrDefault(42));
        Assert.AreEqual(1, map.Count);
        Verify(map, new[] { 42 });
    }

    [Test]
    public void Can_add_key_with_0_hash_code()
    {
        var map = FHashMap.New<int, string, IntEq>();

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
        var map = FHashMap.New<int, string, IntEq>();

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
        var map = FHashMap.New<int, string, IntEq>(2);

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
        var r = map.TryRemove(42 + 32);
        Assert.IsTrue(r);

        Assert.AreEqual(2, map.Count);
        Assert.AreEqual("1", map.GetValueOrDefault(42));
        Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        Verify(map, null);
    }
}
