using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImTools.Experiments;

public static class FHashMap6Extensions
{
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap6<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
#if DEBUG
        Debug.WriteLine($"FirstProbeAdditions: {map.FirstProbeAdditions}, MaxProbes: {map.MaxProbes}");
#endif

        var entries = map._entries;
        var hashesAndIndexes = map._hashesAndIndexes;
        var capacity = hashesAndIndexes.Length;

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
    // todo: @improve make the Entry a type parameter to map and define TEq in terms of the Entry, 
    // todo: @improve it will allow to introduce the Set later without the Value in the Entry, end the Entry may be the Key itself
    [DebuggerDisplay("{Key}->{Value}")]
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    public const int DefaultCapacity = 16;
    public const byte MinFreeCapacityShift = 3; // e.g. for the DefaultCapacity 16 >> 3 => 2, so 2 free slots is 12.5% of the capacity  

    public const byte MaxProbeBits = 5; // 5 bits max
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1;
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int SingleProbeMask = 1 << ProbeCountShift;

    public const int HashAndIndexMask = ~(MaxProbeCount << ProbeCountShift);

    // The _hashesAndIndexes entry is the Int32 which union of: 
    // - 5 high bit (MaxProbeCount == 31) to account for the distance from the ideal index, starting from 1 (to indicate non-empty slot).
    // - H middle bits of the hash, wher H == 32 - 5 - I (lower Index bits)
    // - I lower index bits for the index into the entries array, 0-based.
    // The ProbeCount is starting from 1 to indicate non-empty slot.
    // The removed hash will be actually removed, so we don't use the removed bit here.
    internal int[] _hashesAndIndexes;
    internal Entry[] _entries;
    internal int _entryCount;

    public int[] HashesAndIndexes => _hashesAndIndexes;
    public Entry[] Entries => _entries;
    public int Count => _entryCount;

    public FHashMap6(int capacity = DefaultCapacity)
    {
        _hashesAndIndexes = new int[capacity << 1]; // todo: @wip double the size of entries because hashes are cheap, review later with bms
        _entries = new Entry[capacity];
        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    public V GetValueOrDefault(K key, V defaultValue = default)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var capacity = hashesAndIndexes.Length;

        var indexMask = capacity - 1;
        var probeAndHashMask = ~(capacity - 1);
        var hashMask = ~(capacity - 1) & HashAndIndexMask;

        var hashIndex = hash & indexMask;

        var h = 0;
        for (byte probes = 1; probes <= MaxProbeCount; ++probes)
        {
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
                return defaultValue;

            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | (hash & hashMask)))
            {
                ref var e = ref _entries[h & indexMask]; // todo: @perf wrap access into the interface to separate the entries abstraction
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
    public int MaxProbes = 1;
    public int FirstProbeAdditions = 0;
#endif

    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var hashesCapacity = hashesAndIndexes.Length;

// todo: @wip, @fixme the test Real_world_test is failing
//         var indexMask = hashesCapacity - 1;
//         var hi = hash & indexMask;
//         if (hashesAndIndexes[hi] == 0)
//         {
// #if DEBUG
//             ++FirstProbeAdditions;
// #endif
//             var entryCount = _entryCount;
//             _entryCount = entryCount + 1;
//             hashesAndIndexes[hi] = SingleProbeMask | (hash & (~indexMask & HashAndIndexMask)) | entryCount;

//             if (entryCount + 1 >= _entries.Length)
//             {
//                 Array.Resize(ref _entries, _entries.Length << 1);
// #if DEBUG
//                 Debug.WriteLine($"Resize Entries to {_entries.Length}");
// #endif
//             }
            
//             ref var e = ref _entries[entryCount];
//             e.Key = key;
//             e.Value = value;
//             return;
//         }

        if (hashesCapacity - _entryCount <= (hashesCapacity >> MinFreeCapacityShift)) // if the free capacity is less free slots 1/16 (6.25%)
        {
            _hashesAndIndexes = hashesAndIndexes = DoubleSize(_hashesAndIndexes);
            hashesCapacity = hashesAndIndexes.Length;
#if DEBUG
            Debug.WriteLine($"Resize _hashesAndIndexes to {hashesCapacity} because the _entryCount:{_entryCount}");
#endif
        }

        var hashIndexMask = hashesCapacity - 1;
        var hashMiddleMask = ~hashIndexMask & HashAndIndexMask;

        var hashIndex = hash & hashIndexMask;
        var hashMiddle = hash & hashMiddleMask;

        var robinHooded = false;
        var h = 0;
        var entryIndex = _entryCount; // by default the new entry index is the last one, but the variable may be updated multiple times by Robin Hood in the loop
        for (byte probes = 1; probes <= MaxProbeCount; ++probes, hashIndex = (hashIndex + 1) & hashIndexMask)
        {
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
            {
#if DEBUG
                if (probes == 1)
                    ++FirstProbeAdditions;
                if (probes > MaxProbes)
                {
                    MaxProbes = probes;
                    Debug.WriteLine($"MaxProbes increased to {probes} when adding key:`{key}`");
                }
#endif   
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashMiddle | entryIndex;

                if (_entryCount + 1 >= _entries.Length)
                    Array.Resize(ref _entries, _entries.Length << 1);

                ref var e = ref _entries[_entryCount++];
                e.Key = key;
                e.Value = value;
                return;
            }

            var hp = (byte)(h >> ProbeCountShift);
            if (hp < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
#if DEBUG
                Debug.WriteLine($"Robin Hood from hp:{hp} because probes:{probes}");
#endif
                // Robin Hood goes here to steal from the rich (with the less probe count) and give to the poor (with more probes).
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashMiddle | entryIndex;
                probes = hp;
                hashMiddle = h & hashMiddleMask;
                entryIndex = h & hashIndexMask;
                robinHooded = true;
            }
            
            // todo: @perf avoid robinHooded by breaking out of the loop and swapping the existing hashes in the next loop
            if (!robinHooded & (hp == probes) & ((h & hashMiddleMask) == hashMiddle)) // todo: @perf huh, we may either combine or keep only the hash check, cause probes and hashMiddle are parts of the hash 
            {
#if DEBUG
                Debug.WriteLine($"hp < probes `{probes}` and hashes are equal for the added key:`{key}` and existing key:`{_entries[h & hashIndexMask].Key}`");
#endif
                ref var e = ref _entries[h & hashIndexMask]; // check the existing entry key and update the value if the keys are matched
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
        }

        // todo: @wip going outside of MaxProbeCount?
        Debug.Fail($"Reaching to the max probe count {MaxProbeCount} Resizing to {hashesCapacity << 1}");
    }

    internal static int[] DoubleSize(int[] oldHashesAndIndexes)
    {
        var oldCapacity = oldHashesAndIndexes.Length;
        var newCapacity = oldCapacity << 1;
        var newHashesAndIndexes = new int[newCapacity];

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

            // todo: @perf @wip review how we clearing the additional index bit
            var probePos = (oldHash >> ProbeCountShift) - 1;
            var oldHashIndex = (oldCapacity + i - probePos) & oldIndexMask;

            var newHashIndex = ((oldHash & ~oldIndexMask) | oldHashIndex) & newIndexMask;
            var newHashAndOldIndex = (oldHash & HashAndIndexMask & ~newIndexMask) | (oldHash & oldIndexMask);

            var h = 0;
            for (byte probes = 1;; ++probes) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
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

#if DEBUG
        Debug.Write("old:|");
        foreach (var it in oldHashesAndIndexes)
            Debug.Write(it == 0 ? ".|" : $"{it & oldIndexMask}|");
        Debug.WriteLine("");
        Debug.Write("new:|");
        foreach (var it in newHashesAndIndexes)
            Debug.Write(it == 0 ? ".|" : $"{it & newIndexMask}|");
        Debug.WriteLine("");
#endif
        return newHashesAndIndexes;
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
