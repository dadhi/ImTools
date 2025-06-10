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

    public struct KeyEntry<K>
    {
        public int Probe;
        public int Hash;
        public int Index;
        public K Key;
        public bool IsEmpty => Probe == 0;
        public override string ToString() => Probe == 0 ? "<empty>" : $"probe:{Probe},index:{Index},hash:0b{Hash:b0},key:{Key}";
    }

    /// <summary>Converts the packed hashes and entries into the human readable info.
    /// This also used for the debugging view of the <paramref name="map"/> and by the Verify... methods in tests.</summary>
    public static KeyEntry<K>[] Explain<K, V, TEq>(this ref FHashMap11<K, V, TEq> map)
        where TEq : struct, IEq<K> => map.KeyEntries;

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<bool> assertEq)
        where TEq : struct, IEq<K>
    {
        foreach (var e in map.KeyEntries)
            if (!e.IsEmpty)
                assertEq(default(TEq).GetHashCode(e.Key) == e.Hash);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<K> assertKey)
        where TEq : struct, IEq<K>
    {
        // Verify the indexes do no contains duplicate keys
        var uniq = new Dictionary<K, int>(map.Count);
        var keys = map.KeyEntries;
        for (var i = 0; i < keys.Length; i++)
        {
            var k = keys[i];
            if (k.IsEmpty)
                continue;
            var key = k.Key;
            if (!uniq.TryGetValue(key, out var count))
                uniq.Add(key, 1);
            else
                assertKey(key);
        }
    }

    public static void VerifyProbesAreFitRobinHood<K, V, TEq>(this FHashMap11<K, V, TEq> map, Action<string> reportFail)
        where TEq : struct, IEq<K>
    {
        var hashes = map.KeyEntries;
        var prevProbe = -1;
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            var probe = h.Probe;
            if (prevProbe != -1 & probe - prevProbe > 1)
                reportFail($"Probes are not consequent: {prevProbe}, {probe} for {i}: p{probe}, {h.Index} -> {h.Key}");
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
        internal int MaxProbes;
        internal int[] Probes;
        public ProbesTracker()
        {
            MaxProbes = 1;
            Probes = new int[1];
        }

        // will output something like
        // [Add] Probes abs max = 10, curr max = 6, all = [1: 180, 2: 103, 3: 59, 4: 23, 5: 3, 6: 1]; first 4 probes are 365 out of 369
        internal void DebugOutputProbes(string label)
        {
            Debug.Write($"[{label}] Probes abs max={MaxProbes}, curr max={Probes.Length}, all=[");
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

        internal void DebugCollectAndOutputProbes(int probes, [CallerMemberName] string label = "")
        {
            Probes ??= [];
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

        internal void DebugReCollectAndOutputProbes<K>(KeyEntry<K>[] hashesAndKeys, [CallerMemberName] string label = "")
        {
            var newProbes = new int[1];
            foreach (var h in hashesAndKeys)
            {
                var p = h.Probe;
                if (p == 0) continue;
                if (p > MaxProbes)
                    MaxProbes = p;
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
                if (MaxProbes == probe)
                    --MaxProbes;
            }
        }
    }
#endif

    public class DebugProxy<K, V, TEq>
        where TEq : struct, IEq<K>
    {
        private readonly FHashMap11<K, V, TEq> _map;
        public DebugProxy(FHashMap11<K, V, TEq> map) => _map = map;
        public KeyEntry<K>[] KeyEntries => _map.KeyEntries;
        public V[] Values => _map.Values;
    }
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
[DebuggerTypeProxy(typeof(DebugProxy<,,>))]
[DebuggerDisplay("Count={Count}")]
public struct FHashMap11<K, V, TEq> : IReadOnlyCollection<KeyValuePair<K, V>>
    where TEq : struct, IEq<K>
{
#if DEBUG
    ProbesTracker _dbg = new();
#endif
    private byte _capacityBitShift;
    internal int _count;
    public KeyEntry<K>[] KeyEntries;
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
        KeyEntries = new KeyEntry<K>[1 << capacityBitShift];
        Values = new V[1 << capacityBitShift];
    }

    /// <summary>Lookup for the key and get the associated value if the key is found</summary>
    [MethodImpl((MethodImplOptions)256)]
    public bool TryGetValue(K key, out V value)
    {
        if (KeyEntries != null)
        {
            var hash = default(TEq).GetHashCode(key);

            var indexMask = (1 << _capacityBitShift) - 1;
            var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
            ref var hashAndKeyEntries = ref MemoryMarshal.GetArrayDataReference(KeyEntries);
#else
            var hashAndKeyEntries = KeyEntries;
#endif
            var h = GetItem(ref hashAndKeyEntries, hashIndex);

            var probe = 1;
            while (h.Probe >= probe)
            {
                if ((h.Probe == probe) & (h.Hash == hash) && default(TEq).Equals(h.Key, key))
                {
                    value = Values[h.Index];
                    return true;
                }

                h = GetItem(ref hashAndKeyEntries, ++hashIndex & indexMask);
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

        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var hashAndKeyEntries = ref MemoryMarshal.GetArrayDataReference(KeyEntries);
#else
        var hashAndKeyEntries = KeyEntries;
#endif
        ref var h = ref GetItemRef(ref hashAndKeyEntries, hashIndex);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probe = 1;
        while (h.Probe >= probe)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if ((h.Probe == probe) & (h.Hash == hash) && default(TEq).Equals(h.Key, key))
                return ref Values[h.Index];

            h = ref GetItemRef(ref hashAndKeyEntries, ++hashIndex & indexMask);
            ++probe;
        }

        // nothing found, add a new entry
        _count = currCount + 1;

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hRobinHooded = h;

        h.Hash = hash;
        h.Probe = probe;
        h.Index = currCount;
        h.Key = key;

#if DEBUG
        _dbg.DebugCollectAndOutputProbes(probe, "Add");
#endif
        // 4. If the robin hooded hash is empty then we stop
        // 5. Otherwise we steal the slot with the smaller probes
        probe = hRobinHooded.Probe;
        while (probe != 0)
        {
            h = ref GetItemRef(ref hashAndKeyEntries, ++hashIndex & indexMask);
            if (h.Probe < ++probe)
            {
#if DEBUG
                if (h.Probe != 0)
                    _dbg.RemoveProbe(h.Probe);
                _dbg.DebugCollectAndOutputProbes(probe, "Add-RH");
#endif
                var tmp = h;

                h = hRobinHooded;
                h.Probe = probe;

                hRobinHooded = tmp;
                probe = hRobinHooded.Probe;
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

    /// <summary>Removes the hash and entry of the provided key or returns <see langword="false"/></summary>
    [MethodImpl((MethodImplOptions)256)]
    public bool TryRemove(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
        ref var keyEntries = ref MemoryMarshal.GetArrayDataReference(KeyEntries);
#else
        var keyEntries = KeyEntries;
#endif
        ref var h = ref GetItemRef(ref keyEntries, hashIndex);

        var removed = false;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probe = 1;
        while (h.Probe >= probe)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if ((h.Probe == probe) & (h.Hash == hash) && default(TEq).Equals(h.Key, key))
            {
                removed = true;

                --_count;
                Values[h.Index] = default; // todo: @perf fragmentation issue, add the slot to the free list?
                h = default;
#if DEBUG
                _dbg.RemoveProbe(probe);
#endif
                break;
            }
            h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);
            ++probe;
        }

        if (!removed)
            return false;

        ref var emptied = ref h;
        h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);

        // move the next hash into the emptied slot until the next hash is empty or ideally positioned (hash is 0 or probe is 1)
        while (h.Probe > 1)
        {
            emptied = h;
            emptied.Probe -= 1; // decrease the probe count by one cause we moving the hash closer to the ideal index
            h = default;

            emptied = ref h;
            h = ref GetItemRef(ref keyEntries, ++hashIndex & indexMask);
        }
        return true;
    }

    internal int ResizeHashes(int indexMask)
    {
        if (indexMask == 0)
        {
            _capacityBitShift = MinCapacityBits;
            KeyEntries = new KeyEntry<K>[1 << MinCapacityBits];
            Values = new V[1 << MinCapacityBits];
#if DEBUG
            Debug.WriteLine($"[ResizeHashes] new empty hashes {1} -> {KeyEntries.Length}");
#endif
            return (1 << MinCapacityBits) - 1;
        }

        var oldCapacity = indexMask + 1;
        var newKeyEntries = new KeyEntry<K>[oldCapacity << 1];
        var newIndexMask = indexMask << 1 | 1;

#if NET7_0_OR_GREATER
        ref var newKeys = ref MemoryMarshal.GetArrayDataReference(newKeyEntries);
        ref var oldKeys = ref MemoryMarshal.GetArrayDataReference(KeyEntries);
        var oldKey = oldKeys;
#else
        var newKeys = newKeyEntries;
        var oldKeys = KeyEntries;
        var oldKey = oldKeys[0];
#endif
        // Overflow segment is wrapped-around hashes and! the hashes at the beginning robin-hooded by the wrapped-around hashes
        // so we skip them to start from first ideal or empty entry
        var i = 0;
        while (oldKey.Probe > 1)
            oldKey = GetItem(ref oldKeys, ++i);
        var oldCapacityWithOverflowSegment = i + oldCapacity;

        while (true)
        {
            if (oldKey.Probe != 0)
            {
                var newHashIndex = oldKey.Hash & newIndexMask;

                // no need for robin-hooding because we already did it for the old hashes and
                // now just sparsing the hashes which are already in order into the new array
                var newProbe = 1;
                ref var newKey = ref GetItemRef(ref newKeys, newHashIndex);
                while (newKey.Probe != 0)
                {
                    newKey = ref GetItemRef(ref newKeys, ++newHashIndex & newIndexMask);
                    ++newProbe;
                }
                newKey = oldKey;
                newKey.Probe = newProbe;
            }
            if (++i >= oldCapacityWithOverflowSegment)
                break;

            oldKey = GetItem(ref oldKeys, i & indexMask);
        }
#if DEBUG
        Debug.WriteLine($"[ResizeHashes] {oldCapacity} -> {newKeyEntries.Length}");
        _dbg.DebugReCollectAndOutputProbes(newKeyEntries);
#endif
        ++_capacityBitShift;
        KeyEntries = newKeyEntries;
        return newIndexMask;
    }

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public Enumerator GetEnumerator() => new Enumerator(KeyEntries, Values, _count);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Enumerator of the entries in the order of their addition to the map</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>
    {
        private int _keyIndex;
        private int _index;
        private KeyValuePair<K, V> _current;
        private readonly KeyEntry<K>[] _keys;
        private readonly V[] _values;
        private int _count;
        internal Enumerator(KeyEntry<K>[] keys, V[] values, int count)
        {
            _keyIndex = 0;
            _index = 0;
            _current = default;
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
                var i = _keyIndex;
                while (_keys[i].Probe == 0)
                    ++i;
                _keyIndex = i;
                ref var k = ref _keys[i];
                _current = new KeyValuePair<K, V>(k.Key, _values[k.Index]);

                ++_keyIndex;
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
            _keyIndex = 0;
            _index = 0;
            _current = default;
        }

        public void Dispose() { }
    }
}
