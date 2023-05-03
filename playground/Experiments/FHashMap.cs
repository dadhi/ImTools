using System;
using System.Diagnostics;

namespace ImTools.Experiments;

public sealed class FHashMap1<TKey, TValue>
{
    [DebuggerDisplay("Hash: {Hash}, Key: {Key}, Value: {Value}")]
    public struct Entry
    {
        public int Hash;
        public TKey Key;
        public TValue Value;
    }

    public const int DefaultCapacity = 16;

    private Entry[] _entries;

    private uint _maxDistanceFromIdealIndex;
    private int _count;
    public int Count => _count;

    public FHashMap1(int capacity = DefaultCapacity)
    {
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
            ref var entry = ref _entries[wrappedIndex];
            if (entry.Hash == 0)
                return defaultValue;
            if (entry.Hash == hash && entry.Key.Equals(key))
                return entry.Value;
        }
        return defaultValue;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        var hash = key.GetHashCode();
        var capacityMask = _entries.Length - 1;

        // we know the _maxDistanceFromIdealIndex, so the worst case would be if insert the new item at maxDistance + 1
        var worstDistance = _maxDistanceFromIdealIndex + 1;
        for (uint distance = 0; distance <= worstDistance; ++distance)
        {
            // we need to add distance to the hash first (and not just increment the index) because we need to wrap around the entries array
            var wrappedIndex = (hash + distance) & capacityMask;
            ref var entry = ref _entries[wrappedIndex];
            if (entry.Hash == 0)
            {
                entry.Hash = hash;
                entry.Key = key;
                entry.Value = value;

                // we are succussfully inserted the new item
                ++_count;
                _maxDistanceFromIdealIndex = Math.Max(_maxDistanceFromIdealIndex, distance);
                return;

            }
            if (entry.Hash == hash && entry.Key.Equals(key))
            {
                // we are succussfully updated the existing item, nothing globally change in respect to entry count or max distance
                entry.Value = value;
                return;
            }

            var entryIdealIndex = entry.Hash & capacityMask;

            // we are using the index without wrapping to always get the correct positive entry distance  
            var virtualIndexWithoutWrap = (hash & capacityMask) + distance;
            var entryDistance = (uint)(virtualIndexWithoutWrap - entryIdealIndex);
            if (entryDistance < distance)
            {
                // If the entry distance is less than the current insert distance, then store the new key-value here, and move the current entry further
                var tmp = entry; // copying the entry struct to avoid rewriting it by the new key-value

                // store the new key-value
                entry.Hash = hash;
                entry.Key = key;
                entry.Value = value;

                // switch to the existing entry that need to be stored in the farther distance 
                hash = tmp.Hash;
                key = tmp.Key;
                value = tmp.Value;
                distance = entryDistance;
            }
        }
    }
}
