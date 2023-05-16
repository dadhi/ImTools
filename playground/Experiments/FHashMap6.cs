using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImTools.Experiments;

public static class FHashMap6Extensions
{
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap6<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var capacity = map._capacity;
        var hashesAndIndexes = map._hashesAndIndexes;
        var entries = map._entries;

        var items = new Item<K, V>[capacity];
        var indexMask = capacity - 1;

        for (var i = 0; i < hashesAndIndexes.Length; i++)
        {
            var h = hashesAndIndexes[i];
            if (h == 0)
                continue;

            var probe = (byte)(h >> FHashMap6<K, V, TEq>.ProbeCountShift);
            var hashIndex = (capacity + i - (probe - 1)) & indexMask;

            var hashMiddle = (h & FHashMap6<K, V, TEq>.HashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var index = h & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & FHashMap6<K, V, TEq>.HashAndIndexMask;
                heq = kh == hash;
                hkv = $"{kh.b()}:{e.Key}->{e.Value}";
            }
            items[i] = new Item<K, V> { Probe = probe, Hash = hash.b(), HEq = heq, Index = index, HKV = hkv };
        }
        return items;
    }

    public struct Item<K, V>
    {
        public byte Probe;
        public bool HEq;
        public string Hash;
        public string HKV;
        public int Index;
        public bool IsEmpty => Probe == 0;
        public string Output => $"{Probe}|{Hash}{(HEq ? "==" : "!=")}{HKV}";
        public override string ToString() => IsEmpty ? "empty" : Output;
    }
}

#if DEBUG
public class FHashMap6DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap6<K, V, TEq> _map;
    public FHashMap6DebugProxy(FHashMap6<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap6Extensions.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap6DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
// Combine hashes with indexes to economy on memory
public sealed class FHashMap6<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    [DebuggerDisplay("{Key}->{Value}")]
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    public const int DefaultCapacity = 16;
    public const byte MinFreeCapacityShift = 4; // e.g. for the DefaultCapacity 16 >> 4 => 1/16 => 6.25% free space  

    public const byte MaxProbeBits = 5; // 5 bits max
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1;
    public const byte ProbeCountShift = 32 - MaxProbeBits;

    public const int HashAndIndexMask = ~(MaxProbeCount << ProbeCountShift);

    // The _hashesAndIndexes entry is the Int32 which union of: 
    // - 5 high bit (MaxProbeCount == 31) to account for the distance from the ideal index, starting from 1 (to indicate non-empty slot).
    // - H middle bits of the hash, wher H == 32 - 5 - I (lower Index bits)
    // - I lower index bits for the index into the entries array, 0-based.
    // The ProbeCount is starting from 1 to indicate non-empty slot.
    // The removed hash will be actually removed, so we don't use the removed bit here.
    internal int[] _hashesAndIndexes;
    internal Entry[] _entries;
    internal int _capacity;
    internal int _count;

    public int Count => _count;

    public FHashMap6(int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _hashesAndIndexes = new int[capacity];
        _entries = new Entry[capacity];
        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    public V GetValueOrDefault(K key, V defaultValue = default)
    {
        var hash = default(TEq).GetHashCode(key);

        var capacity = _capacity;
        var indexMask = capacity - 1;
        var probeAndHashMask = ~indexMask;
        var hashMask = probeAndHashMask & HashAndIndexMask;

        var hashesAndIndexes = _hashesAndIndexes;
        var hashIndex = hash & indexMask;

        var h = 0;
        for (byte probes = 1; probes <= MaxProbeCount; ++probes)
        {
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
                return defaultValue;

            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | (hash & hashMask)))
            {
                ref var e = ref _entries[h & indexMask];
                if (default(TEq).Equals(e.Key, key))
                    return e.Value;
            }

            if ((h >> ProbeCountShift) < probes)
                break;

            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping around the hashes array 
        }
        return defaultValue;
    }

#if DEBUG
    int _maxProbeCount = 1;
#endif

    public void AddOrUpdate(K key, V value)
    {
        var capacity = _capacity;
        if (capacity - _count <= (capacity >> MinFreeCapacityShift)) // if the free capacity is less free slots 1/16 (6.25%)
            Resize(capacity <<= 1); // double the capacity, using the <<= assinment here to correctly calculate the new capacityMask later

        var hash = default(TEq).GetHashCode(key);

        var indexMask = capacity - 1;
        var hashMask = ~indexMask & HashAndIndexMask;
        var hashesAndIndexes = _hashesAndIndexes;

        var hashIndex  = hash & indexMask;
        var hashMiddle = hash & hashMask;

        var robinHooded = false;
        var h = 0;
        var entryIndex = _count; // by default the new entry index is the last one, but the variable may be updated multiple times by Robin Hood in the loop
        for (byte probes = 1; probes <= MaxProbeCount; ++probes)
        {
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
            {
#if DEBUG
                if (probes > _maxProbeCount)
                {
                    _maxProbeCount = probes;
                    Debug.WriteLine($"MaxProbeCount:{probes} when adding key:`{key}`");
                }
#endif   
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashMiddle | entryIndex;

                ref var e = ref _entries[_count++]; // always add to the original entry
                e.Key = key;
                e.Value = value;
                return;
            }
            var hp = (byte)(h >> ProbeCountShift);
            if (hp < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
                // Robin Hood goes here to steal from the rich (with the less probe count) and give to the poor (with more probes).
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashMiddle | entryIndex;
                probes = hp;
                hashMiddle = h & hashMask;
                entryIndex = h & indexMask;
                robinHooded = true;
            }
            if (!robinHooded & (hp == probes) & ((h & hashMask) == hashMiddle)) // todo: @perf huh, we may either combine or keep only the hash check, cause probes and hashMiddle are parts of the hash 
            {
#if DEBUG
                Debug.WriteLine($"hp < probes `{probes}` and hashes are equal for the added key:`{key}` and existing key:`{_entries[h & indexMask].Key}`");
#endif
                ref var e = ref _entries[h & indexMask]; // check the existing entry key and update the value if the keys are matched
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping around the hashes array
        }

        // todo: @wip going outside of MaxProbeCount?
        Debug.Fail($"Reaching to the max probe count {MaxProbeCount} Resizing to {capacity << 1}");
    }

    public void Resize(int newCapacity)
    {
        // Just resize the _entries without copying/moving, we don't need to move them, becuase we will be moving _entryIndexes instead
        Array.Resize(ref _entries, newCapacity);
        var newHashesAndIndexes = new int[newCapacity];

        var oldHashesAndIndexes = _hashesAndIndexes;
        var oldCapacity = _capacity;
        var oldIndexMask = oldCapacity - 1;
        var newIndexMask = newCapacity - 1;
#if DEBUG
        var sameIndexes = 0;
        var maxProbeCount = 0;
#endif
        for (var i = 0; i < (uint)oldHashesAndIndexes.Length; i++)
        {
            var oldHash = oldHashesAndIndexes[i];
            if (oldHash == 0)
                continue;

            var probePos = (oldHash >> ProbeCountShift) - 1;
            var oldHashIndex = (oldCapacity + i - probePos) & oldIndexMask;

            var newHashIndex = ((oldHash & ~oldIndexMask) | oldHashIndex) & newIndexMask;
            var newHashAndOldIndex = (oldHash & HashAndIndexMask & ~newIndexMask) | (oldHash & oldIndexMask);

            var h = 0;
            for (byte probes = 1; probes <= MaxProbeCount; ++probes) // just in case we are trying up to the max probe count and then do a Resize
            {
                h = newHashesAndIndexes[newHashIndex];
                if (h == 0)
                {
#if DEBUG
                    sameIndexes += i == newHashIndex ? 1 : 0;
                    maxProbeCount = Math.Max(maxProbeCount, probes);
#endif
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndOldIndex;
                    break;
                }
                if ((h >> ProbeCountShift) < probes)
                {
#if DEBUG
                    sameIndexes += i == newHashIndex ? 1 : 0;
#endif
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndOldIndex;
                    newHashAndOldIndex = h & HashAndIndexMask;
                    probes = (byte)(h >> ProbeCountShift);
                }
                newHashIndex = (newHashIndex + 1) & newIndexMask; // `& indexMask` is for wrapping around the hashes array 
            }
        }

        _capacity = newCapacity;
        _hashesAndIndexes = newHashesAndIndexes;
#if DEBUG
        Debug.WriteLine($"Resize {oldCapacity}->{newCapacity} sameIndexes:{sameIndexes}, maxProbeCount:{maxProbeCount}");
        _maxProbeCount = maxProbeCount;

        Debug.Write("old:|");
        foreach (var it in oldHashesAndIndexes)
            Debug.Write(it == 0 ? ".|" : $"{it & oldIndexMask}|");
        Debug.WriteLine("");
        Debug.Write("new:|");
        foreach (var it in newHashesAndIndexes)
            Debug.Write(it == 0 ? ".|" : $"{it & newIndexMask}|");
        Debug.WriteLine("");
#endif
    }
}

public struct DefaultEq<K> : IEqualityComparer<K>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(K x, K y) => x.Equals(y);

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => obj.GetHashCode();
}

public struct IntEq : IEqualityComparer<int>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(int x, int y) => x == y;

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(int obj) => obj;
}

public struct RefEq<K> : IEqualityComparer<K> where K : class 
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(K x, K y) => x == y;

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => obj.GetHashCode();
}
