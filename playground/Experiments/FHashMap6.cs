using System;
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
        var indexMask = map._indexMask;

        var items = new Item<K, V>[hashesAndIndexes.Length];

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

    [MethodImpl((MethodImplOptions)256)]
    public static T GetItemNoBoundsCheck<T>(this T[] source, int i)
    {
#if NET7_0_OR_GREATER
        ref var s = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(source);
        return Unsafe.Add(ref s, i);
#else
        return source[i];
#endif
    }

    [MethodImpl((MethodImplOptions)256)]
    public static ref T GetItemNoBoundsCheckByRef<T>(this T[] source, int i)
    {
#if NET7_0_OR_GREATER
        ref var s = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(source);
        return ref Unsafe.Add(ref s, i);
#else
        return ref source[i];
#endif
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
    internal int _indexMask;
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
        _indexMask = (seedCapacity << 1) - 1;
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
        var probeAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var hashIndex = hash & indexMask;
        byte probes = 1;
        while (true)
        {
#if NET7_0_OR_GREATER
            var h = Unsafe.Add(ref hashesAndIndexes, hashIndex);
#else
            var h = hashesAndIndexes[hashIndex];
#endif
            if ((h >> ProbeCountShift) < probes)
                break;

            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref _entries[h & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            hashIndex = (hashIndex + 1) & indexMask;
            ++probes;
        }
        value = default;
        return false;
    }

    public V GetValueOrDefault(K key, V defaultValue = default)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var indexMask = _indexMask;
        var probeAndHashMask = ~indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;

        var hashIndex = hash & indexMask;
        byte probes = 1;
        while (true)
        {
            var h = hashesAndIndexes[hashIndex];
            if ((h >> ProbeCountShift) < probes)
                break;

            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref _entries[h & indexMask]; // todo: @perf wrap access into the interface to separate the entries abstraction
                if (default(TEq).Equals(e.Key, key))
                    return e.Value;
            }
            hashIndex = (hashIndex + 1) & indexMask;
            ++probes;
        }
        return default;
    }

#if DEBUG
    public int MaxProbes = 1;
    public int FirstProbeAdditions = 0;
#endif

    public void AddOrUpdate(K key, V value)
    {
        var hash = default(TEq).GetHashCode(key);

        var hashesAndIndexes = _hashesAndIndexes;
        var indexMask = _indexMask;
        if (indexMask - _entryCount <= (indexMask >> MinFreeCapacityShift)) // if the free capacity is less free slots 1/16 (6.25%)
        {
            _hashesAndIndexes = hashesAndIndexes = DoubleSize(_hashesAndIndexes);
            _indexMask = indexMask = (indexMask << 1) | 1;
#if DEBUG
            Debug.WriteLine($"Resize _hashesAndIndexes to {_indexMask + 1} because the _entryCount:{_entryCount}");
#endif
        }

        var hashIndex = hash & indexMask;
        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        var hashAndEntryIndex = 0;
        var h = 0;
        byte probes = 1;
        for (; ; ++probes, hashIndex = (hashIndex + 1) & indexMask)
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

            if ((h & ~indexMask) == hashAndEntryIndex)
            {
#if DEBUG
                Debug.WriteLine($"NEW: hp < probes `{probes}` and hashes are equal for the added key:`{key}` and existing key:`{_entries[h & indexMask].Key}`");
#endif
                ref var e = ref _entries[h & indexMask]; // check the existing entry key and update the value if the keys are matched
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
            hashIndex = (hashIndex + 1) & indexMask;
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

        var oldIndexMask = oldCapacity - 1;
        var newIndexMask = newCapacity - 1;
        var newHashMiddleMask = ~newIndexMask & HashAndIndexMask;

        for (var i = 0; i < (uint)oldHashesAndIndexes.Length; i++)
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
            for (byte probes = 1; ; ++probes, newHashIndex = (newHashIndex + 1) & newIndexMask) // we don't need the condition for the MaxProbes because by increasing the hash space we guarantee that we fit the hashes in the finite amount of probes likely less than previous MaxProbeCount
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
