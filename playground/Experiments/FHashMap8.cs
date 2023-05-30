using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Numerics;
#endif
namespace ImTools.Experiments;

public static class FHashMap8Extensions
{
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap8<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var entries = map._entries;
        var hashesAndIndexes = map._hashesAndIndexes;
        var indexMask = map.IndexMask;
        var indexCapacity = indexMask + 1;
        var hashMiddleMask = ~indexMask & FHashMap8<K, V, TEq>.HashAndIndexMask;
        var probeShift = FHashMap8<K, V, TEq>.ProbeCountShift;

        var items = new Item<K, V>[indexCapacity];

        for (var i = 0; (uint)i < (uint)hashesAndIndexes.Length; i++)
        {
            var hs = hashesAndIndexes[i];
            if (hs == 0)
                continue;

            var hHigh = (int)(hs >> 32); // get the higher 32 bits of the double-hash
            var hHighIndex = i << 1; // 0->0, 1->2, 2->4, 3->6

            // calculate the index from the probe count taking the wrap over arra into account
            var probe = hHigh >> probeShift;
            var deltaFromTheIdealIndex = probe - 1;
            var hashIndex = (indexCapacity + hHighIndex - deltaFromTheIdealIndex) & indexMask;

            var hash = (hHigh & hashMiddleMask) | hashIndex;
            var index = hHigh & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & FHashMap8<K, V, TEq>.HashAndIndexMask;
                heq = kh == hash;
                hkv = $"{kh.b()}:{e.Key}->{e.Value}";
            }
            items[i << 1] = new Item<K, V> { Probe = probe, Hash = hash.b(), HEq = heq, Index = index, HKV = hkv };

            var hLow = (int)hs;
            var hLowIndex = (i << 1) + 1; // 0->1, 1->3, 2->5

            probe = hLow >> probeShift;
            deltaFromTheIdealIndex = probe - 1;
            hashIndex = (indexCapacity + hLowIndex - deltaFromTheIdealIndex) & indexMask;

            hash = (hLow & hashMiddleMask) | hashIndex;
            index = hLow & indexMask;

            hkv = null;
            heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & FHashMap8<K, V, TEq>.HashAndIndexMask;
                heq = kh == hash;
                hkv = $"{kh.b()}:{e.Key}->{e.Value}";
            }
            items[(i << 1) + 1] = new Item<K, V> { Probe = probe, Hash = hash.b(), HEq = heq, Index = index, HKV = hkv };
        }
        return items;
    }

    public struct Item<K, V>
    {
        public int Probe;
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
public class FHashMap8DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap8<K, V, TEq> _map;
    public FHashMap8DebugProxy(FHashMap8<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap8Extensions.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap8DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
public sealed class FHashMap8<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    // todo: @improve make the Entry a type parameter to map and define TEq in terms of the Entry, 
    // todo: @improve it will allow to introduce the Set later without the Value in the Entry, end the Entry may be the Key itself
    [DebuggerDisplay("{Key}->{Value}")]
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    public const int DefaultSeedCapacity = 8;
    public const byte MinFreeCapacityShift = 3; // e.g. for the DefaultCapacity=16 >> 3 => 2, so 2 free slots is 12.5% of the capacity  
    public const byte MaxProbeBits = 5; // 5 bits max, e.g. 31 (11111)
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1; // e.g. 31 (11111) for the 5 bits
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int ProbesMask = MaxProbeCount << ProbeCountShift;
    public const int HashAndIndexMask = ~ProbesMask;

    // The _hashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16). 
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @add For the removed hash we won't use the tumbstone but will actually remove the hash.
    internal long[] _hashesAndIndexes;
    internal Entry[] _entries;
    internal int _indexMask; // pre-calculated and saved on the DoubleSize for the performance
    internal int _entryCount;

    // todo: @wip remove or hide or whatever
    public long[] HashesAndIndexes => _hashesAndIndexes;
    // todo: @wip remove or hide or whatever
    public int IndexMask => _indexMask;

    public Entry[] Entries => _entries;
    public int Count => _entryCount;

    public FHashMap8(uint seedCapacity = DefaultSeedCapacity)
    {
        if (seedCapacity < 2)
            seedCapacity = 2;
#if NET7_0_OR_GREATER
        else if (!BitOperations.IsPow2(seedCapacity))
            seedCapacity = BitOperations.RoundUpToPowerOf2(seedCapacity);
#endif
        // double the size of the hashes, because they are cheap, 
        // this will also provide the flexibility of independence of the sizes of hashes and entries
        _hashesAndIndexes = new long[seedCapacity];
        _indexMask = (int)((seedCapacity << 1) - 1); // double the capacity and subtract 1 to have b0..01111 for capacity 8
        _entries = new Entry[seedCapacity];
        _entryCount = 0;

        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_hashesAndIndexes);
#else
        var hashesAndIndexes = _hashesAndIndexes;
#endif
        var indexMask = _indexMask;

        var probesAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var probes = 1;
        var index = hash & indexMask;
#if NET7_0_OR_GREATER
        var hs = Unsafe.Add(ref hashesAndIndexes, index);
#else
        var hs = hashesAndIndexes[index];
#endif
        var shift = (~(index & 1) & 1) << 5; // for index b???0 -> 32, for index b???1 -> 0, e.g. 0 -> 32, 1 -> 0 
        while (true)
        {
            var h = (int)(hs >> shift);
            if ((h & probesAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
#if NET7_0_OR_GREATER
                ref var e = ref Unsafe.Add(ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries), h & indexMask);
#else
                ref var e = ref _entries[h & indexMask];
#endif
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            if ((h >> ProbeCountShift) < probes)
                break;

            if (shift == 0)
            {
                index = (index + 1) & indexMask; // todo: @perf can be optimized
#if NET7_0_OR_GREATER
                hs = Unsafe.Add(ref hashesAndIndexes, index >> 1);
#else
                hs = hashesAndIndexes[index >> 1];
#endif
            }
            shift = ~shift & 32; // 32 -> 0, 0 -> 32
            ++probes;
        }
        value = default;
        return false;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

#if DEBUG
    public int MaxProbes = 1;
#endif

    // todo: @perf consider using GetArrayDataReference the same as Lookup methods
    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var indexMask = _indexMask;
        if (indexMask - _entryCount <= (indexMask >> MinFreeCapacityShift)) // if the free capacity is less free slots 1/16 (6.25%)
        {
            _hashesAndIndexes = hashesAndIndexes = Resize(_hashesAndIndexes, indexMask);
            _indexMask = indexMask = (indexMask << 1) | 1;
        }

        var hashMiddleMask = ~indexMask & HashAndIndexMask;
        var hashMiddle = hash & hashMiddleMask;
        var index = hash & indexMask;

        int probes = 1;
        int h;
        // hashIndex == 2(b10) in 0:[0|1],1:[2|3] -> 2>>1 -> 1 in 0:[__],1:[2|3] -> shift 2(b10)==32 in 0:[__],1:[2_]
        var hs = hashesAndIndexes[index >> 1];
        var shift = (~(index & 1) & 1) << 5; // for index b???0 -> 32, for index b???1 -> 0, e.g. 0 -> 32, 1 -> 0 
        while (true)
        {
            Debug.Assert(probes <= MaxProbeCount, $"[AddOrUpdate] DEBUG ASSERT FAILED: probes {probes} <= MaxProbeCount {MaxProbeCount}");

            h = (int)(hs >> shift);
            var hProbes = h >> ProbeCountShift;

            // this check is also implicitly breaks if `h == 0` to proceed inserting new entry 
            if (hProbes < probes)
                break;

            if (hProbes == probes && (h & hashMiddleMask) == hashMiddle)
            {
                ref var matchedEntry = ref _entries[h & indexMask];
#if DEBUG
                Debug.WriteLine($"[AddOrUpdate] PROBES AND HASH MATCH: probes {probes}, compare new key `{key}` with matched key:`{matchedEntry.Key}`");
#endif
                if (default(TEq).Equals(matchedEntry.Key, key))
                {
                    matchedEntry.Value = value;
                    return;
                }
            }
            if (shift == 0)
            {
                index = (index + 1) & indexMask;
                hs = hashesAndIndexes[index >> 1]; // todo: @perf can be optimized
            }
            shift = ~shift & 32; // 32 -> 0, 0 -> 32
            ++probes;
        }

        var newEntryIndex = _entryCount;
        hashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | hashMiddle | newEntryIndex) << shift);

#if DEBUG
        if (probes > MaxProbes)
        {
            MaxProbes = probes;
            Debug.WriteLine($"AddOrUpdate: MaxProbes now is {MaxProbes}");
        }
#endif

        if (newEntryIndex >= _entries.Length)
            Array.Resize(ref _entries, _entries.Length * 2);
        ref var e = ref _entries[newEntryIndex];
        e.Key = key;
        e.Value = value;
        _entryCount = newEntryIndex + 1;
        if (h == 0)
            return;

        // Robin Hood loop - the old hash to be re-inserted with the increased probe count
        probes = (h >> ProbeCountShift) + 1;
        var hashWithoutProbes = h & HashAndIndexMask;
        if (shift == 0)
        {
            index = (index + 1) & indexMask;
            hs = hashesAndIndexes[index >> 1];
        }
        shift = ~shift & 32; // 32 -> 0, 0 -> 32
        while (true)
        {
            h = (int)(hs >> shift);
            if (h == 0)
            {
                hashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | hashWithoutProbes) << shift);
                return;
            }
            if ((h >> ProbeCountShift) < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
                hashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | hashWithoutProbes) << shift);
                hashWithoutProbes = h & HashAndIndexMask;
                probes = (h >> ProbeCountShift);
            }
            if (shift == 0)
            {
                index = (index + 1) & indexMask;
                hs = hashesAndIndexes[index >> 1];
            }
            shift = ~shift & 32; // 32 -> 0, 0 -> 32
            ++probes;
        }
    }

    internal static long[] Resize(long[] oldHashesAndIndexes, int oldIndexMask)
    {
        // in terms of the individual hashes and not in terms of the slots, which is `capacity >> 1`
        var oldCapacity = oldIndexMask + 1;
        var newCapacity = oldCapacity << 1;
#if DEBUG
        Debug.WriteLine($"RESIZE _hashesAndIndexes, double the capacity: {oldCapacity} -> {newCapacity}");
#endif

        var newHashesAndIndexes = new long[newCapacity >> 1];

        var newIndexMask = newCapacity - 1;
        var newHashMiddleMask = ~newIndexMask & HashAndIndexMask;
        var newHashWithoutProbesMask = HashAndIndexMask & ~oldCapacity; // erase the old capacity bit

        // todo: @perf find the way to avoid copying the hashes with 0 next bit and with ideal+ probe count
        for (var i = 0; (uint)i < (uint)oldHashesAndIndexes.Length; ++i)
        {
            var oldHashes = oldHashesAndIndexes[i];
            if (oldHashes == 0)
                continue; // 2 empty slots can be skipped

            var oldHash1 = (int)(oldHashes >> 32);
            if (oldHash1 != 0)
            {
                // get the new hash index for the new capacity by restoring the (possibly wrapped) 
                // probes count (and therefore the distance from the ideal hash position) 
                var distance = (oldHash1 >> ProbeCountShift) - 1;
                var oldHashIndex = (oldCapacity + (i << 1) - distance) & oldIndexMask;
                var restoredOldHash = (oldHash1 & ~oldIndexMask) | oldHashIndex;
                var index = restoredOldHash & newIndexMask;

                // erasing the next to capacity bit, given the capacity was 4 and now it is 4 << 1 = 8, 
                // we are erasing the 3rd bit to store the new count in it. 
                var probes = 1;
                var newHashWithoutProbes = oldHash1 & newHashWithoutProbesMask;
                var hs = newHashesAndIndexes[index >> 1];
                var shift = (~(index & 1) & 1) << 5; // for index b???0 -> 32, for index b???1 -> 0, e.g. 0 -> 32, 1 -> 0 
                while (true) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
                {
                    var h = (int)(hs >> shift);
                    if (h == 0)
                    {
                        newHashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | newHashWithoutProbes) << shift);
                        break;
                    }
                    if ((h >> ProbeCountShift) < probes)
                    {
                        newHashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | newHashWithoutProbes) << shift);
                        newHashWithoutProbes = h & HashAndIndexMask;
                        probes = h >> ProbeCountShift;
                    }
                    if (shift == 0)
                    {
                        index = (index + 1) & newIndexMask;
                        hs = newHashesAndIndexes[index >> 1]; // todo: @perf can be optimized
                    }
                    shift = ~shift & 32; // 32 -> 0, 0 -> 32
                    ++probes;
                }
            }
            var oldHash0 = (int)(oldHashes);
            if (oldHash0 != 0)
            {
                var distance = (oldHash0 >> ProbeCountShift) - 1;
                var oldHashIndex = (oldCapacity + (i << 1) + 1 - distance) & oldIndexMask;
                var restoredOldHash = (oldHash0 & ~oldIndexMask) | oldHashIndex;
                var index = restoredOldHash & newIndexMask;

                var probes = 1;
                var newHashWithoutProbes = oldHash0 & newHashWithoutProbesMask;
                var hs = newHashesAndIndexes[index >> 1];
                var shift = (~(index & 1) & 1) << 5; // for index b???0 -> 32, for index b???1 -> 0, e.g. 0 -> 32, 1 -> 0 
                while (true) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
                {
                    var h = (int)(hs >> shift);
                    if (h == 0)
                    {
                        newHashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | newHashWithoutProbes) << shift);
                        break;
                    }
                    if ((h >> ProbeCountShift) < probes)
                    {
                        newHashesAndIndexes[index >> 1] = hs | ((long)((probes << ProbeCountShift) | newHashWithoutProbes) << shift);
                        newHashWithoutProbes = h & HashAndIndexMask;
                        probes = h >> ProbeCountShift;
                    }
                    if (shift == 0)
                    {
                        index = (index + 1) & newIndexMask;
                        hs = newHashesAndIndexes[index >> 1]; // todo: @perf can be optimized
                    }
                    shift = ~shift & 32; // 32 -> 0, 0 -> 32
                    ++probes;
                }
            }
        }
#if DEBUG
        // this will output somthing like this for capacity 32:
        // -_-112--___12-3--44452-223-42311
        // todo: @perf can we move the non`-` hashes in a one loop if possible or non move at all? 
        Debug.Write("Resize: before resize ");
        foreach (var it in oldHashesAndIndexes)
        {
            var it1 = (int)(it >> 32);
            Debug.Write(it1 == 0 ? "_" : (it1 & oldCapacity) != 0 ? "-" : (it1 >> ProbeCountShift).ToString());
            var it0 = (int)(it);
            Debug.Write(it0 == 0 ? "_" : (it0 & oldCapacity) != 0 ? "-" : (it0 >> ProbeCountShift).ToString());
        }

        Debug.WriteLine("");
#endif
        return newHashesAndIndexes;
    }
}
