using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Numerics;
using System.Runtime.InteropServices;
#endif
namespace ImTools.Experiments;

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
    public int GetHashCode(int obj) => (int)(obj * FHashMap91.GoldenRatio32) >>> FHashMap91.MaxProbeBits;
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

public static class FHashMap9Diagnostics
{
    /// <summary>Converts the packed hashes and indexes array into the human readable info</summary>
    public static Item<K, V>[] Explain<K, V, TEq>(this FHashMap9<K, V, TEq> map) where TEq : struct, IEqualityComparer<K>
    {
        var probeCountShift = FHashMap9<K, V, TEq>.ProbeCountShift;
        var hashAndIndexMask = FHashMap9<K, V, TEq>.HashAndIndexMask;

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

    public const uint GoldenRatio32 = 2654435769; // 2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803
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
    // todo: @feature remove - for the removed hash we won't use the tumbstone but will actually remove the hash.
    private int[] _packedHashesAndIndexes;
    private Entry[] _entries;
    // pre-calculated and saved on the DoubleSize for the performance
    private int _indexMask;
    private int _entryCount;

    public int Count => _entryCount;

    internal int[] PackedHashesAndIndexes => _packedHashesAndIndexes;
    internal int HashesCapacity => _indexMask + 1;
    internal Entry[] Entries => _entries;

    internal static int[] _singleCellHashesAndIndexes = new int[1];

    public FHashMap9()
    {
        // using single cell array for hashes instead of empty one to allow the Lookup to work without the additional check for the emptiness
        _packedHashesAndIndexes = _singleCellHashesAndIndexes;
        _entries = Array.Empty<Entry>();
        _indexMask = 0;
        _entryCount = 0;
    }

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
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
#else
        return ref _entries[index];
#endif
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);
#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        var indexMask = _indexMask;

        var probesAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var hashIndex = GetHashIndex(hash, indexMask);

        var probes = 1;
        while (true)
        {
#if NET7_0_OR_GREATER
            var h = Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
            var h = hashesAndIndexes[hashIndex];
#endif
            if ((h & probesAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref GetEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            if ((h >>> ProbeCountShift) < probes)
                break;
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
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
    private void AppendEntry(in K key, in V value)
    {
        var newEntryIndex = _entryCount;
        if (newEntryIndex >= _entries.Length)
        {
#if DEBUG
            Debug.WriteLine($"[AppendEntry] Resize {_entries.Length} -> {_entries.Length << 1}");
#endif
            if (_entries.Length != 0)
                Array.Resize(ref _entries, _entries.Length << 1);
            else
                _entries = new Entry[DefaultEntriesCapacity];
        }
#if NET7_0_OR_GREATER
        ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), newEntryIndex);
#else
        ref var e = ref _entries[newEntryIndex];
#endif
        e.Key = key;
        e.Value = value;
        _entryCount = newEntryIndex + 1;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    private static int GetHashIndex(int hash, int indexMask)
    {
#if NET7_0_OR_GREATER
        return hash & indexMask;
        // return (int)(((uint)hash * GoldenRatio32) >> BitOperations.TrailingZeroCount(indexMask + 1));
#else
        return hash & indexMask;
#endif
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = _indexMask;
        if (indexMask - _entryCount <= (indexMask >>> MinFreeCapacityShift)) // if the free space is less than 1/8 of capacity (12.5%)
        {
            _packedHashesAndIndexes = indexMask == 0 ? new int[2] : ResizeToDoubleCapacity(_packedHashesAndIndexes, indexMask);
            _indexMask = indexMask = (indexMask << 1) | 1;
        }

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif

        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        var hashIndex = GetHashIndex(hash, indexMask);
        var probes = 1;
        while (true)
        {
            Debug.Assert(probes <= MaxProbeCount, $"[AddOrUpdate] DEBUG ASSERT FAILED: probes {probes} <= MaxProbeCount {MaxProbeCount}");
#if NET7_0_OR_GREATER
            ref var h = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
            ref var h = ref hashesAndIndexes[hashIndex];
#endif
            // Robin Hood comes here - to steal the slot with the smaller probes
            var hProbes = h >>> ProbeCountShift;
            if (hProbes < probes) // this check is also includes the check for the empty slot `h == 0`
            {
                var hWithoutProbes = h & HashAndIndexMask;
                h = (probes << ProbeCountShift) | hashMiddle | _entryCount;
#if DEBUG
                if (probes > MaxProbes)
                    Debug.WriteLine($"[AddOrUpdate] MaxProbes {MaxProbes = probes}");
#endif
                AppendEntry(in key, in value);
                probes = hProbes;
                while (probes != 0) // check for the empty slot `h == 0`, because non-empty slot can't have zero probes
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
#if DEBUG
                    if (probes > MaxProbes)
                        Debug.WriteLine($"[AddOrUpdate] MaxProbes {MaxProbes = probes}");
#endif
                        hWithoutProbes = nextHWithoutProbes;
                        probes = hProbes;
                    }
                }
                return;
            }
            if ((h & ~indexMask) == ((probes << ProbeCountShift) | hashMiddle))
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
            ++probes;
            hashIndex = (hashIndex + 1) & indexMask;
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
        for (var i = 0; i < oldCapacity; ++i, oldHash = ref Unsafe.Add(ref oldHash, 1))
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
                    ref var h = ref Unsafe.Add(ref newHash, oldHashNewIndex);
#else
                    ref var h = ref newHashesAndIndexes[oldHashNewIndex];
#endif
                    if (h == 0)
                    {
                        h = (probes << ProbeCountShift) | oldHashWithNextIndexBitErased;
                        break;
                    }
                    if ((h >>> ProbeCountShift) < probes)
                    {
                        var hAndIndex = h & HashAndIndexMask;
                        var hProbes = h >>> ProbeCountShift;
                        h = (probes << ProbeCountShift) | oldHashWithNextIndexBitErased;
                        oldHashWithNextIndexBitErased = hAndIndex;
                        probes = hProbes;
                    }
                    ++probes;
                    oldHashNewIndex = (oldHashNewIndex + 1) & newIndexMask;
                }
            }
        }
        return newHashesAndIndexes;
    }
}
