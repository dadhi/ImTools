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
            for (var distance = 1; distance < capacity; ++distance)
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
            if (TryPut(slots, capacity, capacity - 1, hash, key, value))
                return;

            // Expand slots: Create a new slots and re-populate them from the old slots.
            // Expanding slots will double the capacity.
            var newCapacity = capacity << 1;
            var newSlots = new RefKeyValue<K, V>[newCapacity]; // todo: @perf can we Array.Resize here?
            var newCapacityMask = newCapacity - 1;
            for (var i = 0; i < capacity; i++)
            {
                ref var slot = ref slots[i];
                var slotKey = slot.Key;
                if (slotKey != null)
                    TryPut(newSlots, newCapacity, newCapacityMask, slotKey.GetHashCode(), slotKey, slot.Value);
            }

            TryPut(newSlots, newCapacity, newCapacityMask, hash, key, value);
            _slots = newSlots;
        }

        private static bool TryPut(RefKeyValue<K, V>[] slots, int capacity, int capacityMask, int hash, K key, V value)
        {
            var idealIndex = hash & capacityMask;
            if (Interlocked.CompareExchange(ref slots[idealIndex].Key, key, null) == null)
            {
                slots[idealIndex].Value = value;
                return true;
            }

            capacity >>= 2; // search only for the quarter of capacity
            for (var distance = 1; distance < capacity; ++distance)
            {
                var index = (hash + distance) & capacityMask; // wrap around the array boundaries and start from the beginning
                if (Interlocked.CompareExchange(ref slots[index].Key, key, null) == null)
                {
                    slots[index].Value = value;
                    return true;
                }
            }
            return false;
        }
    }
}
