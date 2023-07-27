﻿
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

using static FHashMap91;

[TestFixture]
public class FHashMap91Tests
{
    internal static void Verify<K, V, TEq, TEntries>(FHashMap91<K, V, TEq, TEntries> map, IEnumerable<K> expectedKeys)
        where TEq : struct, IEqualityComparer<K>
        where TEntries : struct, IEntries<K, V>
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
        // var map = new FHashMap91<Type, string, TypeEq>();         // MaxProbes -> 8
        // var map = new FHashMap91<Type, string, RefEq<Type>>();    // MaxProbes -> 7
        var map = FHashMap91.New<Type, string, GoldenRefEq<Type>>(); // MaxProbes -> 5

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

        var map = FHashMap91.New<Type, string, RefEq<Type>>(8);

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

        var map = FHashMap91.New<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap91Tests), "!");
        Assert.AreEqual(1001, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(FHashMap91Tests)));
        Assert.AreEqual(1000, map.Count);

        Verify(map, types);
    }

    [Test]
    public void Real_world_test_with_TryRemove_from_1000_items_TypeEq()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        var map = FHashMap91.NewChunked<Type, string, TypeEq>();

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

        var map = FHashMap91.New<Type, string, GoldenRefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap91Tests), "!");
        Assert.AreEqual(1001, map.Count);

        // Assert.IsTrue(map.TryRemove(typeof(FHashMap91Tests)));
        // Assert.AreEqual(1000, map.Count);

        var entries = (SingleArrayEntries<Type, string>)map.Entries;
        Console.WriteLine(entries.Contains(typeof(Tuple<>)));
        Console.WriteLine(entries.Contains(typeof(Tuple<,>)));
        Console.WriteLine(entries.Contains(typeof(TimeZoneInfo.TransitionTime)));

        var t1h = default(GoldenRefEq<Type>).GetHashCode(typeof(Tuple<>));
        var t2h = default(GoldenRefEq<Type>).GetHashCode(typeof(Tuple<,>));
        Console.WriteLine($"t1h:{t1h} == t2h:{t2h} --> {t1h == t2h}");

        var hashes = map.PackedHashesAndIndexes;
        var indexMask = (hashes.Length - 1);
        var hashPartMask = HashAndIndexMask & ~indexMask;
        var i = 0;
        foreach (var h in hashes)
        {
            var hPart = h & hashPartMask;
            if (hPart == (t1h & hashPartMask) || hPart == (t2h & hashPartMask))
            {
                var probes = h >>> ProbeCountShift;
                Console.WriteLine($"{i}: p{probes}, {h & indexMask} -> {entries._entries[h & indexMask].Key.Name}");
                if (probes > i)
                {
                    var prev1 = (hashes.Length + i - 1) & indexMask;
                    var h1 = hashes[prev1];
                    Console.WriteLine($"PREV1 {prev1}: p{h1 >>> ProbeCountShift}, {h1 & indexMask} -> {entries._entries[h1 & indexMask].Key.Name}");
                    var prev2 = (hashes.Length + i - 2) & indexMask;
                    var h2 = hashes[prev2];
                    Console.WriteLine($"PREV2 {prev2}: p{h2 >>> ProbeCountShift}, {h2 & indexMask} -> {entries._entries[h2 & indexMask].Key.Name}");
                    var prev3 = (hashes.Length + i - 3) & indexMask;
                    var h3 = hashes[prev3];
                    Console.WriteLine($"PREV3 {prev3}: p{h3 >>> ProbeCountShift}, {h3 & indexMask} -> {entries._entries[h3 & indexMask].Key.Name}");
                }
            }
            ++i;
        }

        /*
          Standard Output Messages:
         RefEquals
         RefEquals
         RefEquals
         t1h:2017736702 == t2h:208253299 --> False
         1: p4, 451 -> Tuple`1
         371: p1, 452 -> Tuple`2
         th1:2017736702
         th1:not found
        */

        // problematic types
        Assert.IsTrue(map.TryGetValue(typeof(Tuple<>), out _));
        Assert.IsTrue(map.TryGetValue(typeof(Tuple<,>), out _));

        Verify(map, types);
    }

    [Test]
    public void Simplified_test_with_equal_hashes_GoldenRefEq()
    {
        // var map = FHashMap91.New<Type, string, GoldenRefEq<Type>>();
        // var map = FHashMap91.New<Type, string, RefEq<Type>>();
        var map = FHashMap91.New<Type, string, GoldenRefEq<Type>>();

        var keys = new[] { typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>) };
        var i = 1;
        foreach (var k in keys)
        {
            map.AddOrUpdate(k, "" + i++);
            // Console.WriteLine($"{k.ToString()}: {default(RefEq<Type>).GetHashCode()} -- {default(TypeEq).GetHashCode()}");
        }

        Assert.AreEqual(3, map.Count);

        Assert.IsTrue(map.TryRemove(typeof(Tuple<,,>)));
        Assert.AreEqual(2, map.Count);

        Verify(map, new[] { typeof(Tuple<>), typeof(Tuple<,>) });
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var map = FHashMap91.New<int, string, IntEq>(2);

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
    ## The example of the output

    [AllocateEntries] Resize entries: 2 -> 4
    [ResizeHashes] with overflow buffer 4+2=6 -> 8+3=11
    [ResizeHashes] Probes abs max = 1, max = 1, all = [1: 3]
    [ResizeHashes] first 4 probes total is 3 out of 3
    [AllocateEntries] Resize entries: 4 -> 8
    [AddOrUpdate] Probes abs max = 2, max = 2, all = [1: 5, 2: 1]
    [AddOrUpdate] first 4 probes total is 6 out of 6
    [ResizeHashes] with overflow buffer 8+3=11 -> 16+4=20
    [ResizeHashes] Probes abs max = 2, max = 2, all = [1: 6, 2: 1]
    [ResizeHashes] first 4 probes total is 7 out of 7
    [AllocateEntries] Resize entries: 8 -> 16
    */
    [Test]
    public void Can_store_and_retrieve_value_from_map_Golden()
    {
        // var map = FHashMap91.New<int, string, IntEq>(2);
        var map = FHashMap91.New<int, string, GoldenIntEq>(2);

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
        var map = FHashMap91.New<int, string, IntEq>(1);

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
        var map = FHashMap91.New<int, string, IntEq>(2);

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
        var map = FHashMap91.New<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual(2, map.Count);
        Verify(map, new[] { 42, 42 + 32 + 32 });
    }

    [Test]
    public void Can_update_a_stored_item_with_new_value()
    {
        var map = FHashMap91.New<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42, "3");

        Assert.AreEqual("3", map.GetValueOrDefault(42));
        Assert.AreEqual(1, map.Count);
        Verify(map, new[] { 42 });
    }

    [Test]
    public void Can_add_key_with_0_hash_code()
    {
        var map = FHashMap91.New<int, string, IntEq>();

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
        var map = FHashMap91.New<int, string, IntEq>();

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
        var map = FHashMap91.New<int, string, IntEq>(2);

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
