﻿using System;
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

    public const int DefaultSeedCapacity = 16;
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

    public FHashMap6(int seedCapacity = DefaultSeedCapacity)
    {
        // double the size of the hashes, because they are cheap, 
        // this will also provide the flexibility of independence of the sizes of hashes and entries
        _hashesAndIndexes = new int[seedCapacity << 1];
        
        _entries = new Entry[seedCapacity];

        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    // todo: @inline the method
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;

        // todo: @perf test what if we pre-calculate the mask on addition and store/load them from the fields 
        // todo: @perf or at least the `_indexMask`, so it will be 2 lines vs 4 `var probeAndHashMask = ~_indexMask; var hashMiddle = hash & probeAndHashMask &Has hAndIndexMask;` 
        var capacity = hashesAndIndexes.Length;
        var indexMask = capacity - 1;
        var probeAndHashMask = ~(capacity - 1);
        var hashMiddle = hash & (~(capacity - 1) & HashAndIndexMask);

        // super important to have it in a such `for` loop, 
        // because it makes it 2x faster than putting the `(h >> ProbeCountShift) < probes` inside the loop as a condition with break
        var hashIndex = hash & indexMask;
        var h = hashesAndIndexes[hashIndex];
        for (byte probes = 1; (h >> ProbeCountShift) >= probes; ++probes)
        {
            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref _entries[h & indexMask]; // todo: @perf wrap access into the interface to separate the entries abstraction
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            hashIndex = (hashIndex + 1) & indexMask;
            h = hashesAndIndexes[hashIndex];
        }
        value = default;
        return false;
    }

    public V GetValueOrDefault(K key, V defaultValue = default)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var capacity = hashesAndIndexes.Length;

        var hashIndexMask = capacity - 1;
        var probeAndHashMask = ~(capacity - 1);
        var hashMask = ~(capacity - 1) & HashAndIndexMask;

        var hashIndex = hash & hashIndexMask;

        var h = 0;
        for (byte probes = 1; ; ++probes)
        {
            h = hashesAndIndexes[hashIndex];
            if ((h >> ProbeCountShift) < probes)
                break;

            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | (hash & hashMask)))
            {
                ref var e = ref _entries[h & hashIndexMask]; // todo: @perf wrap access into the interface to separate the entries abstraction
                if (default(TEq).Equals(e.Key, key))
                    return e.Value;
            }
            hashIndex = (hashIndex + 1) & hashIndexMask; // `& indexMask` is for wrapping around the hashes array 
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
        var hashAndEntryIndex = 0;
        var h = 0;
        byte probes = 1;
        for (; ; ++probes, hashIndex = (hashIndex + 1) & hashIndexMask)
        {
            Debug.Assert(probes <= MaxProbeCount, $"DEBUG ASSERT FAILED probes:{probes} <= MaxProbeCount:{MaxProbeCount}");

            hashAndEntryIndex = (probes << ProbeCountShift) | hashMiddle;

            h = hashesAndIndexes[hashIndex];
            if (h == 0)
            {
#if DEBUG
                if (probes == 1)
                    ++FirstProbeAdditions;
                if (probes > MaxProbes)
                {
                    MaxProbes = probes;
                    Debug.WriteLine($"NEW: MaxProbes increased to {probes} when adding key:`{key}`");
                }
#endif   
                var entryCount = _entryCount;
                hashesAndIndexes[hashIndex] = hashAndEntryIndex | entryCount;

                // todo: @wip wrap in the abstraction together with the check and Resize, 
                // because the abstraction may decide to avoid it completely, e.g. by pre-allocation of enough entries
                if (entryCount + 1 >= _entries.Length)
                    Array.Resize(ref _entries, _entries.Length << 1);
                ref var e = ref _entries[entryCount];
                e.Key = key;
                e.Value = value;

                _entryCount = entryCount + 1;
                return;
            }

            // the check is here, because it is cheaper than hash comparison below and current ont may result in early break/exit
            var hp = h >> ProbeCountShift;
            if (hp < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
                // Robin Hood goes here to steal from the rich (with the less probe count) and give to the poor (with more probes).
                var entryCount = _entryCount;
                hashesAndIndexes[hashIndex] = hashAndEntryIndex | entryCount;
                hashAndEntryIndex = h & HashAndIndexMask;
                probes = (byte)hp;

                if (entryCount + 1 >= _entries.Length)
                    Array.Resize(ref _entries, _entries.Length << 1);
                ref var e = ref _entries[entryCount];
                e.Key = key;
                e.Value = value;

                _entryCount = entryCount + 1;
                break;
            }

            if ((h & ~hashIndexMask) == hashAndEntryIndex)
            {
#if DEBUG
                Debug.WriteLine($"NEW: hp < probes `{probes}` and hashes are equal for the added key:`{key}` and existing key:`{_entries[h & hashIndexMask].Key}`");
#endif
                ref var e = ref _entries[h & hashIndexMask]; // check the existing entry key and update the value if the keys are matched
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
        }

        // todo: @simplify factor out into the method X
        while (true)
        {
            ++probes;
            hashIndex = (hashIndex + 1) & hashIndexMask;
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
            {
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashAndEntryIndex;
                return;
            }
            var hp = h >> ProbeCountShift;
            if (hp < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashAndEntryIndex;
                hashAndEntryIndex = h & HashAndIndexMask;
                probes = (byte)hp;
            }
        }
    }

    internal static int[] DoubleSize(int[] oldHashesAndIndexes)
    {
        var oldCapacity = oldHashesAndIndexes.Length;
        var newCapacity = oldCapacity << 1;
        var newHashesAndIndexes = new int[newCapacity];

        var oldHashIndexMask = oldCapacity - 1;
        var newHashIndexMask = newCapacity - 1;
        var newHashMiddleMask = ~newHashIndexMask & HashAndIndexMask;

        for (var i = 0; i < (uint)oldHashesAndIndexes.Length; i++)
        {
            var oldHash = oldHashesAndIndexes[i];
            if (oldHash == 0)
                continue;

            // get the new hash index for the new capacity by restoring the (possibly wrapped) 
            // probes count (and therefore the distance from the ideal hash position) 
            var distance = (oldHash >> ProbeCountShift) - 1;
            var oldHashIndex = (oldCapacity + i - distance) & oldHashIndexMask;
            var restoredOldHash = (oldHash & ~oldHashIndexMask) | oldHashIndex;
            var newHashIndex = restoredOldHash & newHashIndexMask;

            // erasing the next to capacity bit, given the capacity was 4 and now it is 4 << 1 = 8, 
            // we are erasing the 3rd bit to store the new count in it. 
            var newHashAndEntryIndex = oldHash & HashAndIndexMask & ~oldCapacity;

            var h = 0;
            // todo: @simplify factor out into the method X
            for (byte probes = 1; ; ++probes, newHashIndex = (newHashIndex + 1) & newHashIndexMask) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
            {
                h = newHashesAndIndexes[newHashIndex];
                if (h == 0)
                {
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndEntryIndex;
                    break;
                }
                var hp = h >> ProbeCountShift;
                if (hp < probes)
                {
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndEntryIndex;
                    newHashAndEntryIndex = h & HashAndIndexMask;
                    probes = (byte)hp; // todo: @perf Unsafe.As<int, byte>(ref hp)?
                }
            }
        }
#if DEBUG
        // this will output somthing like this for capacity 32:
        // -_-112--___12-3--44452-223-42311
        // todo: @perf can we move the non`-` hashes in a one loop if possible or non move at all? 
        Debug.Write("before resize:");
        foreach (var it in oldHashesAndIndexes)
            Debug.Write(it == 0 ? "_"
            : (it & oldCapacity) != 0 ? "-"
            : (it >> ProbeCountShift).ToString());

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
    public bool Equals(K x, K y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => obj.GetHashCode();
}
