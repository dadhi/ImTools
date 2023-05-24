using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Numerics;
#endif
namespace ImTools.Experiments;

public static class FHashMap7Extensions
{
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap7<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var entries = map._entries;
        var hashesAndIndexes = map._hashesAndIndexes;
        var capacity = map.HashesCapacity;
        var indexMask = capacity - 1;

        var items = new Item<K, V>[capacity];

        for (var i = 0; i < capacity; i++)
        {
            var h = hashesAndIndexes[i];
            if (h == 0)
                continue;

            var probe = (byte)(h >> FHashMap7<K, V, TEq>.ProbeCountShift);
            var hashIndex = (capacity + i - (probe - 1)) & indexMask;

            var hashMiddle = (h & FHashMap7<K, V, TEq>.HashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var index = h & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & FHashMap7<K, V, TEq>.HashAndIndexMask;
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
public class FHashMap7DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap7<K, V, TEq> _map;
    public FHashMap7DebugProxy(FHashMap7<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap7Extensions.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap7DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
public sealed class FHashMap7<K, V, TEq> where TEq : struct, IEqualityComparer<K>
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

#if NET7_0_OR_GREATER
    // First 4 probes arranged in a vector for batch operations with the hash and for the batch comparison
    internal static readonly Vector128<int> FirstProbesVec = Vector128.Create(1, 2, 3, 4);
    // Next probes increment, we are adding it to each element of the probesVec, e.g. <1 + 4, 2 + 4, 3 + 4, 4 + 4> -> <5, 6, 7, 8>
    internal static readonly Vector128<int> ProbesIncVec = Vector128.Create(4);
#endif

    // The _hashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16). 
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @add For the removed hash we won't use the tumbstone but will actually remove the hash.
    internal int[] _hashesAndIndexes;
    internal Entry[] _entries;
    internal int _indexMask; // pre-calculated and saved on the DoubleSize for the performance
    internal int _entryCount;

    // todo: @wip remove or hide or whatever
    public int[] HashesAndIndexes => _hashesAndIndexes;
    // todo: @wip remove or hide or whatever
    public int HashesCapacity => _indexMask + 1;

    public Entry[] Entries => _entries;
    public int Count => _entryCount;

    public FHashMap7(uint seedCapacity = DefaultSeedCapacity)
    {
        if (seedCapacity < 2)
            seedCapacity = 2;
#if NET7_0_OR_GREATER
        else if (!BitOperations.IsPow2(seedCapacity))
            seedCapacity = BitOperations.RoundUpToPowerOf2(seedCapacity);
#endif
        // double the size of the hashes, because they are cheap, 
        // this will also provide the flexibility of independence of the sizes of hashes and entries
        var doubleCapacity = (int)(seedCapacity << 1);
        _hashesAndIndexes = CreatePaddedHashesAndIndexes(doubleCapacity);
        _indexMask = doubleCapacity - 1;
        _entries = new Entry[seedCapacity];
        _entryCount = 0;

        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

#if BLAH
    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        ref var hashesAndIndexesRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_hashesAndIndexes);

        var indexMask = _indexMask;
        var probeAndHashMask = ~indexMask;
        // var probeAndHashMaskVec = Vector128.Create(~indexMask);

        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        // var hashMiddleVec = Vector128.Create(hash & ~indexMask & HashAndIndexMask);
        var hashIndex = hash & indexMask;

        var probes = 1;
        var probesVec = FirstProbesVec;
        while (true)
        {
            // read the 4 hashesAndIndexes at once into the vector
            ref var hRef = ref Unsafe.Add(ref hashesAndIndexesRef, hashIndex);
            var hVec = Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<int, byte>(ref hRef));
            ref var hVecRef = ref Unsafe.As<Vector128<int>, int>(ref Unsafe.AsRef(in hVec));
            var h0 = hVecRef;
            if ((h0 & probeAndHashMask) == (probes << ProbeCountShift | hashMiddle))
            {
                // ref var entriesRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries);
                // ref var e = ref Unsafe.Add(ref entriesRef, h0 & indexMask);
                ref var e = ref _entries[h0 & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            var h1 = Unsafe.Add(ref hVecRef, 1);
            if ((h1 & probeAndHashMask) == ((probes + 1) << ProbeCountShift | hashMiddle))
            {
                // ref var entriesRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries);
                // ref var e = ref Unsafe.Add(ref entriesRef, h1 & indexMask);
                ref var e = ref _entries[h1 & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            var h2 = Unsafe.Add(ref hVecRef, 2);
            if ((h2 & probeAndHashMask) == ((probes + 2) << ProbeCountShift | hashMiddle))
            {
                // ref var entriesRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries);
                // ref var e = ref Unsafe.Add(ref entriesRef, h2 & indexMask);
                ref var e = ref _entries[h2 & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            var h3 = Unsafe.Add(ref hVecRef, 3);
            if ((h3 & probeAndHashMask) == ((probes + 3) << ProbeCountShift | hashMiddle))
            {
                // ref var entriesRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries);
                // ref var e = ref Unsafe.Add(ref entriesRef, h3 & indexMask);
                ref var e = ref _entries[h3 & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            // // create the hashes + probes vector for the batch comparison of the 4 elements at once
            // // e.g. batching `probes << ProbeCountShift | hashMiddle`
            // var hashProbesAndHashVec = Vector128.BitwiseOr(Vector128.ShiftLeft(probesVec, ProbeCountShift), hashMiddleVec);

            // var hProbesAndHashVec = Vector128.BitwiseAnd(hVec, probeAndHashMaskVec);
            // var hEqHashVec = Vector128.Equals(hProbesAndHashVec, hashProbesAndHashVec);

            // // The check is the first because if looking for the present key, we will avoid the unnecessary exit condition below. 
            // // Regarding the check for missing key, it is fine to pain one comparison, imho - you  will need to select what you care for ;)
            // // if (hEqHashVec != Vector128<int>.Zero)

            // Basically doing this but for the 4 elements in vector `(h >> ProbeCountShift) < probes`
            if (Vector128.LessThanAny(Vector128.ShiftRightLogical(hVec, ProbeCountShift), probesVec))
                break;

            probes += 4;
            probesVec = Vector128.Add(probesVec, ProbesIncVec);
            hashIndex = (hashIndex + 4) & indexMask;
        }
        value = default;
        return false;
    }
#else
    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var indexMask = _indexMask;

        var hashMiddleMask = ~indexMask & HashAndIndexMask;
        var hashMiddle = hash & hashMiddleMask;
        var hashIndex = hash & indexMask;

        var probes = 1;
        int h, hProbes;
        while (true)
        {
            h = hashesAndIndexes[hashIndex];
            hProbes = h >> ProbeCountShift;
            if (hProbes < probes)
                break;

            if (hProbes == probes && (h & hashMiddleMask) == hashMiddle)
            {
                var entryIndex = h & indexMask;
                ref var e = ref _entries[entryIndex];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
                        
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
        }
        value = default;
        return false;
    }
#endif

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

#if DEBUG
    public int MaxProbes = 1;
    public int FirstProbeAdditions = 0;
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
        var hashIndex = hash & indexMask;
        var probes = 1;
        int h, hProbes;
        while (true)
        {
            Debug.Assert(probes <= MaxProbeCount, $"[AddOrUpdate] DEBUG ASSERT FAILED: probes {probes} <= MaxProbeCount {MaxProbeCount}");

            h = hashesAndIndexes[hashIndex];
            hProbes = h >> ProbeCountShift; 

            // this check is also implicitly break if `h == 0` to proceed inserting new entry 
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
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
        }

        // Robin Hood goes here to steal from the rich (with the less probe count) and give to the poor (with more probes).
        var newEntryIndex = _entryCount;
        hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashMiddle | newEntryIndex;

        if (newEntryIndex >= _entries.Length)
            Array.Resize(ref _entries, _entries.Length * 2);
        ref var e = ref _entries[newEntryIndex];
        e.Key = key;
        e.Value = value;
        _entryCount = newEntryIndex + 1;
        if (h == 0)
            return;

        // Robin Hood loop - the old hash to be re-inserted with the increased probe count
        probes = hProbes;
        var hashWithoutProbes = h & HashAndIndexMask;
        while (true)
        {
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
            h = hashesAndIndexes[hashIndex];
            if (h == 0)
            {
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashWithoutProbes;
                return;
            }
            if ((h >> ProbeCountShift) < probes) // skip hashes with the bigger probe count until we find the same or less probes
            {
                hashesAndIndexes[hashIndex] = (probes << ProbeCountShift) | hashWithoutProbes;
                hashWithoutProbes = h & HashAndIndexMask;
                probes = (h >> ProbeCountShift);
            }
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    internal static int[] CreatePaddedHashesAndIndexes(int capacity)
    {
        // `+ 3` because we need to align the `a` to 4 elements boundary for the Vector128 read starting from the last index in array
        var a = new int[capacity + 3];
        a[capacity + 2] = ProbesMask;
        a[capacity + 1] = ProbesMask;
        a[capacity] = ProbesMask;
        return a;
    }

    internal static int[] Resize(int[] oldHashesAndIndexes, int oldIndexMask)
    {
        var oldCapacity = oldIndexMask + 1;
        var newCapacity = oldCapacity << 1;
    #if DEBUG
        Debug.WriteLine($"RESIZE _hashesAndIndexes, double the capacity: {oldCapacity} -> {newCapacity}");
    #endif

        var newHashesAndIndexes = CreatePaddedHashesAndIndexes(newCapacity);

        var newIndexMask = newCapacity - 1;
        var newHashMiddleMask = ~newIndexMask & HashAndIndexMask;

        // todo: @perf find the way to avoid copying the hashes with 0 next bit and with ideal+ probe count
        for (var i = 0; (uint)i < (uint)oldCapacity; ++i)
        {
            var oldHash = oldHashesAndIndexes[i];
            if (oldHash == 0)
                continue;

            // get the new hash index for the new capacity by restoring the (possibly wrapped) 
            // probes count (and therefore the distance from the ideal hash position) 
            var distance = (oldHash >> ProbeCountShift) - 1;
            var oldHashIndex = (oldCapacity + i - distance) & oldIndexMask;
            var restoredOldHash = (oldHash & ~oldIndexMask) | oldHashIndex;
            var newHashIndex = restoredOldHash & newIndexMask;

            // erasing the next to capacity bit, given the capacity was 4 and now it is 4 << 1 = 8, 
            // we are erasing the 3rd bit to store the new count in it. 
            var newHashAndEntryIndex = oldHash & HashAndIndexMask & ~oldCapacity;

            var h = 0;
            // todo: @simplify factor out into the method X
            for (var probes = 1; ; ++probes, newHashIndex = (newHashIndex + 1) & newIndexMask) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
            {
                h = newHashesAndIndexes[newHashIndex];
                if (h == 0)
                {
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndEntryIndex;
                    break;
                }
                if ((h >> ProbeCountShift) < probes)
                {
                    newHashesAndIndexes[newHashIndex] = (probes << ProbeCountShift) | newHashAndEntryIndex;
                    newHashAndEntryIndex = h & HashAndIndexMask;
                    probes = h >> ProbeCountShift;
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
