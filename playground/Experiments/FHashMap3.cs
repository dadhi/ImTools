using System;
using System.Diagnostics;

namespace ImTools.Experiments;

// Split hashes to their own array.
// todo: @wip 0 hash is not supported yet
public sealed class FHashMap3<TKey, TValue>
{
    [DebuggerDisplay("Key: {Key}, Value: {Value}")]
    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    public const float MaxLoadFactor = 0.95f;

    public const int DefaultCapacity = 16;

    private const int AddToHashToDistinguishFromEmpty = 0;//1; // todo: @wip change to 1

    private int[] _hashes;
    private int[] _entryIndexes; // maps the hash to the corresponding entry avoiding copying/moving the entry on Resize and robin hood insert
    private Entry[] _entries;

    private uint _maxDistanceFromIdealIndex; // todo: @perf we don't need this field if we store the PSL with hash (in the lower bits?)
    private int _indexMax;
    private int _count;
    public int Count => _count;

    public FHashMap3(int capacity = DefaultCapacity)
    {
        _indexMax = capacity - 1;
        _hashes = new int[capacity];
        _entryIndexes = new int[capacity];
        _entries = new Entry[capacity];
        // _entries = GC.AllocateUninitializedArray<Entry>(capacity); // todo: create without default values using via GC.AllocateUninitializedArray<Entry>(size);
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue = default)
    {
        var hash = key.GetHashCode() | AddToHashToDistinguishFromEmpty;
        var indexMask = _indexMax;

        // todo: @perf you don't need to go all the way to `_maxDistanceFromIdealIndex`, just to the less distance (PSL) ans top here
        // todo: @perf for this matter you'd better store the PSL with hash (in the lower bits?)
        for (uint distance = 0; distance <= _maxDistanceFromIdealIndex; ++distance)
        {
            // todo: @perf @wip seems like we may just calculate once `hash & capacityMask`, and then add distance (or PSL - probe sequence length) to it.
            // todo: @perf @wip `[hashCapacitated + 1, hashCapacitated + 2, hashCapacitated + 3]` and wrap it around the capacity by applying the mask again,
            // todo: @perf @wip `(hashCapacitated + 1) & capacityMask`. 
            // todo: @perf @wip That way we are avoiding storing the full hash, we just need the next bit of the hash for the hashes array double resize.
            // todo: @perf @wip Experiment with resize just by checking the bit in Tests.
            // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
            var hashIndex = (hash + distance) & indexMask;
            var h =_hashes[hashIndex];
            if (h == 0)
                return defaultValue;
            if (h == hash)
            { 
                ref var entry = ref _entries[_entryIndexes[hashIndex]];
                if (entry.Key.Equals(key))
                    return entry.Value;
            }
        }
        return defaultValue;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        // optimistic resize based on one-time check before insert
        var capacity = _hashes.Length;
        if (_count >= capacity * MaxLoadFactor)
            Resize(capacity <<= 1); // double the capacity, using the <<= assinment here to correctly calculate the new capacityMask later

        var hash = key.GetHashCode() | AddToHashToDistinguishFromEmpty;

        // ideal case when we can insert the new item at the ideal index
        var indexMask = _indexMax;
        var idealHashIndex = hash & indexMask;
        var h = _hashes[idealHashIndex];
        if (h == 0)
        {
            // insert the value here
            _hashes[idealHashIndex] = hash;

            // the index of the new added entry at the end of the entries
            var newEntryIndex = _count++;
            _entryIndexes[idealHashIndex] = newEntryIndex;
            ref var e = ref _entries[newEntryIndex]; 
            e.Key = key;
            e.Value = value;
            return;
        }
        if (h == hash)
        {
            // check the existing entry key and update the value if the keys are matched
            ref var e = ref _entries[_entryIndexes[idealHashIndex]];
            if (e.Key.Equals(key))
                e.Value = value;
            return;
        }

        var hashIndex = idealHashIndex;
        var entryIndex = _count; // by default the new entry index is the last one, but the variable may be updated mulpiple times by Robin Hood in the loop
        for (var distance = 1u;; ++distance)
        {
            // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
            hashIndex = (hashIndex + 1) & indexMask;
            h = _hashes[hashIndex];
            if (h == 0)
            {
                // store the initial hash and index or the robin-hooded hash and index.
                _hashes[hashIndex] = hash;
                _entryIndexes[hashIndex] = entryIndex;
                
                // always insert the new entry at the end of the entries array
                ref var e = ref _entries[_count++]; 
                e.Key = key;
                e.Value = value;

                // updating the max distance, required for the lookup operation
                _maxDistanceFromIdealIndex = Math.Max(_maxDistanceFromIdealIndex, distance);
                return;

            }
            if (h == hash)
            {
                // check the existing entry key and update the value if the keys are matched
                ref var e = ref _entries[_entryIndexes[hashIndex]];
                if (e.Key.Equals(key))
                    e.Value = value;
                return;
            }

            // Robin Hood goes here to steal from the rich with the shorter distance to the ideal.
            // we are using the index without wrapping to always get the correct positive entry distance  
            var distancedHashIndex = idealHashIndex + distance; // it is a non-wrapped variant of hashIndex 
            var entryIdealHashIndex = h & indexMask;
            var d = (uint)(distancedHashIndex - entryIdealHashIndex);
            if (d < distance)
            {                
                // Robin Hood swaps the takes the ricj hash and index and puts in their place the poor one (with the longest distance) 
                var tmp = hash; 
                hash = h;
                _hashes[hashIndex] = tmp;

                tmp = entryIndex;
                entryIndex = _entryIndexes[hashIndex];
                _entryIndexes[hashIndex] = tmp;
                
                idealHashIndex = entryIdealHashIndex;
                distance = d; // the distance even if 0 now, will be incremented in the next iteration of the for loop
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
        var maxDistanceFromIdealIndex = 0u;

        // Move all hashes and indexes. We cannot optimize this process (moving just some of the hashes) 
        // because it may introduce the gaps which wrongly indicate that the hash is empty.   
        for (var i = 0; (uint)i < _hashes.Length; i++)
        {
            var hash = _hashes[i];
            if (hash == 0)
                continue;

            var entryIndex = _entryIndexes[i];

            var idealHashIndex = hash & indexMask;
            var h = hashes[idealHashIndex];
            if (h == 0)
            {
                hashes[idealHashIndex] = hash;
                entryIndexes[idealHashIndex] = entryIndex;
                continue;
            }

            var hashIndex = idealHashIndex;
            for (var distance = 1u;; ++distance)
            {
                // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
                hashIndex = (hashIndex + 1) & indexMask;
                h = hashes[hashIndex];
                if (h == 0)
                {
                    hashes[hashIndex] = hash;
                    entryIndexes[hashIndex] = entryIndex;
                    maxDistanceFromIdealIndex = Math.Max(maxDistanceFromIdealIndex, distance);
                    break;
                }

                // Robin Hood goes here to steal from the rich with the shorter distance to the ideal.
                // we are using the index without wrapping to always get the correct positive entry distance  
                var distancedHashIndex = idealHashIndex + distance; // it is a non-wrapped variant of hashIndex 
                var entryIdealHashIndex = h & indexMask;
                var d = (uint)(distancedHashIndex - entryIdealHashIndex);
                if (d < distance)
                {                
                    // Robin Hood swaps the takes the ricj hash and index and puts in their place the poor one (with the longest distance) 
                    var tmp = hash; 
                    hash = h;
                    hashes[hashIndex] = tmp;

                    tmp = entryIndex;
                    entryIndex = entryIndexes[hashIndex];
                    entryIndexes[hashIndex] = tmp;
                    
                    distance = d; // the distance even if 0 now, will be incremented in the next iteration of the for loop
                }
            }
        }
        _maxDistanceFromIdealIndex = maxDistanceFromIdealIndex;
        _indexMax = indexMask;
        _hashes = hashes;
        _entryIndexes = entryIndexes;
    }
}