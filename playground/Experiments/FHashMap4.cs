using System;
using System.Diagnostics;

namespace ImTools.Experiments;

#if DEBUG
public static class DebugExtensions 
{
    public static string b(this int x) => Convert.ToString(x, 2).PadLeft(32, '0'); 
}
#endif

// Combine hashes with indexes to economy on memory
public sealed class FHashMap4<TKey, TValue>
{
    [DebuggerDisplay("Key: {Key}, Value: {Value}")]
    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    public const float MaxLoadFactor = 0.95f;
    public const int DefaultCapacity = 16;
    public const int HighBitSetMask = 1 << 31;
    public const int HashAndIndexMask = ~HighBitSetMask;

    // the int is the combination/union of the 
    // - 1 bit for distance to indicate the hash closest to the ideal index (to account for the distance)
    // - N higher bits of the hash, wher N == 32 - 1(bit for distance) - M bits for the index (the capacity-1 bits)
    // - M bits for the index into the entries array (the capacity-1 bits).
    // The index itself is stored as actual index + 1 to indicate the non-empty slot for the 0 index and the hash with empty lower bits.
    // The removed hash will be actually removed, so we don't use the removed bit here.
    private int[] _hashesAndIndexes;
    private Entry[] _entries;
    private int _capacity;
    private int _count;
    public int Count => _count;

    public FHashMap4(int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _hashesAndIndexes = new int[capacity];
        _entries = new Entry[capacity];
        // todo: @perf benchmark the un-initialized array?
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue = default)
    {
        var hash = key.GetHashCode();
        
        var capacity = _capacity;
        var indexMask = capacity - 1;
        var hashMask = ~indexMask & HashAndIndexMask;

        var hashIndex = hash & indexMask;
        
        var h = 0;
        while (true) 
        {
            h = _hashesAndIndexes[hashIndex];
            if (h == 0)
                return defaultValue;
            // skip hashes before the ideal index (where the distance bit is set)
            if ((h & HighBitSetMask) != 0)
                break;
            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping acound the hashes array
        }
        
        var higherHashPart = hash & hashMask;
        while (true) 
        {
            if ((h & hashMask) == higherHashPart)
            { 
                ref var entry = ref _entries[(h & indexMask) - 1]; // `- 1` here because we store index + 1 to indicate non-empty slot 
                if (entry.Key.Equals(key))
                    return entry.Value;
            }
            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping acound the hashes array
            h = _hashesAndIndexes[hashIndex];
            if (h == 0 | (h & HighBitSetMask) != 0)
                break;
        };
        return defaultValue;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        // optimistic resize based on one-time check before insert
        var capacity = _capacity;
        if (_count + 1 >= capacity * MaxLoadFactor) // `_count + 1` is required because we use `index + 1` to indicate non-empty hash slot 
            Resize(capacity <<= 1); // double the capacity, using the <<= assinment here to correctly calculate the new capacityMask later

        var hash = key.GetHashCode();

        var indexMask = capacity - 1;
        var hashMask = ~indexMask & HashAndIndexMask;

        var hashIndex = hash & indexMask;
        var higherHashPart = hash & hashMask;

        var h = 0;
        while (true) 
        {
            h = _hashesAndIndexes[hashIndex];
            if (h == 0)
            {
                var newEntryIndex = _count++;
                _hashesAndIndexes[hashIndex] = HighBitSetMask | higherHashPart | (newEntryIndex + 1); // `+1` to indicate non-empty slot
                ref var e = ref _entries[newEntryIndex];
                e.Key = key;
                e.Value = value;
                return;
            }
            // skip hashes before the ideal index (where the distance bit is set)
            if ((h & HighBitSetMask) != 0)
                break;
            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping acound the hashes array
        }

        if ((h & hashMask) == higherHashPart)
        {
            // check the existing entry key and update the value if the keys are matched
            ref var e = ref _entries[(h & indexMask) - 1];
            if (e.Key.Equals(key))
            {
                e.Value = value;
                return;
            }
        }

        // todo: @perf optimize the flags better away
        var swapped = false;
        var setNextAsStartInTheSeries = false;               

        var entryIndexPlusOne = _count + 1; // by default the new entry index is the last one, but the variable may be updated mulpiple times by Robin Hood in the loop
        while (true)
        {
            hashIndex = (hashIndex + 1) & indexMask; // `& indexMask` is for wrapping acound the hashes array 
            h = _hashesAndIndexes[hashIndex];
            if (h == 0)
            {
                // store the initial hash and index or the robin-hooded hash and index.
                _hashesAndIndexes[hashIndex] = higherHashPart | entryIndexPlusOne;
                // always insert the new entry at the end of the entries array
                ref var e = ref _entries[_count++]; 
                e.Key = key;
                e.Value = value;
                return;
            }

            if (!swapped & ((h & hashMask) == higherHashPart))
            {
                // check the existing entry key and update the value if the keys are matched
                ref var e = ref _entries[(h & indexMask) - 1];
                if (e.Key.Equals(key))
                {
                    e.Value = value;
                    return;
                }
            }

            // Robin Hood goes here to steal from the rich with the shorter distance to the ideal.
            // e.g. inserting `h` 72 + 16 = 88, but ideal is occupied. So we need to swap it with 73 because it is further from the ideal.
            //      | 0 | 72 | 73 | 0 |
            // bit:       1    88  <-- swap 72 and 88 and proceed with inserting 73  
            if ((h & HighBitSetMask) != 0)
            {                
                var tmp = higherHashPart | entryIndexPlusOne; 
                
                entryIndexPlusOne = h & indexMask;
                higherHashPart = h & hashMask;
                
                _hashesAndIndexes[hashIndex] = tmp;
                
                swapped = true;
                setNextAsStartInTheSeries = true;               
                continue;
            }

            // If we swapped the hash, then we need to set the next slot as a first in the series
            //      | 0 | 72 | 88 | 89 |
            // bit:       1    73  -^ if swapped set idealHighBit to 0, otherwise ignore  
            if (setNextAsStartInTheSeries)
            {
                _hashesAndIndexes[hashIndex] = HighBitSetMask | h;
                setNextAsStartInTheSeries = false;
                continue;
            }
        }
    }

    public void Resize(int newCapacity)
    {
        // Just resize the _entries without copying/moving, we don't need to move them, becuase we will be moving _entryIndexes instead
        Array.Resize(ref _entries, newCapacity);

        var hashes = new int[newCapacity];
        var entryIndexes = new int[newCapacity];
        var indexMask = newCapacity - 1;

        // todo: @wip TBD

       _hashesAndIndexes = hashes;
    }
}