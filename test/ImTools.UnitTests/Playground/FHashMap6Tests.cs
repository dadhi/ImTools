﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experiments.UnitTests;

[TestFixture]
public class FHashMap6Tests
{
    public static void Verify<K, V, TEq>(FHashMap6<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var exp = map.Explain();
        foreach (var it in exp)
            if (!it.IsEmpty)
                Assert.True(it.HEq);

        // Verify the indexes
        var uniq = new Dictionary<K, int>(map.Count);
        var entryIndexMask = map.HashesAndIndexes.Length - 1;
        var entries = map.Entries;
        foreach (var it in map.HashesAndIndexes)
            if (it != 0)
            {
                var entryIndex = it & entryIndexMask;
                var key = entries[entryIndex].Key;
                if (!uniq.TryGetValue(key, out var count))
                    uniq.Add(key, 1);
                else
                {
                    Assert.Fail($"Duplicate key: {key}");
                    uniq[key] = count + 1;
                }
            }
    }

    [Test]
    public void Real_world_test_AddOrUpdate()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = new ImTools.Experiments.FHashMap6<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(Console), "!");

        Assert.AreEqual(101, map.Count);

        Verify(map);
    }

    [Test]
    public void Real_world_test_TryGetValue()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = new ImTools.Experiments.FHashMap6<Type, string, RefEq<Type>>();

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(Console), "!");

        var found = map.TryGetValue(typeof(Console), out var value);
        Assert.IsTrue(found);
        Assert.AreEqual("!", value);

        Verify(map);
    }

    [Test]
    public void Real_world_test_without_Resize()
    {
        var types = typeof(Dictionary<,>).Assembly.GetTypes().Take(100).ToArray();

        var map = new ImTools.Experiments.FHashMap6<Type, string, RefEq<Type>>(128);

        foreach (var key in types)
            map.AddOrUpdate(key, "a");

        map.AddOrUpdate(typeof(FHashMap6Tests), "!");

        Assert.AreEqual(101, map.Count);

        Verify(map);
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map()
    {
        var map = new FHashMap6<int, string, IntEq>();

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

        Verify(map);
    }

    [Test]
    public void Can_store_and_retrieve_value_from_map_with_Expand_in_the_middle()
    {
        var map = new FHashMap6<int, string, IntEq>(2);

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

        Verify(map);
    }

    [Test]
    public void Can_store_and_get_stored_item_count()
    {
        var map = new FHashMap6<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42 + 32 + 32, "3");

        Assert.AreEqual(2, map.Count);
        Verify(map);
    }

    [Test]
    public void Can_update_a_stored_item_with_new_value()
    {
        var map = new FHashMap6<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(42, "3");

        Assert.AreEqual("3", map.GetValueOrDefault(42));
        Assert.AreEqual(1, map.Count);
        Verify(map);
    }

    [Test]
    public void Can_add_key_with_0_hash_code()
    {
        var map = new FHashMap6<int, string, IntEq>();

        map.AddOrUpdate(0, "aaa");
        map.AddOrUpdate(0 + 32, "2");
        map.AddOrUpdate(0 + 32 + 32, "3");
        Verify(map);

        string value;
        Assert.IsTrue(map.TryGetValue(0, out value));

        Assert.AreEqual("aaa", value);
    }

    [Test]
    public void Can_quickly_find_the_scattered_items_with_the_same_cache()
    {
        var map = new FHashMap6<int, string, IntEq>();

        map.AddOrUpdate(42, "1");
        map.AddOrUpdate(43, "a");
        map.AddOrUpdate(42 + 32, "2");
        map.AddOrUpdate(45, "b");
        map.AddOrUpdate(46, "c");
        map.AddOrUpdate(42 + 32 + 32, "3");
        Verify(map);

        string value;
        Assert.IsTrue(map.TryGetValue(42 + 32, out value));
        Assert.AreEqual("2", value);

        Assert.IsTrue(map.TryGetValue(42 + 32 + 32, out value));
        Assert.AreEqual("3", value);
    }

    // [Test]
    // public void Can_remove_the_stored_item()
    // {
    //     var map = new FHashMap<int, string>();

    //     map.AddOrUpdate(42, "1");
    //     map.AddOrUpdate(42 + 32, "2");
    //     map.AddOrUpdate(42 + 32 + 32, "3");

    //     map.Remove(42 + 32);

    //     Assert.AreEqual(2, map.Count);
    // }
}
