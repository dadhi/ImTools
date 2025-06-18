using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ImTools.Experiments;

using static FHashMap11;

public static class FHashMap11
{
    /// <summary>2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803</summary>
    public const uint GoldenRatio32 = 2654435769;

    internal const byte MinFreeCapacityShift = 3; // e.g. for the capacity 16: 16 >> 3 => 2, 12.5% of the free hash slots (it does not mean the entries free slot)
    internal const byte MinCapacityBits = 3; // 1 << 3 == 8

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<bool> assertEq)
        where TEq : struct, IEq<K>
    {
        for (var i = 0; i < map.Probes.Length; ++i)
            if (map.Probes[i] != 0)
                assertEq(default(TEq).GetHashCode(map.Keys[i]) == map.Hashes[i]);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<K> assertKey)
        where TEq : struct, IEq<K>
    {
        // Verify the indexes do no contains duplicate keys
        var uniq = new Dictionary<K, int>(map.Count);
        var keys = map.Keys;
        for (var i = 0; i < keys.Length; i++)
        {
            if (map.Probes[i] == 0) // skip empty
                continue;
            var key = keys[i];
            if (!uniq.TryGetValue(key, out _))
                uniq.Add(key, 1);
            else
                assertKey(key);
        }
    }

    public static void VerifyProbesAreFitRobinHood<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<string> reportFail)
        where TEq : struct, IEq<K>
    {
        var hashes = map.Hashes;
        var prevProbe = -1;
        for (var i = 0; i < hashes.Length; i++)
        {
            var probe = map.Probes[i];
            if (prevProbe != -1 & probe - prevProbe > 1)
                reportFail($"Probes are not consequent: {prevProbe}, {probe} for {i}: p{probe}, {map.ValueIndexes[i]} -> {map.Keys[i]}");
            prevProbe = probe;
        }
    }

    /// <summary>Verifies that the map contains all passed keys. May be called from the tests.</summary>
    public static void VerifyContainAllKeys<K, V, TEq>(this FHashMap11<K, V, TEq> map, IEnumerable<K> expectedKeys, Action<bool, K> assertContainKey)
        where TEq : struct, IEq<K>
    {
        foreach (var key in expectedKeys)
            assertContainKey(map.TryGetValue(key, out _), key);
    }

    [MethodImpl((MethodImplOptions)256)]
#if NET7_0_OR_GREATER
    internal static ref T GetItemRef<T>(ref T start, int distance) => ref Unsafe.Add(ref start, distance);
#else
    internal static ref T GetItemRef<T>(ref T[] start, int distance) => ref start[distance];
#endif

    [MethodImpl((MethodImplOptions)256)]
#if NET7_0_OR_GREATER
    internal static T GetItem<T>(ref T start, int distance) => Unsafe.Add(ref start, distance);
#else
    internal static T GetItem<T>(ref T[] start, int distance) => start[distance];
#endif

#if DEBUG
    internal struct ProbesTracker
    {
        internal int MaxProbe;
        internal int[] Probes;
        public ProbesTracker()
        {
            MaxProbe = 1;
            Probes = new int[1];
        }

        // will output something like
        // [Add] Probes abs max = 10, curr max = 6, all = [1: 180, 2: 103, 3: 59, 4: 23, 5: 3, 6: 1]; first 4 probes are 365 out of 369
        internal void DebugOutputProbes(string label)
        {
            Debug.Write($"[{label}] Probes abs max={MaxProbe}, curr max={Probes.Length}, all=[");
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
            Debug.WriteLine($"]; first 4 probes are {first4probes} out of {allProbes}");
        }

        internal void DebugCollectAndOutputProbes(int probe, [CallerMemberName] string label = "")
        {
            Probes ??= [];
            if (probe > Probes.Length)
            {
                if (probe > MaxProbe)
                    MaxProbe = probe;
                Array.Resize(ref Probes, probe);
                Probes[probe - 1] = 1;
                DebugOutputProbes(label);
            }
            else
                ++Probes[probe - 1];
        }

        internal void DebugReCollectAndOutputProbes(byte[] probes, [CallerMemberName] string label = "")
        {
            var newProbes = new int[1];
            for (var i = 0; i < probes.Length; ++i)
            {
                var p = probes[i];
                if (p == 0) continue;
                if (p > MaxProbe)
                    MaxProbe = p;
                if (p > newProbes.Length)
                    Array.Resize(ref newProbes, p);
                ++newProbes[p - 1];
            }
            Probes = newProbes;
            DebugOutputProbes(label);
        }

        internal void RemoveProbe(int probe)
        {
            ref var p = ref Probes[probe - 1];
            --p;
            if (p == 0 && probe == Probes.Length)
            {
                Array.Resize(ref Probes, probe - 1);
                if (MaxProbe == probe)
                    --MaxProbe;
            }
        }
    }
#endif
}

// todo: @improve ? how/where to add SIMD to improve CPU utilization but not losing perf for smaller sizes
/// <summary>
/// Fast and less-allocating hash map without thread safety nets. Please measure it in your own use case before use.
/// It is configurable in regard of hash calculation/equality via <typeparamref name="TEq"/> and 
/// in regard of key-value storage via <typeparamref name="TEntries"/>
/// 
/// Features:
/// - Implemented as a struct so that the empty/default map does not allocate on heap
/// - Hashes and key-values are the separate collections enabling better cash locality and faster performance (data-oriented design)
/// - No SIMD for now to avoid complexity and costs for the smaller maps, so the map is more fit for the smaller sizes.
/// - Provides the "stable" enumeration of the entries in the added order
/// 
/// </summary>
[DebuggerDisplay("Count={Count}")]
public struct FHashMap11<K, V, TEq> : IReadOnlyCollection<KeyValuePair<K, V>>
    where TEq : struct, IEq<K>
{
#if DEBUG
    ProbesTracker _dbg = new();
#endif
    private byte _capacityBitShift;
    internal int _count;
    public byte[] Probes;
    public int[] Hashes;
    public K[] Keys;
    public int[] ValueIndexes;
    public V[] Values;

    /// <summary>Get the number of the key/hash entries in array. Should be more than <see cref="Count"/></summary>
    public int Capacity => 1 << _capacityBitShift;

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count => _count;

    /// <summary>Capacity calculates as `1 << capacityBitShift`</summary>
    public FHashMap11(byte capacityBitShift)
    {
        _capacityBitShift = capacityBitShift;

        // the overflow tail to the hashes is the size of log2N where N==capacityBitShift, 
        // it is probably fine to have the check for the overlow of capacity because it will be mispredicted only once at the end of loop (it even rarely for the lookup)
        var cap = 1 << capacityBitShift;
        Probes = new byte[cap];
        Hashes = new int[cap];
        Keys = new K[cap];
        ValueIndexes = new int[cap];
        Values = new V[cap];
    }

    /// <summary>Lookup for the key and get the associated value if the key is found</summary>
    [MethodImpl((MethodImplOptions)256)]
    public bool TryGetValue(K key, out V value)
    {
        if (Keys != null)
        {
            var hash = default(TEq).GetHashCode(key);

            var indexMask = (1 << _capacityBitShift) - 1;
            var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
            ref var probes = ref MemoryMarshal.GetArrayDataReference(Probes);
            ref var hashes = ref MemoryMarshal.GetArrayDataReference(Hashes);
            ref var keys = ref MemoryMarshal.GetArrayDataReference(Keys);
            ref var valueIndexes = ref MemoryMarshal.GetArrayDataReference(ValueIndexes);
#else
            var hashAndKeyEntries = KeyEntries;
#endif
            var p = GetItem(ref probes, hashIndex);

            var probe = 1;
            while (p >= probe)
            {
                if ((p == probe) & (GetItem(ref hashes, hashIndex) == hash) &&
                    default(TEq).Equals(GetItem(ref keys, hashIndex), key))
                {
                    value = Values[GetItem(ref valueIndexes, hashIndex)];
                    return true;
                }

                p = GetItem(ref probes, ++hashIndex & indexMask);
                ++probe;
            }
        }

        value = default;
        return false;
    }

    /// <summary>Lookup for the key and get the associated value or the default value if the key is not found</summary>
    [MethodImpl((MethodImplOptions)256)]
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>Gets the reference to the existing value of the provided key, or the default value to set for the newly added key.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public ref V GetOrAddValueRef(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var currCount = _count;

        // if the free space is less than 1/8 of capacity (12.5%) then Resize
        if (indexMask - currCount <= (indexMask >>> MinFreeCapacityShift))
            indexMask = ResizeHashes(indexMask);

        var index = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var probes = ref MemoryMarshal.GetArrayDataReference(Probes);
        ref var hashes = ref MemoryMarshal.GetArrayDataReference(Hashes);
        ref var keys = ref MemoryMarshal.GetArrayDataReference(Keys);
        ref var valueIndexes = ref MemoryMarshal.GetArrayDataReference(ValueIndexes);
#else
        var hashAndKeyEntries = KeyEntries;
#endif
        ref var pRef = ref GetItemRef(ref probes, index);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probe = (byte)1;
        while (pRef >= probe)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if ((pRef == probe) & GetItem(ref hashes, index) == hash &&
                default(TEq).Equals(GetItem(ref keys, index), key))
                return ref Values[GetItem(ref valueIndexes, index)];

            index = (index + 1) & indexMask;
            pRef = ref GetItemRef(ref probes, index);
            ++probe;
        }

        // Nothing found, add a new entry and inscrease the count first
        _count = currCount + 1;

    // 3. There is an empty slot to insert the new entry, so we can just insert it
    insert:
        if (pRef == 0)
        {
#if DEBUG
            _dbg.DebugCollectAndOutputProbes(probe, "Add in the empty slot");
#endif
            pRef = probe;
            GetItemRef(ref hashes, index) = hash;
            GetItemRef(ref keys, index) = key;
            GetItemRef(ref valueIndexes, index) = currCount;
        }
        else // p < probe, e.g. 3, 4, 5, (5<6), (6==6), (7==7), (3<8), (0<4)
        {
            // 4. If the slot is not empty, then robin-hood the smaller (more valuable) probe
            // and put the newly added item into the free slot. 
            // Then proceed with the robin-hooded key as-if it was a newly inserted key.
            var rhProbe = pRef;
            var rhHash = GetItem(ref hashes, index);
            var rhKey = GetItem(ref keys, index);
            var rhValueIndex = GetItem(ref valueIndexes, index);

#if DEBUG
            _dbg.DebugCollectAndOutputProbes(probe, "Add into the occupied slot after RobinHood it");
#endif
            // and set the new entry to the current slot
            pRef = probe;
            GetItemRef(ref hashes, index) = hash;
            GetItemRef(ref keys, index) = key;
            GetItemRef(ref valueIndexes, index) = currCount;

            // 5. Now treat the robin-hooded entry as a newly inserted entry 
            probe = rhProbe;
            while (true)
            {
                index = (index + 1) & indexMask;
                pRef = ref GetItemRef(ref probes, index);
                if (pRef < ++probe)
                    goto insert;
            }
        }

        if (currCount == Values.Length)
            Array.Resize(ref Values, Values.Length << 1);
        return ref Values[currCount];
    }

    /// <summary>Same as `GetOrAddValueRef` but provides the value to add or override for the existing key</summary>
    [MethodImpl((MethodImplOptions)256)]
    public void AddOrUpdate(K key, in V value) =>
        GetOrAddValueRef(key) = value;

    //         /// <summary>Removes the hash and entry of the provided key or returns <see langword="false"/></summary>
    //         [MethodImpl((MethodImplOptions)256)]
    //         public bool TryRemove(K key)
    //         {
    //             var hash = default(TEq).GetHashCode(key);

    //             var indexMask = (1 << _capacityBitShift) - 1;
    //             var hashIndex = hash & indexMask;

    // #if NET7_0_OR_GREATER
    //             ref var keyEntries = ref MemoryMarshal.GetArrayDataReference(KeyEntries);
    // #else
    //         var keyEntries = KeyEntries;
    // #endif
    //             ref var h = ref GetItemRef(ref keyEntries, hashIndex);

    //             var removed = false;

    //             // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
    //             var probe = 1;
    //             while (h.Probe >= probe)
    //             {
    //                 // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
    //                 if ((h.Probe == probe) & (h.Hash == hash) && default(TEq).Equals(h.Key, key))
    //                 {
    //                     removed = true;

    //                     --_count;
    //                     Values[h.Index] = default; // todo: @perf fragmentation issue, add the slot to the free list?
    //                     h = default;
    // #if DEBUG
    //                     _dbg.RemoveProbe(probe);
    // #endif
    //                     break;
    //                 }
    //                 h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);
    //                 ++probe;
    //             }

    //             if (!removed)
    //                 return false;

    //             ref var emptied = ref h;
    //             h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);

    //             // move the next hash into the emptied slot until the next hash is empty or ideally positioned (hash is 0 or probe is 1)
    //             while (h.Probe > 1)
    //             {
    //                 emptied = h;
    //                 emptied.Probe -= 1; // decrease the probe count by one cause we moving the hash closer to the ideal index
    //                 h = default;

    //                 emptied = ref h;
    //                 h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);
    //             }
    //             return true;
    //         }

    internal int ResizeHashes(int indexMask)
    {
        if (indexMask == 0)
        {
            _capacityBitShift = MinCapacityBits;
            var cap = 1 << MinCapacityBits;
            Probes = new byte[cap];
            Hashes = new int[cap];
            Keys = new K[cap];
            ValueIndexes = new int[cap];
            Values = new V[cap];
#if DEBUG
            Debug.WriteLine($"[ResizeHashes] new empty hashes {1} -> {Probes.Length}");
#endif
            return cap - 1;
        }

        var oldCap = indexMask + 1;
        var newCap = oldCap << 1;
        var newProbes = new byte[newCap];
        var newHashes = new int[newCap];
        var newKeys = new K[newCap];
        var newValueIndexes = new int[newCap];
        var newIndexMask = newCap - 1;

#if NET7_0_OR_GREATER
        ref var newProbesRef = ref MemoryMarshal.GetArrayDataReference(newProbes);
        ref var oldProbesRef = ref MemoryMarshal.GetArrayDataReference(Probes);
        var oldProbe = oldProbesRef;

        ref var newHashesRef = ref MemoryMarshal.GetArrayDataReference(newHashes);
        ref var oldHashesRef = ref MemoryMarshal.GetArrayDataReference(Hashes);

        ref var newKeysRef = ref MemoryMarshal.GetArrayDataReference(newKeys);
        ref var oldKeysRef = ref MemoryMarshal.GetArrayDataReference(Keys);

        ref var newValueIndexesRef = ref MemoryMarshal.GetArrayDataReference(newValueIndexes);
        ref var oldValueIndexesRef = ref MemoryMarshal.GetArrayDataReference(ValueIndexes);
#else
        var newKeys = newKeys;
        var oldKeys = Keys;
        var oldKey = oldKeys[0];
#endif
        // Overflow segment is wrapped-around hashes and! the hashes at the beginning robin-hooded by the wrapped-around hashes
        // so we skip them to start from first ideal or empty entry
        var i = 0;
        while (oldProbe > 1)
            oldProbe = GetItem(ref oldProbesRef, ++i);
        var oldCapWithOverflowSegment = i + oldCap;

        while (true)
        {
            if (oldProbe != 0)
            {
                var hash = GetItem(ref oldHashesRef, i);
                var newIndex = hash & newIndexMask;

                // no need for robin-hooding because we already did it for the old hashes and
                // now just sparsing the hashes which are already in order into the new array
                var newProbe = (byte)1;
                ref var newProbeRef = ref GetItemRef(ref newProbesRef, newIndex);
                while (newProbeRef != 0)
                {
                    newIndex = (newIndex + 1) & newIndexMask;
                    newProbeRef = ref GetItemRef(ref newProbeRef, newIndex);
                    ++newProbe;
                }
                newProbeRef = newProbe;
                GetItemRef(ref newHashesRef, newIndex) = hash;
                GetItemRef(ref newKeysRef, newIndex) = GetItem(ref oldKeysRef, i);
                GetItemRef(ref newValueIndexesRef, newIndex) = GetItem(ref oldValueIndexesRef, i);
            }
            if (++i >= oldCapWithOverflowSegment)
                break;

            oldProbe = GetItem(ref oldProbesRef, i & indexMask);
        }
#if DEBUG
        Debug.WriteLine($"[ResizeHashes] {oldCap} -> {newProbes.Length}");
        _dbg.DebugReCollectAndOutputProbes(newProbes, "ResizeHashes");
#endif
        ++_capacityBitShift;
        Probes = newProbes;
        Hashes = newHashes;
        Keys = newKeys;
        ValueIndexes = newValueIndexes;
        return newIndexMask;
    }

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public Enumerator GetEnumerator() => new Enumerator(Probes, ValueIndexes, Keys, Values, _count);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Enumerator of the entries in the order of their addition to the map</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>
    {
        private int _probeIndex;
        private int _index;
        private KeyValuePair<K, V> _current;
        private readonly byte[] _probes;
        private readonly int[] _valueIndexes;
        private readonly K[] _keys;
        private readonly V[] _values;
        private int _count;
        internal Enumerator(byte[] probes, int[] valueIndexes, K[] keys, V[] values, int count)
        {
            _probeIndex = 0;
            _index = 0;
            _current = default;
            _probes = probes;
            _valueIndexes = valueIndexes;
            _keys = keys;
            _values = values;
            _count = count;
        }

        /// <summary>Move to the next entry in the order of their addition to the map</summary>
        [MethodImpl((MethodImplOptions)256)]
        public bool MoveNext()
        {
            if (_index < _count)
            {
                // skip empty hashes
                var i = _probeIndex;
                while (_probes[i] == 0)
                    ++i;
                _probeIndex = i;
                _current = new KeyValuePair<K, V>(_keys[i], _values[_valueIndexes[i]]);

                ++_probeIndex;
                ++_index;
                return true;
            }

            _current = default;
            return false;
        }

        public KeyValuePair<K, V> Current => _current;
        object IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            _probeIndex = 0;
            _index = 0;
            _current = default;
        }

        public void Dispose() { }
    }
}
