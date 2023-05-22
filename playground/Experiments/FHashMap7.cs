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
#if DEBUG
        Debug.WriteLine($"FirstProbeAdditions: {map.FirstProbeAdditions}, MaxProbes: {map.MaxProbes}");
#endif

        var entries = map._entries;
        var hashesAndIndexes = map._hashesAndIndexes;
        var indexMask = map._indexMask;
        var capacity = indexMask + 1;

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

    public const int DefaultSeedCapacity = 16;
    public const byte MinFreeCapacityShift = 3; // e.g. for the DefaultCapacity 16 >> 3 => 2, so 2 free slots is 12.5% of the capacity  

    public const byte MaxProbeBits = 5; // 5 bits max
    public const byte MaxProbeCount = (1 << MaxProbeBits) - 1;
    public const byte ProbeCountShift = 32 - MaxProbeBits;
    public const int HashAndIndexMask = ~(MaxProbeCount << ProbeCountShift);
    private const int DivBy4RemainderMask = 3;

    // The _hashesAndIndexes item is the Int32, 
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

#if NET7_0_OR_GREATER
    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
    {
        var hash = default(TEq).GetHashCode(key);

        ref var hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_hashesAndIndexes);

        var indexMask = _indexMask;
        var probeAndHashMask = Vector128.Create(~indexMask);

        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

        int probes = 1;
        while (true)
        {
            // create the seed for the batch comparison of 4 hashMiddle with probes incremented by 1
            var hashSeed = Vector128.Create(
                hashMiddle | ( probes    << ProbeCountShift),
                hashMiddle | ((probes+1) << ProbeCountShift),
                hashMiddle | ((probes+2) << ProbeCountShift),
                hashMiddle | ((probes+3) << ProbeCountShift));

            // read the 4 hashesAndIndexes at once into the vector
            ref var h4Ref = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
            var h4 = Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<int, byte>(ref h4Ref));

            var h4ProbeAndHash = h4 & probeAndHashMask;
            var eq = Vector128.Equals(h4ProbeAndHash, hashSeed);

            // the check is the first because if look for the present key, 
            // we will avoid the unnecessary exit condition below. 
            // refarding the check for missing key, it is fine to pain one comparison, 
            // imho - you  will need to select what you care for ;)
            if (!eq.Equals(Vector128<int>.Zero))
            {
                var eqBits = Vector128.ShiftRightLogical(eq, 31);
                var eqMask = eqBits[0] | (eqBits[1] << 1) | (eqBits[2] << 2) | (eqBits[3] << 3); // todo: @perf optimize
                var index = System.Numerics.BitOperations.TrailingZeroCount(eqMask);
                ref var e = ref _entries[h4[index] & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            // Basically doing this but for the 4 elements in vector `(h >> ProbeCountShift) < probes`
            if (Vector128.LessThanAny(
                Vector128.ShiftRightLogical(h4ProbeAndHash, ProbeCountShift), 
                Vector128.ShiftRightLogical(hashSeed, ProbeCountShift)))
                break;

            probes += 4;
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
        var probeAndHashMask = ~indexMask;

        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

        byte probes = 1;
        while (true)
        {
            var h = hashesAndIndexes[hashIndex];
            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref _entries[h & indexMask];
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }
            
            if ((h >> ProbeCountShift) < probes)
                break;
            
            hashIndex = (hashIndex + 1) & indexMask;
            ++probes;
        }
        value = default;
        return false;
    }
#endif

#if NET7_0_OR_GREATER
    public V GetValueOrDefault(K key, V defaultValue = default)
    {
        var hash = default(TEq).GetHashCode(key);

        ref var hashesAndIndexes = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(_hashesAndIndexes);

        var indexMask = _indexMask;
        var probeAndHashMask = Vector128.Create(~indexMask);

        var hashMiddle = hash & ~indexMask & HashAndIndexMask;
        var hashIndex = hash & indexMask;

        int probes = 1;
        while (true)
        {
            // create the seed for the batch comparison of 4 hashMiddle with probes incremented by 1
            var hashSeed = Vector128.Create(
                hashMiddle | ( probes    << ProbeCountShift),
                hashMiddle | ((probes+1) << ProbeCountShift),
                hashMiddle | ((probes+2) << ProbeCountShift),
                hashMiddle | ((probes+3) << ProbeCountShift));

            // read the 4 hashesAndIndexes at once into the vector
            ref var h4Ref = ref Unsafe.Add(ref hashesAndIndexes, hashIndex);
            var h4 = Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<int, byte>(ref h4Ref));

            var h4ProbeAndHash = h4 & probeAndHashMask;
            var eq = Vector128.Equals(h4ProbeAndHash, hashSeed);

            // the check is the first because if look for the present key, 
            // we will avoid the unnecessary exit condition below. 
            // refarding the check for missing key, it is fine to pain one comparison, 
            // imho - you  will need to select what you care for ;)
            if (!eq.Equals(Vector128<int>.Zero))
            {
                var eqBits = Vector128.ShiftRightLogical(eq, 31);
                var eqMask = eqBits[0] | (eqBits[1] << 1) | (eqBits[2] << 2) | (eqBits[3] << 3); // todo: @perf optimize
                var index = System.Numerics.BitOperations.TrailingZeroCount(eqMask);
                ref var e = ref _entries[h4[index] & indexMask];
                if (default(TEq).Equals(e.Key, key))
                    return e.Value;
            }

            // Basically doing this but for the 4 elements in vector `(h >> ProbeCountShift) < probes`
            if (Vector128.LessThanAny(
                Vector128.ShiftRightLogical(h4ProbeAndHash, ProbeCountShift), 
                Vector128.ShiftRightLogical(hashSeed, ProbeCountShift)))
                break;

            probes += 4;
            hashIndex = (hashIndex + 4) & indexMask;
        }
        return default;
    }
#else
    [MethodImpl((MethodImplOptions)256)] // MethodImplOptions.AggressiveInlining
    public bool TryGetValue(K key, out V value)
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
            if ((h & probeAndHashMask) == ((probes << ProbeCountShift) | hashMiddle))
            {
                ref var e = ref _entries[h & indexMask];
                if (default(TEq).Equals(e.Key, key))
                    return e.Value;
            }
            
            if ((h >> ProbeCountShift) < probes)
                break;
            
            hashIndex = (hashIndex + 1) & indexMask;
            ++probes;
        }
        return default;
    }
#endif

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
            _hashesAndIndexes = hashesAndIndexes = DoubleSize(_hashesAndIndexes, indexMask);
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

    [MethodImpl((MethodImplOptions)256)]
    internal static int[] CreatePaddedHashesAndIndexes(int capacity)
    {
        // `+3` because we need to align the `a` to 4 elements boundary for the Vector128 read starting from the last index in array
        var a = new int[capacity + 3];
        a[capacity] = MaxProbeCount << ProbeCountShift;
        a[capacity + 1] = MaxProbeCount << ProbeCountShift;
        a[capacity + 2] = MaxProbeCount << ProbeCountShift;
        return a; 
    }

    internal static int[] DoubleSize(int[] oldHashesAndIndexes, int oldIndexMask)
    {
        var oldCapacity = oldIndexMask + 1;
        var newCapacity = oldCapacity << 1;
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
