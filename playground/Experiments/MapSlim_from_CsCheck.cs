namespace ImTools.Experiments;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MapSlim<K, V> : IReadOnlyCollection<KeyValuePair<K, V>> where K : IEquatable<K>
{
    public struct Entry
    {
        public int PayloadEntryIndexPlusOne;
        public int PrevPayloadEntryIndex;
        public int Hash;
        public K Key;
        public V Value;
    }

    // todo: @wip why do we need an empty single entry, is it to avoid lookup condition for no entries case?
    static class Holder { internal static Entry[] Initial = new Entry[1]; }
    int _count;
    Entry[] _entries;
    public MapSlim() => _entries = Holder.Initial;

    public MapSlim(int capacity)
    {
        if (capacity < 2)
            capacity = 2;
        _entries = new Entry[PowerOf2(capacity)];
    }

    public MapSlim(IEnumerable<KeyValuePair<K, V>> items)
    {
        _entries = new Entry[2];
        foreach (var (k, v) in items)
            this[k] = v;
    }

    public int Count => _count;

    private static int PowerOf2(int capacity)
    {
        if ((capacity & (capacity - 1)) == 0)
            return capacity;
        var i = 2;
        while (i < capacity)
            i <<= 1;
        return i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Entry[] ResizeAndCopy(Entry[] entries)
    {
        if (entries.Length == 1)
            return new Entry[2];

        var newEntries = new Entry[entries.Length * 2];
        for (var i = 0; i < entries.Length; ++i)
        {
            var e = entries[i];
            var idealIndex = e.Hash & (newEntries.Length - 1);
            FillEntry(ref newEntries[idealIndex], ref newEntries[i], i, e.Hash, e.Key, e.Value);
        }
        return newEntries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillEntry(ref Entry idealEntry, ref Entry payloadEntry, int payloadEntryIndex, int hash, K key, V value)
    {
        // establish a link
        payloadEntry.PrevPayloadEntryIndex = idealEntry.PayloadEntryIndexPlusOne - 1;
        idealEntry.PayloadEntryIndexPlusOne = payloadEntryIndex + 1;

        payloadEntry.Hash = hash;
        payloadEntry.Key = key;
        payloadEntry.Value = value;
    }

    public int IndexOf(K key, int hash, out Entry[] entries)
    {
        entries = _entries;
        var i = entries[hash & (entries.Length - 1)].PayloadEntryIndexPlusOne - 1;
        while (i >= 0)
        {
            var entry = entries[i];
            if (entry.Hash == hash && key.Equals(entry.Key))
                break;
            i = entry.PrevPayloadEntryIndex;
        }
        return i;
    }

    /// <summary>Can make the map to contain the same key-hash entries</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int JustAddDefault(K key, int hash, out Entry[] entries)
    {
        entries = _entries;
        var addIndex = _count++;

        if (entries.Length == addIndex | entries.Length == 1)
            _entries = entries = ResizeAndCopy(entries);

        var idealIndex = hash & (entries.Length - 1);
        FillEntry(ref entries[idealIndex], ref entries[addIndex], addIndex, hash, key, default);
        return addIndex;
    }

    public ref V GetOrAddValue(K key, int hash)
    {
        var i = IndexOf(key, hash, out var entries);
        if (i == -1)
            i = JustAddDefault(key, hash, out entries);
        return ref entries[i].Value;
    }

    public V this[K key]
    {
        get
        {
            var i = IndexOf(key, key.GetHashCode(), out var entries);
            if (i == -1)
                throw new ArgumentOutOfRangeException($"The key `${key}` is not found in map `${this}`");
            return entries[i].Value;
        }
        set
        {
            GetOrAddValue(key, key.GetHashCode()) = value;
        }
    }

    public K Key(int i) => _entries[i].Key;
    public V Value(int i) => _entries[i].Value;

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
            yield return new(_entries[i].Key, _entries[i].Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class MapSlim
{
    public static int IndexOf<K, V>(this MapSlim<K, V> map, K key, int hash) where K : IEquatable<K>
        => map.IndexOf(key, hash, out _);

    public static int IndexOf<K, V>(this MapSlim<K, V> map, K key) where K : IEquatable<K>
        => map.IndexOf(key, key.GetHashCode(), out _);

    public static bool TryGetValue<K, V>(this MapSlim<K, V> map, K key, int hash, out V value) where K : IEquatable<K>
    {
        var i = map.IndexOf(key, hash, out var entries);
        value = i != -1 ? entries[i].Value : default;
        return i != -1;
    }

    public static bool TryGetValue<K, V>(this MapSlim<K, V> map, K key, out V value) where K : IEquatable<K>
        => TryGetValue(map, key, key.GetHashCode(), out value);

    public static void AddOrUpdate<K, V>(this MapSlim<K, V> map, K key, int hash, V value) where K : IEquatable<K> =>
        map.GetOrAddValue(key, hash) = value;

    public static void AddOrUpdate<K, V>(this MapSlim<K, V> map, K key, V value) where K : IEquatable<K> =>
        map.GetOrAddValue(key, key.GetHashCode()) = value;

    public static void AddPossibleDuplicate<K, V>(this MapSlim<K, V> map, K key, V value) where K : IEquatable<K>
    {
        var i = map.JustAddDefault(key, key.GetHashCode(), out var entries);
        entries[i].Value = value;
    }
}