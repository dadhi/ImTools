using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Numerics;
using System.Runtime.InteropServices;
#endif
namespace ImTools.Experiments;

public static class FHashMap91Diagnostics
{
    /// <summary>Converts the packed hashes and indexes array into the human readable info</summary>
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap91<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var probeCountShift = FHashMap91<K, V, TEq>.ProbeCountShift;
        var hashAndIndexMask = FHashMap91<K, V, TEq>.HashAndIndexMask;

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

            var probe = (byte)(h >>> probeCountShift);
            var hashIndex = (capacity + i - (probe - 1)) & indexMask;

            var hashMiddle = (h & hashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var index = h & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                var e = entries[index];
                var kh = e.Key.GetHashCode() & hashAndIndexMask;
                heq = kh == hash;
                hkv = $"{toB(kh)}:{e.Key}->{e.Value}";
            }
            items[i] = new Item<K, V> { Probe = probe, Hash = toB(hash), HEq = heq, Index = index, HKV = hkv };
        }
        return items;
        static string toB(int x) => Convert.ToString(x, 2).PadLeft(32, '0');
    }

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq>(this FHashMap91<K, V, TEq> map, Action<bool> assertEq)
         where TEq : struct, IEqualityComparer<K>
    {
        var exp = map.Explain();
        foreach (var it in exp)
            if (!it.IsEmpty)
                assertEq(it.HEq);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq>(this FHashMap91<K, V, TEq> map, Action<K> assertKey)
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
    public static void VerifyContainAllKeys<K, V, TEq>(this FHashMap91<K, V, TEq> map, IEnumerable<K> expectedKeys, Action<bool, K> assertContainKey)
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
public class FHashMap91DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap91<K, V, TEq> _map;
    public FHashMap91DebugProxy(FHashMap91<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap91Diagnostics.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap91DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
public struct FHashMap91<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    // todo: @improve make the Entry a type parameter to map and define TEq in terms of the Entry, 
    // todo: @improve it will allow to introduce the Set later without the Value in the Entry, end the Entry may be the Key itself
    [DebuggerDisplay("{Key}->{Value}")]
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    // todo: @unused, tested but the benchmarks degraded significally for some reason. Will keep it here until the moral improves, or rather it is suggested to apply it on the side of the user if necessary.
    public const uint GoldenRatio32 = 2654435769; // 2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803
    public const int MinEntriesCapacity = 2;
    public const byte MinFreeCapacityShift = 3; // e.g. for the capacity 16: 16 >> 3 => 2, so 2 free slots is 12.5% of the capacity
    public const int MinCapacity = 8;
    public const byte MaxProbeBits = 5; // 5 bits max, e.g. 31 (11111)
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1; // e.g. 31 (11111) for the 5 bits
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int ProbesMask = MaxProbeCount << ProbeCountShift;
    public const int HashAndIndexMask = ~ProbesMask;

    internal const int EntriesMaxIndexBitCount = 8; // todo: @wip mAsyncTaskMethodBuilder configurable?
    internal const int EntriesMaxIndexMask = (1 << EntriesMaxIndexBitCount) - 1; // e.g. 256 - 1 = 255

    // The _packedHashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16).
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @feature remove - for the removed hash we won't use the tumbstone but will actually remove the hash.
    private int[] _packedHashesAndIndexes;
    private Entry[] _entries; // for the performance it always contains the current entries (maybe a nested array in the batch) where we are adding the new entry
    private Entry[][] _entriesBatch;
    // pre-calculated and saved on the DoubleSize for the performance
    private int _indexMask;
    private int _entryCount;

    public int Count => _entryCount;

    internal int[] PackedHashesAndIndexes => _packedHashesAndIndexes;
    internal int HashesCapacity => _indexMask + 1;
    internal Entry[] Entries => _entries;

    internal static int[] _singleCellHashesAndIndexes = new int[1];

    public FHashMap91()
    {
        // using single cell array for hashes instead of empty one to allow the Lookup to work without the additional check for the emptiness
        _packedHashesAndIndexes = _singleCellHashesAndIndexes;
        _entries = Array.Empty<Entry>();
        _indexMask = 0;
        _entryCount = 0;
    }

    public FHashMap91(uint entriesCapacity)
    {
        if (entriesCapacity < 2)
            entriesCapacity = 2;
#if NET7_0_OR_GREATER
        else if (!BitOperations.IsPow2(entriesCapacity))
            entriesCapacity = BitOperations.RoundUpToPowerOf2(entriesCapacity);
#endif
        // double the size of the hashes, because they are cheap, 
        // this will also provide the flexibility of independence of the sizes of hashes and entries
        var hashesCapacity = (int)(entriesCapacity << 1);
        _packedHashesAndIndexes = new int[hashesCapacity];
        _entries = new Entry[entriesCapacity];
        _indexMask = hashesCapacity - 1;
        _entryCount = 0;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private ref Entry GetEntryRef(int index)
    {
#if NET7_0_OR_GREATER
        if (_entriesBatch == null)
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
        ref var entries = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entriesBatch), index >>> EntriesMaxIndexBitCount);
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), index & EntriesMaxIndexMask);
#else
        if (_entriesBatch == null)
            return ref _entries[index];
        return _entriesBatch[index >>> EntriesMaxIndexBitCount][index & EntriesMaxIndexMask];
#endif
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private void AppendEntry(in K key, in V value)
    {
        var newEntryIndex = _entryCount & EntriesMaxIndexMask;

        // if the new entry index is on the edge of the entries then we always need to resize or allocate more for the batch
        if ((newEntryIndex == 0) | (newEntryIndex == _entries.Length))
            AllocateEntries();

#if NET7_0_OR_GREATER
        ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), newEntryIndex);
#else
        ref var e = ref _entries[newEntryIndex];
#endif
        e.Key = key;
        e.Value = value;
        ++_entryCount;
    }

    private void AllocateEntries()
    {
        if (_entryCount <= EntriesMaxIndexMask) // for the small indexes which fit in the single entries
        {
#if DEBUG
            Debug.WriteLine($"[AllocateEntries] {_entryCount} -> {_entryCount << 1}");
#endif
            if (_entryCount != 0)
                Array.Resize(ref _entries, _entryCount << 1);
            else
                _entries = new Entry[MinEntriesCapacity];
        }
        else
        {
            if (_entriesBatch != null)
            {
                if ((_entryCount >>> EntriesMaxIndexBitCount) == _entriesBatch.Length) // check that index is outside of the batch
                    Array.Resize(ref _entriesBatch, _entriesBatch.Length << 1); // double the batch in order to speedup the index calculation by shift avoiding the div cost.
                _entriesBatch[_entryCount >>> EntriesMaxIndexBitCount] = _entries = new Entry[EntriesMaxIndexMask + 1]; // todo: @optimize with non-initialized entries
            }
            else
                _entriesBatch = new Entry[][] { _entries, _entries = new Entry[EntriesMaxIndexMask + 1] };
        }
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = _indexMask;
        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
        var h = Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
        var h = hashesAndIndexes[hashIndex];
#endif

        var probes = 1;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashPartMask) == (hash & hashPartMask)))
            {
                ref var e = ref GetEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

#if NET7_0_OR_GREATER
            h = Unsafe.Add(ref hashesAndIndexes, ++hashIndex & indexMask);
#else
            h = hashesAndIndexes[++hashIndex & indexMask];
#endif
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

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = _indexMask;
        if (indexMask - _entryCount <= (indexMask >>> MinFreeCapacityShift)) // if the free space is less than 1/8 of capacity (12.5%)
        {
            _packedHashesAndIndexes = indexMask != 0 ? ResizeToDoubleCapacity(_packedHashesAndIndexes, indexMask) : new int[MinCapacity];
            _indexMask = indexMask = _packedHashesAndIndexes.Length - 1;
        }

        var hashIndex = hash & indexMask;
        var hashPart = hash & ~indexMask & HashAndIndexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
        ref var h = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
        ref var h = ref hashesAndIndexes[hashIndex];
#endif

        var probes = 1;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if ((h & ~indexMask) == ((probes << ProbeCountShift) | hashPart))
            {
                ref var e = ref GetEntryRef(h & indexMask);
#if DEBUG
                Debug.WriteLine($"[AddOrUpdate] Probes and Hash parts are matching: probes {probes}, new key:`{key}` with matched key:`{e.Key}`");
#endif
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
#if NET7_0_OR_GREATER
            h = ref Unsafe.Add(ref hashesAndIndexes, ++hashIndex & indexMask);
#else
            h = ref hashesAndIndexes[++hashIndex & indexMask];
#endif
            ++probes;
        }

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hHooded = h;
        h = (probes << ProbeCountShift) | hashPart | _entryCount;
#if DEBUG
        if (probes > MaxProbes)
            Debug.WriteLine($"[AddOrUpdate] MaxProbes {MaxProbes = probes}");
#endif
        AppendEntry(in key, in value);

        // 4. If old hash is empty then we stop
        // 5. Robin Hood goes here - to steal the slot with the smaller probes
        probes = hHooded >>> ProbeCountShift;
        while (hHooded != 0)
        {
#if NET7_0_OR_GREATER
            h = ref Unsafe.Add(ref hashesAndIndexes, ++hashIndex & indexMask);
#else
            h = ref hashesAndIndexes[++hashIndex & indexMask];
#endif
            if ((h >>> ProbeCountShift) < (++probes))
            {
                var hHoodedNext = h;
                h = (probes << ProbeCountShift) | (hHooded & HashAndIndexMask);
#if DEBUG
                if (probes > MaxProbes)
                    Debug.WriteLine($"[AddOrUpdate] MaxProbes {MaxProbes = probes}");
#endif
                hHooded = hHoodedNext;
                probes = hHoodedNext >>> ProbeCountShift;
            }
        }
    }

    internal static int[] ResizeToDoubleCapacity(int[] oldHashesAndIndexes, int oldIndexMask)
    {
        var oldCapacity = oldIndexMask + 1;
        var newIndexMask = (oldIndexMask << 1) | 1;
#if DEBUG
        Debug.WriteLine($"[ResizeToDoubleCapacity] {oldCapacity} -> {oldCapacity << 1}");
#endif

        // todo: @perf is there a way to avoid the copying of the hashes and indexes, at least some of them?
        var newHashesAndIndexes = new int[oldCapacity << 1]; // double the hashes capacity

#if NET7_0_OR_GREATER
        ref var oldHash = ref MemoryMarshal.GetArrayDataReference(oldHashesAndIndexes);
        ref var newHash = ref MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);

        var i = 0;
        while (true)
        {
#else
        for (var i = 0; (uint)i < (uint)oldHashesAndIndexes.Length; ++i)
        {
            ref var oldHash = ref oldHashesAndIndexes[i];
#endif
            if (oldHash != 0)
            {
                // get the new hash index for the new capacity by restoring the (possibly wrapped) old one from the probes - 1, 
                // to account for the wrapping we use `(oldCapacity + i...) & oldIndexMask` 
                var distance = (oldHash >>> ProbeCountShift) - 1;
                var oldHashNewIndex = (oldHash & oldCapacity) | ((oldCapacity + i - distance) & oldIndexMask);

                // erasing the next to capacity bit, given the capacity was 4 and now it is 8 == 4 << 1,
                // we are erasing the 3rd bit to store the new entry index in it.
                var oldHashWithNextIndexBitErased = oldHash & ~oldCapacity & HashAndIndexMask;
                var probes = 1;
                while (true)
                {
#if NET7_0_OR_GREATER
                    ref var h = ref Unsafe.Add(ref newHash, oldHashNewIndex & newIndexMask);
#else
                    ref var h = ref newHashesAndIndexes[oldHashNewIndex & newIndexMask];
#endif
                    if ((h >>> ProbeCountShift) < probes)
                    {
                        var hHooded = h;
                        h = (probes << ProbeCountShift) | oldHashWithNextIndexBitErased;
                        if (hHooded == 0)
                            break;
                        oldHashWithNextIndexBitErased = hHooded & HashAndIndexMask;
                        probes = hHooded >>> ProbeCountShift;
                    }
                    ++probes;
                    ++oldHashNewIndex;
                }
            }
#if NET7_0_OR_GREATER
            if (i >= oldIndexMask)
                break;
            ++i;
            oldHash = ref Unsafe.Add(ref oldHash, 1);
#endif
        }
        return newHashesAndIndexes;
    }
}
