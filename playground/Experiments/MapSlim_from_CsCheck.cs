namespace ImTools.Experiments;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MapSlim<K, V> : IReadOnlyCollection<KeyValuePair<K, V>> where K : IEquatable<K>
{
    struct Entry
    {
        internal int BucketIndexPlusOne;
        internal int NextBucketIndex;
        internal K Key;
        internal V Value;
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

    static int PowerOf2(int capacity)
    {
        if ((capacity & (capacity - 1)) == 0)
            return capacity;
        var i = 2;
        while (i < capacity)
            i <<= 1;
        return i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Entry[] Resize()
    {
        var oldEntries = _entries;
        if (oldEntries.Length == 1)
            return _entries = new Entry[2];

        var newEntries = new Entry[oldEntries.Length * 2];
        for (var i = 0; i < oldEntries.Length;)
        {
            var bucketIndex = oldEntries[i].Key.GetHashCode() & (newEntries.Length - 1);

            newEntries[i].NextBucketIndex = newEntries[bucketIndex].BucketIndexPlusOne - 1;

            newEntries[i].Key = oldEntries[i].Key;
            newEntries[i].Value = oldEntries[i].Value;

            newEntries[bucketIndex].BucketIndexPlusOne = ++i;
        }

        return _entries = newEntries;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddItem(K key, V value, int hashCode)
    {
        var newIndex = _count++;
        var ent = _entries;
        if (ent.Length == newIndex | ent.Length == 1)
            ent = Resize();

        var bucketIndex = hashCode & (ent.Length - 1);

        ent[newIndex].NextBucketIndex = ent[bucketIndex].BucketIndexPlusOne - 1;

        // todo: @wip save the hash code
        ent[newIndex].Key = key;
        ent[newIndex].Value = value;

        ent[bucketIndex].BucketIndexPlusOne = newIndex + 1;
    }

    public V this[K key]
    {
        get
        {
            var ent = _entries;
            var hashCode = key.GetHashCode();
            var i = ent[hashCode & (ent.Length - 1)].BucketIndexPlusOne - 1;
            while (i >= 0 && !key.Equals(ent[i].Key))
                i = ent[i].NextBucketIndex;
            return ent[i].Value;
        }
        set
        {
            var ent = _entries;
            var hashCode = key.GetHashCode();
            var i = ent[hashCode & (ent.Length - 1)].BucketIndexPlusOne - 1;
            while (i >= 0 && !key.Equals(ent[i].Key))
                i = ent[i].NextBucketIndex;
            if (i >= 0)
                ent[i].Value = value;
            else
                AddItem(key, value, hashCode);
        }
    }

    public ref V GetValueOrNullRef(K key)
    {
        var ent = _entries;
        var hashCode = key.GetHashCode();
        var n = ent[hashCode & (ent.Length - 1)].BucketIndexPlusOne - 1;

        while (n >= 0 && !key.Equals(ent[n].Key))
            n = ent[n].NextBucketIndex;

        if (n >= 0)
            return ref ent[n].Value;

        n = _count;
        if (ent.Length == n | ent.Length == 1)
            ent = Resize();

        var bucketIndex = hashCode & (ent.Length - 1);
        ent[n].NextBucketIndex = ent[bucketIndex].BucketIndexPlusOne - 1;
        ent[n].Key = key;
        ent[n].Value = default!;
        ent[bucketIndex].BucketIndexPlusOne = ++_count;
        return ref ent[n].Value;
    }

    public int IndexOf(K key)
    {
        var ent = _entries;
        var hashCode = key.GetHashCode();
        var i = ent[hashCode & (ent.Length - 1)].BucketIndexPlusOne - 1;
        while (i >= 0 && !key.Equals(ent[i].Key))
            i = ent[i].NextBucketIndex;
        return i;
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