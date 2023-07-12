using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
namespace ImTools.Experiments;

public static class FHashMap91
{
    internal const uint GoldenRatio32 = 2654435769; // 2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803

    internal static readonly int[] SingleCellHashesAndIndexes = new int[1];

    public struct Item<K, V>
    {
        public int Probe;
        public bool HEq;
        public string Hash;
        public string HKV;
        public int Index;
        public bool IsEmpty => Probe == 0;
        public string Output => $"{Probe}|{Hash}|{Index}";
        public override string ToString() => IsEmpty ? "empty" : Output;
    }

    /// <summary>Converts the packed hashes and entries into the human readable info.
    /// This also used for the debugging view of the <paramref name="map"/> and by the Verify... methods in tests.</summary>
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap91<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var probeCountShift = FHashMap91<K, V, TEq>.ProbeCountShift;
        var hashAndIndexMask = FHashMap91<K, V, TEq>.HashAndIndexMask;

        var hashes = map.PackedHashesAndIndexes;
        var capacityBits = map.CapacityBits;
        var capacity = 1 << capacityBits;
        var indexMask = capacity - 1;

        var items = new Item<K, V>[hashes.Length];
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            if (h == 0)
                continue;

            var probe = h >>> probeCountShift;
            var hashIndex = i - probe + 1;

            var hashMiddle = (h & hashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var entryIndex = h & indexMask;

            string hkv = null;
            var heq = false;
            if (probe != 0)
            {
                ref var e = ref map.GetEntryRef(entryIndex);
                var kh = default(TEq).GetHashCode(e.Key) & hashAndIndexMask;
                heq = kh == hash;
                // hkv = $"{toB(kh)}:{e.Key}->{e.Value}"; // todo: @wip
            }
            items[i] = new Item<K, V> { Probe = probe, Hash = toB(hash), HEq = heq, Index = entryIndex, HKV = hkv };
        }
        return items;

        // binary reprsentation of the `int`
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
        var hashes = map.PackedHashesAndIndexes;
        var capacityBits = map.CapacityBits;
        var capacity = 1 << capacityBits;
        var indexMask = capacity - 1;
        var entries = map.Entries;
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
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
}

/// <summary>Default comparer using the `object.GetHashCode` and `object.Equals` oveloads</summary>
public struct DefaultEq<K> : IEqualityComparer<K>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(K x, K y) => x.Equals(y);

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => obj.GetHashCode();
}

/// <summary>Uses the `object.GetHashCode` and `object.ReferenceEquals`</summary>
public struct RefEq<K> : IEqualityComparer<K> where K : class
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(K x, K y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => obj.GetHashCode();
}

/// <summary>Uses Fibonacci hashing by multiplying the original hash on the factor derived from the GoldenRatio</summary>
public struct GoldenRefEq<K> : IEqualityComparer<K> where K : class
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(K x, K y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(K obj) => (int)(obj.GetHashCode() * FHashMap91.GoldenRatio32);
}

/// <summary>Uses the integer itself as hash code and `==` for equality</summary>
public struct IntEq : IEqualityComparer<int>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(int x, int y) => x == y;

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(int obj) => obj;
}

/// <summary>Uses Fibonacci hashing by multiplying the integer on the factor derived from the GoldenRatio</summary>
public struct GoldenIntEq : IEqualityComparer<int>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(int x, int y) => x == y;

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(int obj) => (int)(obj * FHashMap91.GoldenRatio32);
}

/// <summary>Fast-comparing the types via `==` and gets the hash faster via `RuntimeHelpers.GetHashCode`</summary>
public struct TypeEq : IEqualityComparer<Type>
{
    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public bool Equals(Type x, Type y) => x == y;

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public int GetHashCode(Type obj) => RuntimeHelpers.GetHashCode(obj);
}

#if DEBUG
public class FHashMap91DebugProxy<K, V, TEq> where TEq : struct, IEqualityComparer<K>
{
    private readonly FHashMap91<K, V, TEq> _map;
    public FHashMap91DebugProxy(FHashMap91<K, V, TEq> map) => _map = map;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public FHashMap91.Item<K, V>[] Items => _map.Explain();
}

[DebuggerTypeProxy(typeof(FHashMap91DebugProxy<,,>))] // todo: @wip add separately for the packed hashes
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
    public const byte MinCapacityBits = 3; // 1 << 3 == 8
    public const byte MaxProbeBits = 5; // 5 bits max, e.g. 31 (11111)
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1; // e.g. 31 (11111) for the 5 bits
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int ProbesMask = MaxProbeCount << ProbeCountShift;
    public const int HashAndIndexMask = ~ProbesMask;

    // public const byte DefaultEntriesMaxIndexBitsBeforeSplit = 8;
    private bool _hashesOverflowBufferIsFull;
    // private int _entriesMaxIndexBitsBeforeSplit;
    // private int _entriesMaxIndexMask;
    private byte _capacityBits;

    // The _packedHashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16).
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @feature remove - for the removed hash we won't use the tumbstone but will actually remove the hash.
    private int[] _packedHashesAndIndexes;
    private Entry[] _entries; // for the performance it always contains the current entries (maybe a nested array in the batch) where we are adding the new entry
    // private Entry[][] _entriesBatch;
    private int _entryCount;

    public int Count => _entryCount;
    public int CapacityBits => _capacityBits;

    internal int[] PackedHashesAndIndexes => _packedHashesAndIndexes;
    internal Entry[] Entries => _entries;

    public FHashMap91()
    {
        _capacityBits = 0;
        // _entriesMaxIndexBitsBeforeSplit = DefaultEntriesMaxIndexBitsBeforeSplit;
        // _entriesMaxIndexMask = (1 << DefaultEntriesMaxIndexBitsBeforeSplit) - 1; // e.g. 256 - 1 = 255

        // using single cell array for hashes instead of empty one to allow the Lookup to work without the additional check for the emptiness
        _packedHashesAndIndexes = FHashMap91.SingleCellHashesAndIndexes;
        _entries = Array.Empty<Entry>();
        _entryCount = 0;
    }

    /// <summary>Capacity calculates as `1 << capacityBitShift`</summary>
    public FHashMap91(byte capacityBits
        //, byte entriesMaxIndexBitsBeforeSplit = DefaultEntriesMaxIndexBitsBeforeSplit
        )
    {
        _capacityBits = capacityBits;
        // _entriesMaxIndexBitsBeforeSplit = entriesMaxIndexBitsBeforeSplit;
        // _entriesMaxIndexMask = (1 << entriesMaxIndexBitsBeforeSplit) - 1; // e.g. 256 - 1 = 255

        // the overflow tail to the hashes is the size of log2N where N==capacityBits, 
        // it is probably fine to have the check for the overlow of capacity because it will be mispredicted only once at the end of loop (it even rarely for the lookup)
        _packedHashesAndIndexes = new int[(1 << capacityBits) + capacityBits];
        _entries = new Entry[1 << capacityBits];
        _entryCount = 0;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    internal ref Entry GetEntryRef(int index)
    {
#if NET7_0_OR_GREATER
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
        // if (_entriesBatch == null) // todo: @perf can we optimize by always using the batch?
        //     return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
        // ref var entries = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entriesBatch), index >>> _entriesMaxIndexBitsBeforeSplit);
        // return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), index & _entriesMaxIndexMask);
#else
        return ref _entries[index];
        // if (_entriesBatch == null)
        //     return ref _entries[index];
        // return ref _entriesBatch[index >>> _entriesMaxIndexBitsBeforeSplit][index & _entriesMaxIndexMask];
#endif
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private void AppendEntry(in K key, in V value)
    {
        var newEntryIndex = _entryCount;
        var entriesCapacity = _entries.Length;
        if (newEntryIndex >= _entries.Length)
            AllocateEntries(entriesCapacity);

        // if the new entry index is on the edge of the entries then we always need to resize or allocate more for the batch
        // var newEntryIndex = _entryCount & _entriesMaxIndexMask;
        // var entriesCapacity = _entries.Length;
        // if ((newEntryIndex == 0) | (newEntryIndex == entriesCapacity))
        //     AllocateEntries(entriesCapacity);

#if NET7_0_OR_GREATER
        ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), newEntryIndex);
#else
        ref var e = ref _entries[newEntryIndex];
#endif
        e.Key = key;
        e.Value = value;
        ++_entryCount;
    }

    private void AllocateEntries(int entriesCapacity)
    {
#if DEBUG
        Debug.WriteLine($"[AllocateEntries] Resize entries: {entriesCapacity} -> {entriesCapacity << 1}");
#endif
        if (entriesCapacity != 0)
            Array.Resize(ref _entries, entriesCapacity << 1);
        else
            _entries = new Entry[MinEntriesCapacity];
    }

    //     private void AllocateEntries(int entriesCapacity)
    //     {
    //         if (_entryCount <= _entriesMaxIndexMask) // for the small indexes which fit in the single entries
    //         {
    // #if DEBUG
    //             Debug.WriteLine($"[AllocateEntries] {_entryCount} -> {_entryCount << 1}");
    // #endif
    //             if (entriesCapacity != 0)
    //                 Array.Resize(ref _entries, entriesCapacity << 1);
    //             else
    //                 _entries = new Entry[MinEntriesCapacity]; // todo: @wip, @bug of reallocating if the _entryCount == 0
    //         }
    //         else
    //         {
    //             if (_entriesBatch != null)
    //             {
    //                 if ((_entryCount >>> _entriesMaxIndexBitsBeforeSplit) == _entriesBatch.Length) // check that index is outside of the batch
    //                     Array.Resize(ref _entriesBatch, _entriesBatch.Length << 1); // double the batch in order to speedup the index calculation by shift avoiding the div cost.
    //                 // note: We're not using GC.UninitializedArray here, because it makes sense only for the entries bigger than 2kb, but we usually split into the lesser entries in the batch.
    //                 _entriesBatch[_entryCount >>> _entriesMaxIndexBitsBeforeSplit] = _entries = new Entry[_entriesMaxIndexMask + 1];
    //             }
    //             else
    //                 _entriesBatch = new Entry[][] { _entries, _entries = new Entry[_entriesMaxIndexMask + 1] };
    //         }
    //     }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBits) - 1;
        var lastIndex = (1 << _capacityBits) + (_capacityBits - 1);
        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
        var h = Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
        var h = hashesAndIndexes[hashIndex];
#endif

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
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

            if (hashIndex == lastIndex)
                break;

#if NET7_0_OR_GREATER
            h = Unsafe.Add(ref hashesAndIndexes, ++hashIndex);
#else
            h = hashesAndIndexes[++hashIndex];
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

        // if the overflow space is filled-in or
        // if the free space is less than 1/8 of capacity (12.5%) then Resize
        var indexMask = (1 << _capacityBits) - 1;
        if ((indexMask - _entryCount <= (indexMask >>> MinFreeCapacityShift)) | _hashesOverflowBufferIsFull)
        {
            ResizeHashes(indexMask);
            indexMask = (1 << _capacityBits) - 1;
            _hashesOverflowBufferIsFull = false;
        }

        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
        ref var h = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
        ref var h = ref hashesAndIndexes[hashIndex];
#endif

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashPartMask) == (hash & hashPartMask)))
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
            h = ref Unsafe.Add(ref hashesAndIndexes, ++hashIndex);
#else
            h = ref hashesAndIndexes[++hashIndex];
#endif
            ++probes;
        }

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hHooded = h;
        h = (probes << ProbeCountShift) | (hash & hashPartMask) | _entryCount;
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
            h = ref Unsafe.Add(ref hashesAndIndexes, ++hashIndex);
#else
            h = ref hashesAndIndexes[++hashIndex];
#endif
            ++probes;
            if ((h >>> ProbeCountShift) < probes)
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
        // keep the already met overflow or set it if the last inserted index is the last one
        _hashesOverflowBufferIsFull |= (hashIndex + 1) == _packedHashesAndIndexes.Length;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryRemove(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBits) - 1;
        var lastIndex = (1 << _capacityBits) + (_capacityBits - 1);
        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        ref var h = ref GetElementRef(ref hashesAndIndexes, hashIndex);

        var removed = false;
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
                    removed = true;
                    h = 0;
                    e = default;
                    --_entryCount;
                    break;
                }
            }
            h = ref GetElementRef(ref hashesAndIndexes, ++hashIndex);
            ++probes;
        }

        if (!removed)
            return false;

        ref var emptied = ref h;
        h = ref GetElementRef(ref hashesAndIndexes, ++hashIndex);

        // move the next hash into the emptied slot until the next hash is empty or ideally positioned (hash is 0 or probe is 1)
        while ((h >>> ProbeCountShift) > 1)
        {
            emptied = (((h >>> ProbeCountShift) - 1) << ProbeCountShift) | (h & HashAndIndexMask); // decrease the probe count by one cause we moving the hash closer to the ideal index
            h = 0;

            emptied = ref h;
            h = ref GetElementRef(ref hashesAndIndexes, ++hashIndex);
        }
        return true;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
#if NET7_0_OR_GREATER
    private static ref int GetElementRef(ref int start, int distance) => ref Unsafe.Add(ref start, distance);
#else
    private static ref int GetElementRef(ref int[] start, int distance) => ref start[distance];
#endif

    internal void ResizeHashes(int indexMask)
    {
        if (indexMask == 0)
        {
            _capacityBits = MinCapacityBits;
            _packedHashesAndIndexes = new int[(1 << MinCapacityBits) + MinCapacityBits];
            // no need to reset _lastAddedIndex because it will be reassigned by AddOrUpdate anyway
#if DEBUG
            Debug.WriteLine($"[ResizeHashes] new empty hashes with overflow buffer {1} -> {_packedHashesAndIndexes.Length}");
#endif
            return;
        }

        var oldCapacityBits = _capacityBits;
        var oldCapacity = indexMask + 1;
        var oldCapacityWithOverflow = oldCapacity + oldCapacityBits;
        var newHashAndIndexMask = ~oldCapacity & HashAndIndexMask;
        var newHashesAndIndexes = new int[(oldCapacity << 1) + (oldCapacityBits + 1)];

#if NET7_0_OR_GREATER
        ref var newHashes = ref MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);
        ref var oldHashes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var oldHashes = _packedHashesAndIndexes;
#endif

#if NET7_0_OR_GREATER
        for (var i = 0; i < oldCapacityWithOverflow; ++i)
        {
            var oldHash = Unsafe.Add(ref oldHashes, i);
#else
        for (var i = 0; (uint)i < (uint)_packedHashesAndIndexes.Length; ++i)
        {
            var oldHash = _packedHashesAndIndexes[i];
#endif
            if (oldHash != 0)
            {
                // get the new hash index from the old one with the next bit equal to the `oldCapacity`
                var indexWithNextBit = (oldHash & oldCapacity) | (i - (oldHash >>> ProbeCountShift) + 1);

                // no need for robinhooding because we already did it for the old hashes and now just sparcing the hashes which are already in order
                var idealIndex = indexWithNextBit;
                // todo: @perf vectorize this - lookup for the first empty slot
#if NET7_0_OR_GREATER
                ref var h = ref Unsafe.Add(ref newHashes, indexWithNextBit);
#else
                ref var h = ref newHashesAndIndexes[indexWithNextBit];
#endif
                while (h != 0)
                {
                    ++indexWithNextBit;
#if NET7_0_OR_GREATER
                    h = ref Unsafe.Add(ref h, 1);
#else
                    h = ref newHashesAndIndexes[indexWithNextBit];
#endif
                }
                h = ((indexWithNextBit - idealIndex + 1) << ProbeCountShift) | (oldHash & newHashAndIndexMask);
            }
        }

#if DEBUG
        Debug.WriteLine($"[ResizeHashes] with overflow buffer {oldCapacityWithOverflow} -> {newHashesAndIndexes.Length}");
#endif
        ++_capacityBits;
        _packedHashesAndIndexes = newHashesAndIndexes;
    }
}
