using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Benchmarks
{
    public static class RefEqHashMap
    {
        public const int InitialCapacity = 1 << 5; // 32

        // reserve quarter of capacity for hash conflicts
        public static RefKeyValue<K, V>[] Empty<K, V>(int initialCapacity = InitialCapacity)
            where K : class =>
            new RefKeyValue<K, V>[initialCapacity];

        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this RefKeyValue<K, V>[] slots, K key)
            where K : class
        {
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode();

            ref var idealSlot = ref slots[hash & capacityMask];
            if (idealSlot.Key == key)
                return idealSlot.Value;

            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 1; distance < capacity; ++distance)
            {
                ref var slot = ref slots[(hash + distance) & capacityMask];
                if (slot.Key == key)
                    return slot.Value;
                if (slot.Key == null) // not found, stop on an empty key
                    break;
            }

            return default(V);
        }

        // may return new slots if old slot capacity is not enough to add a new item
        public static RefKeyValue<K, V>[] AddOrUpdate<K, V>(this RefKeyValue<K, V>[] slots, K key, V value)
            where K : class
        {
            var capacity = slots.Length;
            var hash = key.GetHashCode();
            if (TryPut(slots, capacity, hash, key, value))
                return slots;

            // Expand slots: Create a new slots and re-populate them from the old slots.
            // Expanding slots will double the capacity.
            var newCapacity = capacity << 1;
            var newSlots = new RefKeyValue<K, V>[newCapacity];

            for (var i = 0; i < capacity; i++)
            {
                ref var slot = ref slots[i];
                var slotKey = slot.Key;
                if (slotKey != null)
                    TryPut(newSlots, newCapacity, slotKey.GetHashCode(), slotKey, slot.Value);
            }

            TryPut(newSlots, newCapacity, hash, key, value);
            return newSlots;
        }

        private static bool TryPut<K, V>(RefKeyValue<K, V>[] slots, int capacity, int hash, K key, V value)
            where K : class
        {
            var capacityMask = capacity - 1;
            var idealIndex = hash & capacityMask;
            if (Interlocked.CompareExchange(ref slots[idealIndex].Key, key, null) == null)
            {
                slots[idealIndex].Value = value;
                return true;
            }

            capacity >>= 2; // search only for the quarter of capacity
            for (var distance = 1; distance < capacity; ++distance)
            {
                var index = (hash + distance) & capacityMask;
                if (Interlocked.CompareExchange(ref slots[index].Key, key, null) == null)
                {
                    slots[index].Value = value;
                    return true;
                }
            }
            return false;
        }
    }

    public struct RefKeyValue<K, V> where K : class
    {
        public K Key;
        public V Value;
    }
}
