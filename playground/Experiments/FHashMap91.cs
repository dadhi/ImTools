using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
namespace ImTools.Experiments;

using static FHashMap91;

public static class FHashMap91
{
    public const uint GoldenRatio32 = 2654435769; // 2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803

    public const byte MinFreeCapacityShift = 3; // e.g. for the capacity 16: 16 >> 3 => 2, 12.5% of the free hash slots (it does not mean the entries free slot)
    public const byte MinCapacityBits = 3; // 1 << 3 == 8
    public const byte MaxProbeBits = 5; // 5 bits max, e.g. 31 (11111)
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1; // e.g. 31 (11111) for the 5 bits
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int ProbesMask = MaxProbeCount << ProbeCountShift;
    public const int HashAndIndexMask = ~ProbesMask;

    internal static readonly int[] SingleCellHashesAndIndexes = new int[1];

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public static FHashMap91<K, V, TEq, SingleArrayEntries<K, V>> New<K, V, TEq>(byte capacityBitShift = 0)
        where TEq : struct, IEqualityComparer<K> =>
        new FHashMap91<K, V, TEq, SingleArrayEntries<K, V>>(capacityBitShift);

    // todo: @name better name like NewMemEfficient or NewAddFocused ?
    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public static FHashMap91<K, V, TEq, ChunkedArrayEntries<K, V>> NewChunked<K, V, TEq>(byte capacityBitShift = 0)
        where TEq : struct, IEqualityComparer<K> =>
        new FHashMap91<K, V, TEq, ChunkedArrayEntries<K, V>>(capacityBitShift);

    [DebuggerDisplay("{Key.ToString()}->{Value}")]
    public struct Entry<K, V>
    {
        public K Key;
        public V Value;
    }

    public struct DebugItem<K, V>
    {
        public int Probe;
        public bool HEq;
        public string Hash;
        public int Index;
        public bool IsEmpty => Probe == 0;
        public string Output => $"{Probe}|{Hash}|{Index}";
        public override string ToString() => IsEmpty ? "empty" : Output;
    }

    /// <summary>Converts the packed hashes and entries into the human readable info.
    /// This also used for the debugging view of the <paramref name="map"/> and by the Verify... methods in tests.</summary>
    public static DebugItem<K, V>[] Explain<K, V, TEq, TEntries>(this FHashMap91<K, V, TEq, TEntries> map)
        where TEq : struct, IEqualityComparer<K>
        where TEntries : struct, IEntries<K, V>
    {
        var hashes = map.PackedHashesAndIndexes;
        var capacityBitShift = map.CapacityBitShift;
        var capacity = 1 << capacityBitShift;
        var indexMask = capacity - 1;

        var items = new DebugItem<K, V>[hashes.Length];
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            if (h == 0)
                continue;

            var probe = h >>> ProbeCountShift;
            var hashIndex = i - probe + 1;

            var hashMiddle = (h & HashAndIndexMask & ~indexMask);
            var hash = hashMiddle | hashIndex;
            var entryIndex = h & indexMask;

            var heq = false;
            if (probe != 0)
            {
                ref var e = ref map.Entries.GetSurePresentEntryRef(entryIndex);
                var kh = default(TEq).GetHashCode(e.Key) & HashAndIndexMask;
                heq = kh == hash;
            }
            items[i] = new DebugItem<K, V> { Probe = probe, Hash = toB(hash), HEq = heq, Index = entryIndex };
        }
        return items;

        // binary reprsentation of the `int`
        static string toB(int x) => Convert.ToString(x, 2).PadLeft(32, '0');
    }

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq, TEntries>(this FHashMap91<K, V, TEq, TEntries> map, Action<bool> assertEq)
        where TEq : struct, IEqualityComparer<K>
        where TEntries : struct, IEntries<K, V>
    {
        var exp = map.Explain();
        foreach (var it in exp)
            if (!it.IsEmpty)
                assertEq(it.HEq);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq, TEntries>(this FHashMap91<K, V, TEq, TEntries> map, Action<K> assertKey)
        where TEq : struct, IEqualityComparer<K>
        where TEntries : struct, IEntries<K, V>
    {
        // Verify the indexes do no contains duplicate keys
        var uniq = new Dictionary<K, int>(map.Count);
        var hashes = map.PackedHashesAndIndexes;
        var capacityBitShift = map.CapacityBitShift;
        var capacity = 1 << capacityBitShift;
        var indexMask = capacity - 1;
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            if (h == 0)
                continue;
            var key = map.Entries.GetSurePresentEntryRef(h & indexMask).Key;
            if (!uniq.TryGetValue(key, out var count))
                uniq.Add(key, 1);
            else
                assertKey(key);
        }
    }

    /// <summary>Verifies that the map contains all passed keys. May be called from the tests.</summary>
    public static void VerifyContainAllKeys<K, V, TEq, TEntries>(this FHashMap91<K, V, TEq, TEntries> map, IEnumerable<K> expectedKeys, Action<bool, K> assertContainKey)
        where TEq : struct, IEqualityComparer<K>
        where TEntries : struct, IEntries<K, V>
    {
        foreach (var key in expectedKeys)
            assertContainKey(map.TryGetValue(key, out _), key);
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
#if NET7_0_OR_GREATER
    internal static ref int GetHashRef(ref int start, int distance) => ref Unsafe.Add(ref start, distance);
#else
    internal static ref int GetHashRef(ref int[] start, int distance) => ref start[distance];
#endif

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
#if NET7_0_OR_GREATER
    internal static int GetHash(ref int start, int distance) => Unsafe.Add(ref start, distance);
#else
    internal static int GetHash(ref int[] start, int distance) => start[distance];
#endif

    // todo: @improve can we move the Entry into the type parameter to configure and possibly save the memory e.g. for the sets? 
    /// <summary>Abstraction to configure your own entries data structure. Check the derivitives for the examples</summary>
    public interface IEntries<K, V>
    {
        void Init(byte capacityBitShift);
        int GetCount();
        ref Entry<K, V> GetSurePresentEntryRef(int index);
        void AppendEntry(in K key, in V value);
        void RemoveSurePresentEntry(int index);
    }

    public const int MinEntriesCapacity = 2;

    public struct SingleArrayEntries<K, V> : IEntries<K, V>
    {
        int _entryCount;
        Entry<K, V>[] _entries;

        public void Init(byte capacityBitShift) =>
            _entries = new Entry<K, V>[1 << capacityBitShift];

        [MethodImpl((MethodImplOptions)256)]
        public int GetCount() => _entryCount;

        [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
        public ref Entry<K, V> GetSurePresentEntryRef(int index)
        {
#if NET7_0_OR_GREATER
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
#else
            return ref _entries[index];
#endif
        }

        [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
        public void AppendEntry(in K key, in V value)
        {
            var index = _entryCount;
            if (index == 0)
                _entries = new Entry<K, V>[MinEntriesCapacity];
            else if (index == _entries.Length)
            {
#if DEBUG
                Debug.WriteLine($"[AllocateEntries] Resize entries: {index} -> {index << 1}");
#endif
                Array.Resize(ref _entries, index << 1);
            }
#if NET7_0_OR_GREATER
            ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
#else
            ref var e = ref _entries[index];
#endif
            e.Key = key;
            e.Value = value;
            ++_entryCount;
        }

        [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
        public void RemoveSurePresentEntry(int index)
        {
            GetSurePresentEntryRef(index) = default;
            --_entryCount;
        }
    }

    const byte ChunkCapacityBitShift = 8; // 8 bits == 256
    const int ChunkCapacity = 1 << ChunkCapacityBitShift;
    const int ChunkCapacityMask = ChunkCapacity - 1;

    public struct ChunkedArrayEntries<K, V> : IEntries<K, V>
    {
        int _entryCount;
        Entry<K, V>[][] _entries;

        public void Init(byte capacityBitShift) =>
            _entries = new[] { new Entry<K, V>[(1 << capacityBitShift) & ChunkCapacityMask] };

        [MethodImpl((MethodImplOptions)256)]
        public int GetCount() => _entryCount;

        [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
        public ref Entry<K, V> GetSurePresentEntryRef(int index)
        {
#if NET7_0_OR_GREATER
            ref var entries = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index >>> ChunkCapacityBitShift);
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), index & ChunkCapacityMask);
#else
            return ref _entries[index >>> ChunkCapacityBitShift][index & ChunkCapacityMask];
#endif
        }

        public void AppendEntry(in K key, in V value)
        {
            var index = _entryCount++;
            var bucketIndex = index >>> ChunkCapacityBitShift;
            if (bucketIndex == 0) // small count of element fit into a single array
            {
                if (index != 0)
                {
    #if NET7_0_OR_GREATER
                    ref var bucket = ref MemoryMarshal.GetArrayDataReference(_entries);
    #else
                    ref var bucket = ref _entries[0];
    #endif
                    if (index == bucket.Length)
                        Array.Resize(ref bucket, index << 1);

    #if NET7_0_OR_GREATER
                    ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bucket), index);
    #else
                    ref var e = ref bucket[index];
    #endif
                    e.Key = key;
                    e.Value = value;
                    return;
                }
                {
                    var bucket = new Entry<K, V>[MinEntriesCapacity];
#if NET7_0_OR_GREATER
                    ref var e = ref MemoryMarshal.GetArrayDataReference(bucket);
#else
                    ref var e = ref bucket[0];
#endif
                    e.Key = key;
                    e.Value = value;
                    _entries = new[] { bucket };
                    return;
                }
            }

            if ((index & ChunkCapacityMask) != 0)
            {
                ref var e = ref GetSurePresentEntryRef(index);
                e.Key = key;
                e.Value = value;
                return;
            }
            {
                if (bucketIndex == _entries.Length)
                    Array.Resize(ref _entries, bucketIndex << 1);
#if NET7_0_OR_GREATER
                ref var bucket = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), bucketIndex);
#else
                ref var bucket = ref _entries[bucketIndex];
#endif
                bucket = new Entry<K, V>[ChunkCapacity];
#if NET7_0_OR_GREATER
                ref var e = ref MemoryMarshal.GetArrayDataReference(bucket);
#else
                ref var e = ref bucket[0];
#endif
                e.Key = key;
                e.Value = value;
            }
        }

        [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
        public void RemoveSurePresentEntry(int index)
        {
            GetSurePresentEntryRef(index) = default;
            --_entryCount;
            // todo: @perf we may try to free the chunk if it is empty
        }
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
    public int GetHashCode(K obj) => (int)(obj.GetHashCode() * FHashMap91.GoldenRatio32) >>> FHashMap91.MaxProbeBits;
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

#if DEBUG
public class FHashMap91DebugProxy<K, V, TEq, TEntries>
    where TEq : struct, IEqualityComparer<K>
    where TEntries : struct, IEntries<K, V>
{
    private readonly FHashMap91<K, V, TEq, TEntries> _map;
    public FHashMap91DebugProxy(FHashMap91<K, V, TEq, TEntries> map) => _map = map;
    public FHashMap91.DebugItem<K, V>[] PackedHashes => _map.Explain();
    public TEntries Entries => _map.Entries;
}

[DebuggerTypeProxy(typeof(FHashMap91DebugProxy<,,,>))]
[DebuggerDisplay("Count={Count}")]
#endif
public struct FHashMap91<K, V, TEq, TEntries>
    where TEq : struct, IEqualityComparer<K>
    where TEntries : struct, IEntries<K, V>
{
    private bool _hashesOverflowBufferIsFull;
    private byte _capacityBitShift;

    // The _packedHashesAndIndexes elements are of `Int32`, 
    // e.g. 00010|000...110|01101
    //      |     |         |- The index into the _entries array, 0-based. It is the size of the hashes array size-1 (e.g. 15 for the 16).
    //      |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so we are safely using it to store the index into entries.
    //      |     |- The middle bits of the hash
    //      |- 5 high bits of the Probe count, with the minimal value of 00001  indicating non-empty slot.
    // todo: @feature remove - for the removed hash we won't use the tumbstone but will actually remove the hash.
    private int[] _packedHashesAndIndexes;
    public int CapacityBitShift => _capacityBitShift;
    internal int[] PackedHashesAndIndexes => _packedHashesAndIndexes;
    private TEntries _entries;
    public int Count => _entries.GetCount();
    internal TEntries Entries => _entries;
    public FHashMap91()
    {
        _capacityBitShift = 0;
        _hashesOverflowBufferIsFull = false;

        // using single cell array for hashes instead of empty one to allow the Lookup to work without the additional check for the emptiness
        _packedHashesAndIndexes = FHashMap91.SingleCellHashesAndIndexes;

        _entries = default;
    }

    /// <summary>Capacity calculates as `1 << capacityBitShift`</summary>
    public FHashMap91(byte capacityBitShift)
    {
        _capacityBitShift = capacityBitShift;
        _hashesOverflowBufferIsFull = false;

        // the overflow tail to the hashes is the size of log2N where N==capacityBitShift, 
        // it is probably fine to have the check for the overlow of capacity because it will be mispredicted only once at the end of loop (it even rarely for the lookup)
        _packedHashesAndIndexes = new int[(1 << capacityBitShift) + capacityBitShift];

        _entries = default;
        _entries.Init(capacityBitShift);
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var lastIndex = (1 << _capacityBitShift) + (_capacityBitShift - 1);
        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif

        var h = GetHash(ref hashesAndIndexes, hashIndex);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashPartMask) == (hash & hashPartMask)))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            if (hashIndex == lastIndex)
                break;

            h = GetHash(ref hashesAndIndexes, ++hashIndex);
            ++probes;
        }

        value = default;
        return false;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

#if DEBUG
    internal int MaxProbes = 1;
    internal int[] Probes = new int[1];

    // will output something like
    // [AddOrUpdate] max_probes = 6, all probes = [1: 180, 2: 103, 3: 59, 4: 23, 5: 3, 6: 1]
    // [AddOrUpdate] first 4 probes total is 365 out of 369
    internal void DebugOutputProbes(string label)
    {
        Debug.Write($"[{label}] Probes abs max = {MaxProbes}, max = {Probes.Length}, all = [");
        var first4probes = 0;
        var allProbes = 0;
        for (var i = 0; i < Probes.Length; i++)
        {
            var p = Probes[i];
            Debug.Write($"{(i == 0 ? "" : ", ")}{i + 1}: {p}");
            if (i < 4)
                first4probes += p;
            allProbes += p;
        }
        Debug.WriteLine("]");
        Debug.WriteLine($"[{label}] first 4 probes total is {first4probes} out of {allProbes}");
    }

    internal void DebugCollectAndOutputProbes(int probes, [CallerMemberName] string label = "")
    {
        if (probes > Probes.Length)
        {
            if (probes > MaxProbes)
                MaxProbes = probes;
            Array.Resize(ref Probes, probes);
            Probes[probes - 1] = 1;
            DebugOutputProbes(label);
        }
        else
            ++Probes[probes - 1];
    }

    internal void DebugReCollectAndOutputProbes(int[] packedHashes, [CallerMemberName] string label = "")
    {
        var newProbes = new int[1];
        foreach (var h in packedHashes)
        {
            if (h == 0) continue;
            var p = h >>> ProbeCountShift;
            if (p > MaxProbes)
                MaxProbes = p;
            if (p > newProbes.Length)
                Array.Resize(ref newProbes, p);
            ++newProbes[p - 1];
        }
        Probes = newProbes;
        DebugOutputProbes(label);
    }
#endif

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        // if the overflow space is filled-in or
        // if the free space is less than 1/8 of capacity (12.5%) then Resize
        var indexMask = (1 << _capacityBitShift) - 1;
        var entryCount = _entries.GetCount();
        if ((indexMask - entryCount <= (indexMask >>> MinFreeCapacityShift)) | _hashesOverflowBufferIsFull)
        {
            ResizeHashes(indexMask);
#if DEBUG
            if (_hashesOverflowBufferIsFull)
                Debug.WriteLine("_hashesOverflowBufferIsFull!");
#endif
            indexMask = (1 << _capacityBitShift) - 1;
            _hashesOverflowBufferIsFull = false;
        }

        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        ref var h = ref GetHashRef(ref hashesAndIndexes, hashIndex);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashPartMask) == (hash & hashPartMask)))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
#if DEBUG
                Debug.WriteLine($"[AddOrUpdate] Probes and Hash parts are matching: probes {probes}, new key:`{key}` with matched hash of key:`{e.Key}`");
#endif
                if (default(TEq).Equals(e.Key, key))
                {
                    e.Value = value;
                    return;
                }
            }
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex);
            ++probes;
        }

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hHooded = h;
        h = (probes << ProbeCountShift) | (hash & hashPartMask) | entryCount;
#if DEBUG
        DebugCollectAndOutputProbes(probes);
#endif

        _entries.AppendEntry(in key, in value);

        // 4. If the hooded hash is empty then we stop
        // 5. Otherwise we steal the slot with the smaller probes
        probes = hHooded >>> ProbeCountShift;
        while (hHooded != 0)
        {
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex);
            if ((h >>> ProbeCountShift) < ++probes)
            {
#if DEBUG
                if (h != 0)
                    --Probes[(h >>> ProbeCountShift) - 1];
                DebugCollectAndOutputProbes(probes, "AddOrUpdate-RH");
#endif
                var hPrev = h;
                h = (probes << ProbeCountShift) | (hHooded & HashAndIndexMask);
                hHooded = hPrev;
                probes = hPrev >>> ProbeCountShift;
            }
        }
        // keep the already met overflow or set it if the last inserted index is the last one
        _hashesOverflowBufferIsFull |= (hashIndex + 1) == _packedHashesAndIndexes.Length;
    }

    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryRemove(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var hashPartMask = ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        ref var h = ref GetHashRef(ref hashesAndIndexes, hashIndex);

        var removed = false;
        var probes = 1;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashPartMask) == (hash & hashPartMask)))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    _entries.RemoveSurePresentEntry(h & indexMask);
                    removed = true;
                    h = 0;
#if DEBUG
                    --Probes[probes - 1];
#endif
                    break;
                }
            }
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex);
            ++probes;
        }

        if (!removed)
            return false;

        ref var emptied = ref h;
        h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex);

        // move the next hash into the emptied slot until the next hash is empty or ideally positioned (hash is 0 or probe is 1)
        while ((h >>> ProbeCountShift) > 1)
        {
            emptied = (((h >>> ProbeCountShift) - 1) << ProbeCountShift) | (h & HashAndIndexMask); // decrease the probe count by one cause we moving the hash closer to the ideal index
            h = 0;

            emptied = ref h;
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex);
        }
        return true;
    }

    internal void ResizeHashes(int indexMask)
    {
        if (indexMask == 0)
        {
            _capacityBitShift = MinCapacityBits;
            _packedHashesAndIndexes = new int[(1 << MinCapacityBits) + MinCapacityBits];
#if DEBUG
            Debug.WriteLine($"[ResizeHashes] new empty hashes with overflow buffer {1} -> {_packedHashesAndIndexes.Length}");
#endif
            return;
        }

        var oldCapacityBits = _capacityBitShift;
        var oldCapacity = indexMask + 1;
        var oldCapacityWithOverflow = oldCapacity + oldCapacityBits;
        var newHashAndIndexMask = ~oldCapacity & HashAndIndexMask;

        var newHashesAndIndexes = new int[(oldCapacity << 1) + (oldCapacityBits + 1)];

#if NET7_0_OR_GREATER
        ref var newHashes = ref MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);
        ref var oldHashes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var oldHashes = _packedHashesAndIndexes;
        var newHashes = newHashesAndIndexes;
#endif
        for (var i = 0; i < oldCapacityWithOverflow; ++i)
        {
            var oldHash = GetHash(ref oldHashes, i);
            if (oldHash != 0)
            {
                // get the new hash index from the old one with the next bit equal to the `oldCapacity`
                var indexWithNextBit = (oldHash & oldCapacity) | ((i + 1) - (oldHash >>> ProbeCountShift));

                // no need for robinhooding because we already did it for the old hashes and now just sparcing the hashes into the new array which are already in order
                var idealIndexMinusOne = indexWithNextBit - 1;
                ref var h = ref GetHashRef(ref newHashes, indexWithNextBit);
                while (h != 0)
                    h = ref GetHashRef(ref newHashes, ++indexWithNextBit);
                h = ((indexWithNextBit - idealIndexMinusOne) << ProbeCountShift) | (oldHash & newHashAndIndexMask);
            }
        }

#if DEBUG
        Debug.WriteLine($"[ResizeHashes] with overflow buffer {oldCapacity}+{_capacityBitShift}={oldCapacityWithOverflow} -> {oldCapacity << 1}+{_capacityBitShift + 1}={newHashesAndIndexes.Length}");
        DebugReCollectAndOutputProbes(newHashesAndIndexes);
#endif
        ++_capacityBitShift;
        _packedHashesAndIndexes = newHashesAndIndexes;
    }
}
