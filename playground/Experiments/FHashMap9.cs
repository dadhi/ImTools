using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif
namespace ImTools.Experiments;

public static class FHashMap9Diagnostics
{
    /// <summary>Converts the packed hashes and indexes array into the human readable info</summary>
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap9<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var entries = map.Entries;
        var hashesAndIndexes = map.PackedHashesAndIndexes;
        var capacity = map.HashesCapacity;
        var indexMask = capacity - 1;

        var items = new Item<K, V>[capacity];

        for (var i = 0; i < capacity; i++)
        {
            var h = hashesAndIndexes[i];
            if (h == 0)
                continue;

            var probe = (byte)(h >>> FHashMap9<K, V, TEq>.ProbeCountShift);
            var hashIndex = (capacity + i - (probe - 1)) & indexMask;

            var hashMiddle = (h & FHashMap9<K, V, TEq>.HashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var index = h & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & FHashMap9<K, V, TEq>.HashAndIndexMask;
                heq = kh == hash;
                hkv = $"{toB(kh)}:{e.Key}->{e.Value}";
            }
            items[i] = new Item<K, V> { Probe = probe, Hash = toB(hash), HEq = heq, Index = index, HKV = hkv };
        }
        return items;
        static string toB(int x) => Convert.ToString(x, 2).PadLeft(32, '0');
    }

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq>(this FHashMap9<K, V, TEq> map, Action<bool> assertEq)
         where TEq : struct, IEqualityComparer<K>
    {
        var exp = map.Explain();
        foreach (var it in exp)
            if (!it.IsEmpty)
                assertEq(it.HEq);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq>(this FHashMap9<K, V, TEq> map, Action<K> assertKey)
        where TEq : struct, IEqualityComparer<K>
    {
        // Verify the indexes do no contains duplicate keys
        var uniq = new Dictionary<K, int>(map.Count);
        var capacity = map.HashesCapacity;
        var indexMask = capacity - 1;
        var entries = map.Entries;
        for (var i = 0; i < capacity; i++)
        {
            var h = map.PackedHashesAndIndexes[i];
            if (h == 0)
                continue;
            var key = entries[h & indexMask].Key;
            if (!uniq.TryGetValue(key, out var count))
                uniq.Add(key, 1);
            else
                assertKey(key);
        }
    }

    /// <summary>Verifies that the map contains all passed keys. May be called from the tests.</summary>
    public static void VerifyContainAllKeys<K, V, TEq>(this FHashMap9<K, V, TEq> map, IEnumerable<K> expectedKeys, Action<bool, K> assertContainKey)
         where TEq : struct, IEqualityComparer<K>
    {
        foreach (var key in expectedKeys)
            assertContainKey(map.TryGetValue(key, out _), key);
    }

    public struct Item<K, V>
    {
        public byte Probe;
        public bool HEq;
        public string Hash;
        public string HKV;
        public int Index;
        public bool IsEmpty => Probe == 0;
        public string Output => $"{Probe}|{(HEq ? "" : "" + Hash)}{(HEq ? "==" : "!=")}{HKV}";
        public override string ToString() => IsEmpty ? "empty" : Output;
    }
}

#if DEBUG
public class FHashMap9DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap9<K, V, TEq> _map;
    public FHashMap9DebugProxy(FHashMap9<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap9Diagnostics.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap9DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
public struct FHashMap9<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    // todo: @improve make the Entry a type parameter to map and define TEq in terms of the Entry, 
    // todo: @improve it will allow to introduce the Set later without the Value in the Entry, end the Entry may be the Key itself
    [DebuggerDisplay("{Key}->{Value}")]
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    public const int DefaultEntriesCapacity = 2;
    public const byte MinFreeCapacityShift = 3; // e.g. for the DefaultCapacity=16 >> 3 => 2, so 2 free slots is 12.5% of the capacity  

    public const byte MaxProbeBits = 5; // 5 bits max, e.g. 31 (11111)
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1; // e.g. 31 (11111) for the 5 bits
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int ProbesMask = MaxProbeCount << ProbeCountShift;
    public const int HashAndIndexMask = ~ProbesMask;

    // The _packedHashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16). 
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @feature For the removed hash we won't use the tumbstone but will actually remove the hash.
    private int[] _packedHashesAndIndexes;
    private Entry[] _entries;
    // pre-calculated and saved on the DoubleSize for the performance
    private int _indexMask;
    private int _entryCount;

    public int Count => _entryCount;

    internal int[] PackedHashesAndIndexes => _packedHashesAndIndexes;
    internal int HashesCapacity => _indexMask + 1;
    internal Entry[] Entries => _entries;

    public FHashMap9() : this(DefaultEntriesCapacity) { }

    public FHashMap9(uint entriesCapacity)
    {
        if (entriesCapacity < 2)
            entriesCapacity = 2;
#if NET7_0_OR_GREATER
        else if (!BitOperations.IsPow2(entriesCapacity))
            entriesCapacity = BitOperations.RoundUpToPowerOf2(entriesCapacity);
#endif
        // double the size of the hashes, because they are cheap, 
        // this will also provide the flexibility of independence of the sizes of hashes and entries
        var doubleCapacity = (int)(entriesCapacity << 1);
        _packedHashesAndIndexes = new int[doubleCapacity];
        _indexMask = doubleCapacity - 1;
        _entries = new Entry[entriesCapacity];
        _entryCount = 0;

        // todo: @perf benchmark the un-initialized array, me personally did not see any benifits for the small maps?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity);
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private ref Entry TryGetEntryRef(int index)
    {
#if NET7_0_OR_GREATER
        return ref Unsafe.Add(ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries), index);
#else
        return ref _entries[index];
#endif
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);
#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        var indexMask = _indexMask;

        var probesAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var index = hash & indexMask;
        var probes = 1;
        while (true)
        {
#if NET7_0_OR_GREATER
            var h = Unsafe.Add(ref hashesAndIndexes, index);
#else
            var h = hashesAndIndexes[index];
#endif
            if ((h & probesAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref TryGetEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            if ((h >>> ProbeCountShift) < probes)
                break;
            ++probes;
            index = (index + 1) & indexMask;
        }
        value = default;
        return false;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

#if DEBUG
    public int MaxProbes = 1;
    public int FirstProbeAdditions = 0;
#endif

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private int AppendEntry(in K key, in V value)
    {
        var newEntryIndex = _entryCount;
        if (newEntryIndex >= (uint)_entries.Length)
            Array.Resize(ref _entries, _entries.Length << 1);
#if NET7_0_OR_GREATER
        ref var entriesData = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_entries);
        ref var e = ref Unsafe.Add(ref entriesData, newEntryIndex);
#else
        ref var e = ref _entries[newEntryIndex];
#endif
        e.Key = key;
        e.Value = value;
        _entryCount = newEntryIndex + 1;
        return newEntryIndex;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        var indexMask = _indexMask;
        if (indexMask - _entryCount <= (indexMask >>> MinFreeCapacityShift)) // if the free capacity is less free slots 1/16 (6.25%)
        {
#if NET7_0_OR_GREATER
            _packedHashesAndIndexes = Resize(ref hashesAndIndexes, indexMask);
            hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
            _packedHashesAndIndexes = hashesAndIndexes = Resize(_packedHashesAndIndexes, indexMask);
#endif
            _indexMask = indexMask = (indexMask << 1) | 1;
        }

        var probesAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var hashIndex = hash & indexMask;
        var probes = 1;
        while (true)
        {
            Debug.Assert(probes <= MaxProbeCount, $"[AddOrUpdate] DEBUG ASSERT FAILED: probes {probes} <= MaxProbeCount {MaxProbeCount}");
#if NET7_0_OR_GREATER
            ref var h = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
            ref var h = ref hashesAndIndexes[hashIndex];
#endif
            // this check is also implicitly break if `h == 0` to proceed inserting new entry 
            if (h == 0)
            {
                var newEntryIndex = AppendEntry(in key, in value);
                h = (probes << ProbeCountShift) | hashMiddle | newEntryIndex;
                return;
            }
            // Robin Hood loop - the old hash to be re-inserted with the increased probe count
            if ((h >>> ProbeCountShift) < probes)
            {
                var newEntryIndex = AppendEntry(in key, in value);
                var hWithoutProbes = h & HashAndIndexMask;
                var hProbes = h >>> ProbeCountShift;
                h = (probes << ProbeCountShift) | hashMiddle | newEntryIndex;
                probes = hProbes;
                while (true)
                {
                    ++probes;
                    hashIndex = (hashIndex + 1) & indexMask;
#if NET7_0_OR_GREATER
                    h = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
                    h = ref hashesAndIndexes[hashIndex];
#endif
                    hProbes = h >>> ProbeCountShift;
                    if (hProbes < probes) // skip hashes with the bigger probe count until we find the same or less probes
                    {
                        var nextHWithoutProbes = h & HashAndIndexMask;
                        h = (probes << ProbeCountShift) | hWithoutProbes;
                        if (hProbes == 0)
                            return;
                        hWithoutProbes = nextHWithoutProbes;
                        probes = hProbes;
                    }
                }
            }
            if ((h & probesAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {

                ref var e = ref TryGetEntryRef(h & indexMask);
#if DEBUG
                Debug.WriteLine($"[AddOrUpdate] PROBES AND HASH MATCH: probes {probes}, compare new key `{key}` with matched key:`{e.Key}`");
#endif
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
        }
    }

#if NET7_0_OR_GREATER
    internal static int[] Resize(ref int oldHash, int oldIndexMask)
#else
    internal static int[] Resize(int[] oldHashesAndIndexes, int oldIndexMask)
#endif
    {
        // double the hashes capacity
        var oldCapacity = oldIndexMask + 1;
        var newCapacity = oldCapacity << 1;
        var newIndexMask = (oldCapacity << 1) - 1;
        var hashAndIndexMaskWithNextIndexBitErased = HashAndIndexMask & ~oldCapacity;
#if DEBUG
        Debug.WriteLine($"RESIZE _packedHashesAndIndexes, double the capacity: {oldCapacity} -> {newCapacity}");
#endif
        // todo: @perf is there a way to avoid the copying of the hashes and indexes, at least some of them?
        var newHashesAndIndexes = new int[newCapacity];

// #if NET7_0_OR_GREATER
//         ref var newHashRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);
//         ref var hOld = ref oldHash;
//         for (var i = 0; i < oldCapacity; ++i, hOld = ref Unsafe.Add(ref hOld, 1))
//         {
//             if ((hOld >>> ProbeCountShift) == 1)
//             {
//                 var bit = oldHash & oldCapacity;
//                 ref var hNew = ref Unsafe.Add(ref newHashRef, i + bit);
//                 hNew = hOld & ~oldCapacity;
//                 hOld = 0;
//             }
//         }
// #endif

#if NET7_0_OR_GREATER
        ref var newHashRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);
        for (var i = 0; i < oldCapacity; ++i, oldHash = ref Unsafe.Add(ref oldHash, 1))
        {
#else
        for (var i = 0; (uint)i < (uint)oldHashesAndIndexes.Length; ++i)
        {
            ref var oldHash = ref oldHashesAndIndexes[i];
#endif
            if (oldHash == 0)
                continue;

            // todo: @perf ways to simplify?
            // get the new hash index for the new capacity by restoring the (possibly wrapped) 
            // probes count (and therefore the distance from the ideal hash position) 
            var distance = (oldHash >>> ProbeCountShift) - 1;
            var oldHashIndex = (oldCapacity + i - distance) & oldIndexMask;
            var restoredOldHash = (oldHash & ~oldIndexMask) | oldHashIndex;
            var newHashIndex = restoredOldHash & newIndexMask;

            // erasing the next to capacity bit, given the capacity was 4 and now it is 8 == 4 << 1,
            // we are erasing the 3rd bit to store the new count in it. 
            var oldHashWithNewIndexBits = oldHash & hashAndIndexMaskWithNextIndexBitErased;
            var probes = 1;
            while (true)
            {
#if NET7_0_OR_GREATER
                ref var h = ref Unsafe.Add(ref newHashRef, newHashIndex);
#else
                ref var h = ref newHashesAndIndexes[newHashIndex];
#endif
                if (h == 0)
                {
                    h = (probes << ProbeCountShift) | oldHashWithNewIndexBits;
                    break;
                }
                if ((h >>> ProbeCountShift) < probes)
                {
                    var hAndIndex = h & HashAndIndexMask;
                    var hProbes = h >>> ProbeCountShift;
                    h = (probes << ProbeCountShift) | oldHashWithNewIndexBits;
                    oldHashWithNewIndexBits = hAndIndex;
                    probes = hProbes;
                }
                ++probes;
                newHashIndex = (newHashIndex + 1) & newIndexMask;
            }
        }
        return newHashesAndIndexes;
    }
}
