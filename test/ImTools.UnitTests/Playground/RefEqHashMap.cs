using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools
{
    // todo: @perf improve the performance by storing the hash, especially when we expanding the hash map?
    public struct RefKeyValue<K, V> where K : class
    {
        public K Key;
        public V Value;
        public RefKeyValue(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    public struct RefEqHashMap<K, V> where K : class
    {
        //todo: @wip can we put first N slot on the stack here like in `ImTools.MapStack`
        public RefKeyValue<K, V>[] _slots;

        // <summary>The actual capacity is calculated as 2^capacityBits, e.g. 2^2 = 4 slots, 2^3 = 8 slots, etc.</summary>
        public RefEqHashMap(int capacityBits) =>
            _slots = new RefKeyValue<K, V>[1 << capacityBits];

        /// <summary>Returns the index of Slot with the give key or `-1` otherwise</summary>
        public int IndexOf(K key)
        {
            var slots = _slots;
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode();

            var idealIndex = hash & capacityMask;
            if (slots[idealIndex].Key == key)
                return idealIndex;

            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 1; distance <= capacity; ++distance)
            {
                var index = (hash + distance) & capacityMask;
                ref var slot = ref slots[index];
                if (slot.Key == key)
                    return index;
                if (slot.Key == null) // not found, stop on an empty key
                    break;
            }

            return -1;
        }

        public V GetValueOrDefault(K key) 
        {
            var index = IndexOf(key);
            return index == -1 ? default(V) : _slots[index].Value;   
        }

        public void AddOrUpdate(K key, V value)
        { 
            var slots = _slots;
            var capacity = slots.Length;
            var hash = key.GetHashCode();
            if (TryPut(slots, capacity >> 2, capacity - 1, hash, key, value))
                return;

            // We got here because we did not find the empty slot to put the item from the ideal index to the distance equal quarter of capacity.  
            // Expand slots in a loop (if needed, hopefully single iteration is enough).
            // Expanding slots will double the capacity.
            var success = false;
            while (!success) 
            {
                expand:
                capacity <<= 1; // double the capacity
                var newSlots = new RefKeyValue<K, V>[capacity];

                var capacityMask = capacity - 1;
                var searchDistance = capacity >> 2; // search only for the quarter of capacity
                for (var i = 0; i < slots.Length; ++i)
                {
                    ref var slot = ref slots[i];
                    var itemKey = slot.Key;
                    // for every non-empty old slot, copy the item to the new slots
                    if (itemKey != null)
                    {
                        success = TryPut(newSlots, searchDistance, capacityMask, itemKey.GetHashCode(), itemKey, slot.Value);
                        if (!success)
                            goto expand; // if unable to put the item, try expand further
                    }
                }
                if (success = TryPut(newSlots, searchDistance, capacityMask, hash, key, value))
                    _slots = newSlots; // if we were able to put the item, set the slots to the new ones
            }
        }

        private static bool TryPut(RefKeyValue<K, V>[] slots, int searchDistance, int capacityMask, int hash, K key, V value)
        {
            ref var idealSlot = ref slots[hash & capacityMask];
            // adding the new item to the empty slot, or updating the item (checking by the reference equality)
            if (idealSlot.Key == null || idealSlot.Key == key)
            {
                idealSlot.Key = key; // todo: @perf maybe not the case if we are updating the item
                idealSlot.Value = value;
                return true;
            }
            for (uint distance = 1; distance <= searchDistance; ++distance)
            {
                // wrap around the array boundaries and start from the beginning
                ref var slot = ref slots[(hash + distance) & capacityMask];
                // adding the new item to the empty slot, or updating the item (checking by the reference equality)
                if (slot.Key == null || slot.Key == key)
                {
                    slot.Key = key; // todo: @perf maybe not the case if we are updating the item
                    slot.Value = value;
                    return true;
                }
            }
            return false;
        }
    }
}
