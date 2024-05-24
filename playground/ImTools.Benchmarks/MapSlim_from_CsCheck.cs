namespace Playground;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MapSlim<K, V> : IReadOnlyCollection<KeyValuePair<K, V>> where K : IEquatable<K>
{
    struct Entry { internal int Bucket; internal int Next; internal K Key; internal V Value; }
    static class Holder { internal static Entry[] Initial = new Entry[1]; }
    int count;
    Entry[] entries;
    public MapSlim() => entries = Holder.Initial;

    public MapSlim(int capacity)
    {
        if (capacity < 2) capacity = 2;
        entries = new Entry[PowerOf2(capacity)];
    }

    public MapSlim(IEnumerable<KeyValuePair<K, V>> items)
    {
        entries = new Entry[2];
        foreach (var (k, v) in items) this[k] = v;
    }

    public int Count => count;

    static int PowerOf2(int capacity)
    {
        if ((capacity & (capacity - 1)) == 0) return capacity;
        int i = 2;
        while (i < capacity) i <<= 1;
        return i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Entry[] Resize()
    {
        var oldEntries = entries;
        if (oldEntries.Length == 1) return entries = new Entry[2];
        var newEntries = new Entry[oldEntries.Length * 2];
        for (int i = 0; i < oldEntries.Length;)
        {
            var bucketIndex = oldEntries[i].Key.GetHashCode() & (newEntries.Length - 1);
            newEntries[i].Next = newEntries[bucketIndex].Bucket - 1;
            newEntries[i].Key = oldEntries[i].Key;
            newEntries[i].Value = oldEntries[i].Value;
            newEntries[bucketIndex].Bucket = ++i;
        }
        return entries = newEntries;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void AddItem(K key, V value, int hashCode)
    {
        var i = count;
        var ent = entries;
        if (ent.Length == i || ent.Length == 1) ent = Resize();
        var bucketIndex = hashCode & (ent.Length - 1);
        ent[i].Next = ent[bucketIndex].Bucket - 1;
        ent[i].Key = key;
        ent[i].Value = value;
        ent[bucketIndex].Bucket = ++count;
    }

    public V this[K key]
    {
        get
        {
            var ent = entries;
            var hashCode = key.GetHashCode();
            var i = ent[hashCode & (ent.Length - 1)].Bucket - 1;
            while (i >= 0 && !key.Equals(ent[i].Key)) i = ent[i].Next;
            return ent[i].Value;
        }
        set
        {
            var ent = entries;
            var hashCode = key.GetHashCode();
            var i = ent[hashCode & (ent.Length - 1)].Bucket - 1;
            while (i >= 0 && !key.Equals(ent[i].Key)) i = ent[i].Next;
            if (i >= 0) ent[i].Value = value;
            else AddItem(key, value, hashCode);
        }
    }

    public ref V GetValueOrNullRef(K key)
    {
        var ent = entries;
        var hashCode = key.GetHashCode();
        var i = ent[hashCode & (ent.Length - 1)].Bucket - 1;
        while (i >= 0 && !key.Equals(ent[i].Key)) i = ent[i].Next;
        if (i >= 0)
        {
            return ref ent[i].Value;
        }
        else
        {
            i = count;
            if (ent.Length == i || ent.Length == 1) ent = Resize();
            var bucketIndex = hashCode & (ent.Length - 1);
            ent[i].Next = ent[bucketIndex].Bucket - 1;
            ent[i].Key = key;
            ent[i].Value = default!;
            ent[bucketIndex].Bucket = ++count;
            return ref ent[i].Value;
        }
    }

    public int IndexOf(K key)
    {
        var ent = entries;
        var hashCode = key.GetHashCode();
        var i = ent[hashCode & (ent.Length - 1)].Bucket - 1;
        while (i >= 0 && !key.Equals(ent[i].Key)) i = ent[i].Next;
        return i;
    }

    public K Key(int i) => entries[i].Key;
    public V Value(int i) => entries[i].Value;

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
            yield return new(entries[i].Key, entries[i].Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}