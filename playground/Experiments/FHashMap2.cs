using System;
using System.Diagnostics;

namespace ImTools.Experiments;

// Split hashes to their own array.
// todo: @wip 0 hash is not supported yet
public sealed class FHashMap2<TKey, TValue>
{
    [DebuggerDisplay("Key: {Key}, Value: {Value}")]
    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    public const float MaxLoadFactor = 0.95f;

    public const int DefaultCapacity = 16;

    private int[] _hashes;
    private Entry[] _entries;

    private uint _maxDistanceFromIdealIndex;
    private int _count;
    public int Count => _count;

    public FHashMap2(int capacity = DefaultCapacity)
    {
        _hashes = new int[capacity];
        _entries = new Entry[capacity];
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue = default)
    {
        var hash = key.GetHashCode();
        var capacityMask = _entries.Length - 1;
        for (uint distance = 0; distance <= _maxDistanceFromIdealIndex; ++distance)
        {
            // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
            var wrappedIndex = (hash + distance) & capacityMask;
            var entryHash = _hashes[wrappedIndex]; 
            if (entryHash == 0)
                return defaultValue;
            if (entryHash == hash) // todo: @perf compare distances first, so we can stop if entry distance is smaller than the current one?
            { 
                ref var entry = ref _entries[wrappedIndex];
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

        var hash = key.GetHashCode();
        var capacityMask = capacity - 1;

        // ideal case when we can insert the new item at the ideal index
        var idealIndex = hash & capacityMask;
        var entryHash = _hashes[idealIndex];
        if (entryHash == 0)
        {
            _hashes[idealIndex] = hash;
            ref var entry = ref _entries[idealIndex];
            entry.Key = key;
            entry.Value = value;
            ++_count;
            return;
        }
        if (entryHash == hash)
        {
            ref var entry = ref _entries[idealIndex];
            if (entry.Key.Equals(key))
                entry.Value = value;
            return;
        }

        // we know the _maxDistanceFromIdealIndex, so the worst case would be if insert the new item at maxDistance + 1
        var worstDistance = _maxDistanceFromIdealIndex + 1;
        for (uint distance = 1; distance <= worstDistance; ++distance)
        {
            // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
            var wrappedIndex = (hash + distance) & capacityMask;
            entryHash = _hashes[wrappedIndex];
            if (entryHash == 0)
            {
                _hashes[wrappedIndex] = hash;
                ref var entry = ref _entries[wrappedIndex];
                entry.Key = key;
                entry.Value = value;
                
                ++_count;

                // we need to update the max distance
                _maxDistanceFromIdealIndex = Math.Max(_maxDistanceFromIdealIndex, distance); // todo: @perf try to replace with simple comparison instead of the Math method call
                return;

            }
            if (entryHash == hash)
            {
                // we are succussfully updated the existing item, nothing globally change in respect to entry count or max distance
                ref var entry = ref _entries[wrappedIndex];
                if (entry.Key.Equals(key))
                    entry.Value = value;
                return;
            }

            // we are using the index without wrapping to always get the correct positive entry distance  
            var nonWrappedIndex = (hash & capacityMask) + distance;
            var entryIdealIndex = entryHash & capacityMask;
            var entryDistance = (uint)(nonWrappedIndex - entryIdealIndex);
            if (entryDistance < distance)
            {
                // If the entry distance is less than the current insert distance, then store the new key-value here, and move the current entry further
                ref var entry = ref _entries[wrappedIndex];
                var tmp = entry; // copying the entry struct to avoid rewriting it by the new key-value

                // store the new key-value
                entry.Key = key;
                entry.Value = value;

                // switch to the existing entry that need to be stored in the farther distance 
                key = tmp.Key;
                value = tmp.Value;
                
                _hashes[wrappedIndex] = hash;
                hash = entryHash;

                distance = entryDistance; // the distance even if 0 now, will be incremented in the next iteration of the for loop
            }
        }
    }

    public void Resize(int newCapacity)
    {
        var hashes = new int[newCapacity];
        var entries = new Entry[newCapacity];

        var capacityMask = newCapacity - 1;
        var maxDistanceFromIdealIndex = 0u;

        // rehash all the entries and copy the to possibly new indexes
        // todo: @perf track what indexes are actually changed to avoid the movement
        for (var i = 0; (uint)i < _hashes.Length; i++)
        {
            var existingHash = _hashes[i];
            if (existingHash == 0)
                continue;

            var existingEntry = _entries[i];
            for (var distance = 0u;;++distance)
            {
                // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
                var wrappedIndex = (existingHash + distance) & capacityMask;
                var entryHash = hashes[wrappedIndex];
                if (entryHash == 0)
                {
                    hashes[wrappedIndex] = existingHash;
                    entries[wrappedIndex] = existingEntry;
                    maxDistanceFromIdealIndex = Math.Max(maxDistanceFromIdealIndex, distance);
                    break;
                }

                if (distance > 0) 
                {
                    // we are using the index without wrapping to always get the correct positive entry distance  
                    var nonWrappedIndex = (existingHash & capacityMask) + distance;
                    var entryIdealIndex = entryHash & capacityMask;
                    var entryDistance = (uint)(nonWrappedIndex - entryIdealIndex);
                    if (entryDistance < distance)
                    {
                        // swap entries
                        ref var entry = ref entries[wrappedIndex];
                        var tmp = entry;
                        entry = existingEntry;
                        existingEntry = tmp;

                        hashes[wrappedIndex] = existingHash;
                        existingHash = entryHash;
                        
                        distance = entryDistance;
                    }
                }
            }
        }

        _hashes = hashes;
        _entries = entries;
        _maxDistanceFromIdealIndex = maxDistanceFromIdealIndex;
    }
}